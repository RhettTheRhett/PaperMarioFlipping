using System;
using System.Collections.Generic;
using UnityEngine;

// Owns the list of panes, tracks which one the player currently belongs to and
// tells every pane member when to load, unload or extrude itself.
public class PaneManager : MonoBehaviour
{
    private static PaneManager instance;

    [Header("Fallback Pane")]
    [Tooltip("Scenes with no Pane objects get one big pane so the game still behaves like a single flat plane.")]
    public bool createFallbackPane = true;
    public float fallbackDepth = 0f;
    public float fallbackThickness = 60f;

    [Header("Flat Render")]
    [Tooltip("While flat, panes further away than this still draw but never collide.")]
    public float graphicsRange = 0f;

    private readonly List<Pane> panes = new List<Pane>();
    private readonly List<PaneMember> members = new List<PaneMember>();
    private readonly List<Pane> sortBuffer = new List<Pane>();

    private WorldStateManager worldStateManager;
    private Pane fallbackPane;
    private bool started;

    public Pane CurrentPane { get; private set; }

    public event Action<Pane, Pane> OnCurrentPaneChanged;

    public static bool Exists { get { return instance != null; } }

    public int PaneCount { get { return panes.Count; } }

    public float CurrentDepth { get { return CurrentPane != null ? CurrentPane.Depth : fallbackDepth; } }

    public float CurrentThickness { get { return CurrentPane != null ? CurrentPane.thickness : fallbackThickness; } }

    public int CurrentPaneIndex { get { return CurrentPane != null ? CurrentPane.paneIndex : 0; } }

    public static PaneManager Get()
    {
        if (instance != null) return instance;

        instance = FindObjectOfType<PaneManager>();
        if (instance == null)
        {
            GameObject host = new GameObject("PaneManager");
            instance = host.AddComponent<PaneManager>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        worldStateManager = WorldStateManager.Get();
        worldStateManager.OnWorldStateChanged += HandleWorldStateChanged;
        worldStateManager.OnRenderModeChanged += HandleRenderModeChanged;
        worldStateManager.OnFlipCompleted += HandleWorldStateChanged;
    }

    private void OnDestroy()
    {
        if (worldStateManager != null)
        {
            worldStateManager.OnWorldStateChanged -= HandleWorldStateChanged;
            worldStateManager.OnRenderModeChanged -= HandleRenderModeChanged;
            worldStateManager.OnFlipCompleted -= HandleWorldStateChanged;
        }
        if (instance == this) instance = null;
    }

    private void Start()
    {
        started = true;
        EnsureFallbackPane();

        if (CurrentPane == null && panes.Count > 0) SetCurrentPane(panes[0]);
        RefreshMembers();
    }

    #region Registration

    public void RegisterPane(Pane pane)
    {
        if (pane == null || panes.Contains(pane)) return;

        panes.Add(pane);
        panes.Sort(CompareByDepth);

        if (CurrentPane == null) SetCurrentPane(pane);
        if (started) RefreshMembers();
    }

    public void UnregisterPane(Pane pane)
    {
        if (pane == null) return;

        panes.Remove(pane);
        if (CurrentPane == pane) SetCurrentPane(panes.Count > 0 ? panes[0] : null);
    }

    public void RegisterMember(PaneMember member)
    {
        if (member == null || members.Contains(member)) return;

        members.Add(member);
        if (started) member.ApplyState(worldStateManager, this);
    }

    public void UnregisterMember(PaneMember member)
    {
        if (member == null) return;
        members.Remove(member);
    }

    private static int CompareByDepth(Pane a, Pane b)
    {
        return a.Depth.CompareTo(b.Depth);
    }

    private void EnsureFallbackPane()
    {
        if (!createFallbackPane || panes.Count > 0) return;

        GameObject host = new GameObject("Pane (auto)");
        host.transform.position = new Vector3(0f, 0f, fallbackDepth);

        fallbackPane = host.AddComponent<Pane>();
        fallbackPane.paneIndex = 0;
        fallbackPane.useTransformZ = true;
        fallbackPane.thickness = fallbackThickness;
        fallbackPane.autoAssignChildren = false;
    }

    #endregion

    #region Queries

    public Pane GetPaneAt(float worldZ)
    {
        Pane nearest = null;
        for (int i = 0; i < panes.Count; i++)
        {
            if (panes[i].Contains(worldZ) && (nearest == null || panes[i].DistanceTo(worldZ) < nearest.DistanceTo(worldZ)))
                nearest = panes[i];
        }
        return nearest;
    }

    public Pane GetNearestPane(float worldZ)
    {
        Pane best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < panes.Count; i++)
        {
            float distance = panes[i].DistanceTo(worldZ);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = panes[i];
        }
        return best;
    }

    // Panes ordered by how close they are to a depth, used when a flip back to
    // 2D has to fall back to another slice because the nearest one is blocked.
    public List<Pane> GetPanesByDistance(float worldZ)
    {
        sortBuffer.Clear();
        sortBuffer.AddRange(panes);
        sortBuffer.Sort((a, b) => a.DistanceTo(worldZ).CompareTo(b.DistanceTo(worldZ)));
        return sortBuffer;
    }

    public bool IsCurrent(Pane pane)
    {
        // Objects that were never assigned to a pane always count as present.
        return pane == null || CurrentPane == null || pane == CurrentPane;
    }

    #endregion

    #region Current pane

    public void SetCurrentPane(Pane pane)
    {
        if (CurrentPane == pane) return;

        Pane previous = CurrentPane;
        CurrentPane = pane;

        RefreshMembers();
        if (OnCurrentPaneChanged != null) OnCurrentPaneChanged(previous, pane);
    }

    // Called while the world is 3D, where crossing depth changes which pane you
    // will land in when flipping back.
    public void TrackDepth(float worldZ)
    {
        Pane containing = GetPaneAt(worldZ);
        if (containing == null) containing = GetNearestPane(worldZ);
        if (containing != null) SetCurrentPane(containing);
    }

    public float SnapDepth(float worldZ)
    {
        Pane pane = CurrentPane != null ? CurrentPane : GetNearestPane(worldZ);
        return pane != null ? pane.Depth : fallbackDepth;
    }

    #endregion

    public void RefreshMembers()
    {
        if (worldStateManager == null) worldStateManager = WorldStateManager.Get();

        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (members[i] == null)
            {
                members.RemoveAt(i);
                continue;
            }
            members[i].ApplyState(worldStateManager, this);
        }
    }

    private void HandleWorldStateChanged(WorldState state)
    {
        RefreshMembers();
    }

    private void HandleRenderModeChanged(MapRenderMode mode)
    {
        RefreshMembers();
    }
}

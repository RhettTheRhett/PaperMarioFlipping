using System;
using UnityEngine;

public enum WorldState
{
    Flat2d,
    Flipped3d,
}

// Show every pane during a flip; return to the selected slice in flat view.
public enum MapRenderMode
{
    FlatRender,
    RawRender,
}

public class WorldStateManager : MonoBehaviour
{
    private static WorldStateManager instance;

    [Header("Gameplay Flag")]
    public WorldState worldState;

    [Header("Presentation")]
    public MapRenderMode renderMode = MapRenderMode.FlatRender;

    public bool IsFlipping { get; private set; }
    public float FlipProgress { get; private set; }
    public WorldState FlipFrom { get; private set; }
    public WorldState FlipTo { get; private set; }
    public float FlipDuration { get; private set; }

    public event Action<WorldState> OnWorldStateChanged;
    public event Action<WorldState, WorldState> OnFlipStarted;
    public event Action<WorldState> OnFlipCompleted;
    public event Action<MapRenderMode> OnRenderModeChanged;

    public bool Is2d { get { return worldState == WorldState.Flat2d; } }

    public static WorldStateManager Instance { get { return Get(); } }

    public static WorldStateManager Get()
    {
        if (instance != null) return instance;

        instance = FindObjectOfType<WorldStateManager>();
        if (instance == null)
        {
            GameObject host = new GameObject("WorldStateManager");
            instance = host.AddComponent<WorldStateManager>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        renderMode = RenderModeFor(worldState);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public WorldState GetWorldState()
    {
        return worldState;
    }

    public void ChangeWorldState(WorldState newState)
    {
        SetWorldState(newState);
        if (!IsFlipping) SetRenderMode(RenderModeFor(newState));
    }

    public WorldState ToggleWorldState()
    {
        ChangeWorldState(worldState == WorldState.Flat2d ? WorldState.Flipped3d : WorldState.Flat2d);
        return worldState;
    }

    public void BeginFlip(WorldState target, float duration)
    {
        FlipFrom = worldState;
        FlipTo = target;
        FlipDuration = Mathf.Max(0.0001f, duration);
        FlipProgress = 0f;
        IsFlipping = true;

        // The gameplay flag changes up front so movement and the camera start
        // following the new dimension while the world is still rotating.
        SetWorldState(target);

        // Depth has to stay visible for the whole rotation, so both directions
        // animate in the raw render and only settle into flat afterwards.
        SetRenderMode(MapRenderMode.RawRender);

        if (OnFlipStarted != null) OnFlipStarted(FlipFrom, FlipTo);
    }

    public void ReportFlipProgress(float t)
    {
        FlipProgress = Mathf.Clamp01(t);
    }

    public void CompleteFlip()
    {
        if (!IsFlipping) return;

        IsFlipping = false;
        FlipProgress = 1f;
        SetRenderMode(RenderModeFor(worldState));

        if (OnFlipCompleted != null) OnFlipCompleted(worldState);
    }

    private void SetWorldState(WorldState newState)
    {
        if (worldState == newState) return;

        worldState = newState;
        if (OnWorldStateChanged != null) OnWorldStateChanged(newState);
    }

    private void SetRenderMode(MapRenderMode mode)
    {
        if (renderMode == mode) return;

        renderMode = mode;
        if (OnRenderModeChanged != null) OnRenderModeChanged(mode);
    }

    private static MapRenderMode RenderModeFor(WorldState state)
    {
        return state == WorldState.Flat2d ? MapRenderMode.FlatRender : MapRenderMode.RawRender;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Binds an object to a depth area. In 2D its colliders project through that
// area; in 3D they return to their authored shapes.
public class PaneMember : MonoBehaviour
{
    [Header("Pane")]
    public Pane pane;
    [Tooltip("Find a parent Pane, or fall back to the nearest pane by depth, when none is assigned.")]
    public bool autoResolvePane = true;

    [Header("Flat Behaviour")]
    [Tooltip("Disable colliders while the flat render is showing a different pane.")]
    public bool unloadWhenOffPane = true;
    [Tooltip("Hide renderers once the pane is beyond the manager's graphics range.")]
    public bool hideWhenOffPane = true;
    [Tooltip("Extrude box colliders through the pane so anything lined up on screen can be touched.")]
    public bool extrudeHitbox = true;

    private struct BoxState
    {
        public BoxCollider box;
        public Vector3 size;
        public Vector3 center;
        public bool extrudable;
    }

    // Interaction volumes are radii rather than boxes, so they stretch by turning
    // into a depth-aligned capsule instead of growing a face.
    private struct CapsuleState
    {
        public CapsuleCollider capsule;
        public float height;
        public int direction;
        public Vector3 center;
        public bool extrudable;
    }

    private Collider[] colliders;
    private bool[] colliderEnabled;
    private Renderer[] renderers;
    private bool[] rendererEnabled;
    private readonly List<BoxState> boxes = new List<BoxState>();
    private readonly List<CapsuleState> capsules = new List<CapsuleState>();

    private bool extruded;
    private bool cached;

    public float Depth { get { return pane != null ? pane.Depth : transform.position.z; } }

    private void Awake()
    {
        // The player is driven by its own scripts and must never be unloaded.
        if (CompareTag("Player"))
        {
            enabled = false;
            return;
        }
        CacheComponents();
    }

    private void OnEnable()
    {
        PaneManager.Get().RegisterMember(this);
    }

    private void OnDisable()
    {
        if (PaneManager.Exists) PaneManager.Get().UnregisterMember(this);
    }

    private void CacheComponents()
    {
        if (cached) return;
        cached = true;

        List<Collider> ownColliders = new List<Collider>();
        Collider[] found = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < found.Length; i++)
        {
            // Extended hitboxes drive themselves through HitboxExtender.
            if (found[i].GetComponent<HitboxExtender>() != null) continue;
            ownColliders.Add(found[i]);
        }

        colliders = ownColliders.ToArray();
        colliderEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;

        List<Renderer> ownRenderers = new List<Renderer>();
        Renderer[] foundRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < foundRenderers.Length; i++)
        {
            // Line renderers are usually debug or effect output owned by another
            // script, so toggling them here just fights that script.
            if (foundRenderers[i] is LineRenderer) continue;
            ownRenderers.Add(foundRenderers[i]);
        }

        renderers = ownRenderers.ToArray();
        rendererEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererEnabled[i] = renderers[i].enabled;

        boxes.Clear();
        capsules.Clear();
        for (int i = 0; i < colliders.Length; i++)
        {
            // Extruding along local Z only lines up with the depth axis when the
            // object is not turned, so rotated props keep their authored shape.
            bool axisAligned = Quaternion.Angle(colliders[i].transform.rotation, Quaternion.identity) < 5f;

            BoxCollider box = colliders[i] as BoxCollider;
            if (box != null)
            {
                BoxState state;
                state.box = box;
                state.size = box.size;
                state.center = box.center;
                state.extrudable = axisAligned;
                boxes.Add(state);
                continue;
            }

            CapsuleCollider capsule = colliders[i] as CapsuleCollider;
            if (capsule == null) continue;

            CapsuleState capsuleState;
            capsuleState.capsule = capsule;
            capsuleState.height = capsule.height;
            capsuleState.direction = capsule.direction;
            capsuleState.center = capsule.center;
            capsuleState.extrudable = axisAligned;
            capsules.Add(capsuleState);
        }
    }

    public void ApplyState(WorldStateManager worldStateManager, PaneManager paneManager)
    {
        if (!enabled || worldStateManager == null || paneManager == null) return;

        CacheComponents();
        ResolvePane(paneManager);

        bool flatRender = worldStateManager.renderMode == MapRenderMode.FlatRender;
        // Extrusion waits for the rotation to finish: growing hitboxes while the
        // player is parked mid-flip would shove them out of the world.
        bool flatGameplay = worldStateManager.worldState == WorldState.Flat2d && !worldStateManager.IsFlipping;
        bool onPane = paneManager.IsCurrent(pane);

        // Loading follows the render mode, hitbox extrusion follows the gameplay
        // flag. Keeping them apart is what makes the two modes independent.
        bool loaded = !flatRender || onPane || !unloadWhenOffPane;
        bool visible = !flatRender || onPane || !hideWhenOffPane
                       || Mathf.Abs(Depth - paneManager.CurrentDepth) <= paneManager.graphicsRange;

        SetCollidersLoaded(loaded);
        SetRenderersVisible(visible);
        SetExtruded(flatGameplay && extrudeHitbox, paneManager);
    }

    private void ResolvePane(PaneManager paneManager)
    {
        if (pane != null || !autoResolvePane) return;

        pane = GetComponentInParent<Pane>();
        if (pane != null) return;

        pane = paneManager.GetPaneAt(transform.position.z);
        if (pane == null) pane = paneManager.GetNearestPane(transform.position.z);
    }

    private void SetCollidersLoaded(bool loaded)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            colliders[i].enabled = loaded && colliderEnabled[i];
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].enabled = visible && rendererEnabled[i];
        }
    }

    private void SetExtruded(bool extrude, PaneManager paneManager)
    {
        if (extrude == extruded) return;
        extruded = extrude;

        float depth = pane != null ? pane.Depth : paneManager.CurrentDepth;
        float thickness = pane != null ? pane.thickness : paneManager.CurrentThickness;

        for (int i = 0; i < boxes.Count; i++)
        {
            BoxState state = boxes[i];
            if (state.box == null || !state.extrudable) continue;

            if (!extrude)
            {
                state.box.size = state.size;
                state.box.center = state.center;
                continue;
            }

            Transform boxTransform = state.box.transform;
            float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(boxTransform.lossyScale.z));

            Vector3 worldCenter = boxTransform.TransformPoint(state.center);
            worldCenter.z = depth;
            Vector3 localCenter = boxTransform.InverseTransformPoint(worldCenter);

            state.box.size = new Vector3(state.size.x, state.size.y, Mathf.Max(0.01f, thickness) / scaleZ);
            state.box.center = new Vector3(state.center.x, state.center.y, localCenter.z);
        }

        for (int i = 0; i < capsules.Count; i++)
        {
            CapsuleState state = capsules[i];
            if (state.capsule == null || !state.extrudable) continue;

            if (!extrude)
            {
                state.capsule.height = state.height;
                state.capsule.direction = state.direction;
                state.capsule.center = state.center;
                continue;
            }

            Transform capsuleTransform = state.capsule.transform;
            float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(capsuleTransform.lossyScale.z));

            Vector3 worldCenter = capsuleTransform.TransformPoint(state.center);
            worldCenter.z = depth;
            Vector3 localCenter = capsuleTransform.InverseTransformPoint(worldCenter);

            state.capsule.direction = 2;
            state.capsule.height = Mathf.Max(0.01f, thickness) / scaleZ;
            state.capsule.center = new Vector3(state.center.x, state.center.y, localCenter.z);
        }
    }
}

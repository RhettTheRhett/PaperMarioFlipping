using UnityEngine;

public class EnemyDimension : MonoBehaviour
{
    public enum Presence
    {
        Flat2DOnly,
        Flat3DOnly,
        BothDimensions
    }

    [SerializeField] private Presence presence = Presence.Flat2DOnly;

    private WorldStateManager world;
    private Collider[] hitboxes;
    private Renderer[] visuals;
    private bool[] initialHitboxStates;
    private bool[] initialVisualStates;
    private Collider activityHitbox;
    private bool dimensionInteractive;
    private WorldState lastAppliedWorldState;
    private bool hasAppliedWorldState;

    public Presence DimensionPresence { get { return presence; } }
    // PaneMember unloads an off-pane enemy by disabling its main collider.
    // Keeping that check here gives every enemy controller one activity gate.
    public bool IsInteractive
    {
        get { return dimensionInteractive && (activityHitbox == null || activityHitbox.enabled); }
    }
    public bool CanSimulate
    {
        get
        {
            // Flat-2D enemies remain alive in the revealed 3D world even
            // though their player-facing collision is disabled there.
            if (presence == Presence.Flat2DOnly && world != null && !world.Is2d) return true;
            return IsInteractive;
        }
    }

    private void Awake()
    {
        hitboxes = GetComponentsInChildren<Collider>(true);
        visuals = GetComponentsInChildren<Renderer>(true);
        activityHitbox = GetComponent<Collider>();
        if (activityHitbox == null && hitboxes.Length > 0) activityHitbox = hitboxes[0];
        initialHitboxStates = new bool[hitboxes.Length];
        initialVisualStates = new bool[visuals.Length];

        for (int i = 0; i < hitboxes.Length; i++) initialHitboxStates[i] = hitboxes[i].enabled;
        for (int i = 0; i < visuals.Length; i++) initialVisualStates[i] = visuals[i].enabled;

        world = WorldStateManager.Get();
    }

    private void OnEnable()
    {
        if (world == null) world = WorldStateManager.Get();
        world.OnWorldStateChanged += ApplyWorldState;
        ApplyWorldState(world.GetWorldState());
    }

    private void OnDisable()
    {
        if (world != null) world.OnWorldStateChanged -= ApplyWorldState;
    }

    private void LateUpdate()
    {
        WorldState currentState = world.GetWorldState();
        if (!hasAppliedWorldState || currentState != lastAppliedWorldState)
            ApplyWorldState(currentState);

        // Other presentation systems may also update renderers/colliders while
        // a flip settles. Inactive dimensions always win for gameplay, and
        // flat 3D enemies always stay hidden from the 2D view.
        if (!dimensionInteractive)
            for (int i = 0; i < hitboxes.Length; i++)
                if (hitboxes[i] != null) hitboxes[i].enabled = false;

        // Keep fixed-3D presentation authoritative. This also restores the
        // authored renderer state if another presentation update ran after
        // the world-state event.
        if (presence == Presence.Flat3DOnly)
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null)
                    visuals[i].enabled = initialVisualStates[i] && !world.Is2d;
    }

    private void ApplyWorldState(WorldState state)
    {
        lastAppliedWorldState = state;
        hasAppliedWorldState = true;
        bool is2D = state == WorldState.Flat2d;
        dimensionInteractive = presence == Presence.BothDimensions ||
                               (presence == Presence.Flat2DOnly && is2D) ||
                               (presence == Presence.Flat3DOnly && !is2D);

        for (int i = 0; i < hitboxes.Length; i++)
            if (hitboxes[i] != null)
                hitboxes[i].enabled = initialHitboxStates[i] && dimensionInteractive;

        // Flat enemies placed in 3D are depth-only content and must disappear
        // in 2D. Ordinary 2D enemies may remain visible while flipped, but are
        // non-interactive there. 3D models use BothDimensions.
        bool visualsVisible = presence != Presence.Flat3DOnly || !is2D;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].enabled = initialVisualStates[i] && visualsVisible;
    }
}

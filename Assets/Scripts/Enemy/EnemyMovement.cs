using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyDimension))]
[RequireComponent(typeof(EnemyGravity), typeof(EnemyVisualFacing))]
[RequireComponent(typeof(EnemyStun))]
public class EnemyMovement : MonoBehaviour, IEnemyMovementSource
{
    public enum MovementSpace { Flat2D, Volume3D }
    public enum MovementStyle { Idle, Patrol }
    public enum PatrolAxis { LocalX, LocalZ }
    public enum StartingDirection { Left = -1, Right = 1 }

    [Header("Mode")]
    [SerializeField] private MovementSpace movementSpace = MovementSpace.Flat2D;
    [SerializeField] private MovementStyle movementStyle = MovementStyle.Patrol;
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.LocalX;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField] private StartingDirection startingDirection = StartingDirection.Right;
    [SerializeField, Min(0.02f)] private float wallLookAhead = 0.12f;
    [SerializeField, Min(0.05f)] private float ledgeLookAhead = 0.3f;
    [SerializeField, Min(0.05f)] private float groundProbeLength = 1.2f;
    [SerializeField] private LayerMask environmentLayers = 1 << 6;
    [SerializeField] private bool avoidLedges = true;

    private Rigidbody body;
    private Collider hitbox;
    private EnemyHealth health;
    private EnemyDimension dimension;
    private EnemyGravity gravity;
    private EnemyStun stun;
    private int direction;
    private bool movementEnabled = true;
    private Vector3 hitboxCenterOffset;
    private Vector3 hitboxExtents;

    public MovementSpace Space
    {
        get
        {
            if (dimension == null) return movementSpace;
            if (dimension.DimensionPresence == EnemyDimension.Presence.Flat2DOnly)
                return MovementSpace.Flat2D;
            if (dimension.DimensionPresence == EnemyDimension.Presence.Flat3DOnly)
                return MovementSpace.Volume3D;
            return movementSpace;
        }
    }
    public int Direction { get { return direction; } }
    public Vector3 FacingDirection { get { return GetAxis() * direction; } }
    public Vector3 CurrentMoveDirection { get; private set; }
    public Bounds CollisionBounds { get { return GetHitboxBounds(); } }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<Collider>();
        health = GetComponent<EnemyHealth>();
        dimension = GetComponent<EnemyDimension>();
        gravity = GetComponent<EnemyGravity>();
        stun = GetComponent<EnemyStun>();
        direction = (int)startingDirection;
        Bounds initialBounds = hitbox.bounds;
        hitboxCenterOffset = initialBounds.center - body.position;
        hitboxExtents = initialBounds.extents;

        // A kinematic body is a solid obstacle but cannot be shoved by the player.
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (!health.IsAlive || !dimension.CanSimulate)
        {
            CurrentMoveDirection = Vector3.zero;
            gravity.ResetVerticalMotion();
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        if (movementEnabled && !stun.IsStunned &&
            movementStyle == MovementStyle.Patrol && moveSpeed > 0f)
        {
            moveDirection = GetAxis() * direction;
            if (HasWallAhead(moveDirection) || (avoidLedges && !HasGroundAhead(moveDirection)))
            {
                direction = -direction;
                moveDirection = -moveDirection;
            }
        }

        CurrentMoveDirection = moveDirection;
        Vector3 displacement = moveDirection * (moveSpeed * Time.fixedDeltaTime);
        displacement.y = gravity.CalculateVerticalDisplacement(Time.fixedDeltaTime);
        if (displacement.sqrMagnitude > 0f) body.MovePosition(body.position + displacement);
    }

    private Vector3 GetAxis()
    {
        Vector3 axis = patrolAxis == PatrolAxis.LocalZ ? transform.forward : transform.right;
        axis.y = 0f;

        if (Space == MovementSpace.Flat2D)
        {
            // Flat enemies always patrol in screen X and retain their placed depth.
            axis = Vector3.right * (Vector3.Dot(axis, Vector3.right) < 0f ? -1f : 1f);
        }

        return axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
    }

    private bool HasWallAhead(Vector3 moveDirection)
    {
        Bounds bounds = GetHitboxBounds();
        Vector3 origin = bounds.center;
        origin.y = bounds.min.y + bounds.size.y * 0.55f;
        float distance = GetHorizontalExtent(bounds, moveDirection) + wallLookAhead;

        RaycastHit[] hits = Physics.RaycastAll(origin, moveDirection, distance, environmentLayers,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
            if (hit.collider != hitbox && hit.collider.attachedRigidbody != body) return true;
        return false;
    }

    private bool HasGroundAhead(Vector3 moveDirection)
    {
        Bounds bounds = GetHitboxBounds();
        float extent = GetHorizontalExtent(bounds, moveDirection);
        Vector3 origin = bounds.center + moveDirection * (extent + ledgeLookAhead);
        origin.y = bounds.min.y + 0.08f;

        return Physics.Raycast(origin, Vector3.down, groundProbeLength + 0.08f, environmentLayers,
            QueryTriggerInteraction.Ignore);
    }

    private static float GetHorizontalExtent(Bounds bounds, Vector3 directionVector)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(directionVector.x), 0f, Mathf.Abs(directionVector.z));
        return Vector3.Dot(bounds.extents, absolute);
    }

    private Bounds GetHitboxBounds()
    {
        if (hitbox.enabled)
        {
            Bounds current = hitbox.bounds;
            hitboxCenterOffset = current.center - body.position;
            hitboxExtents = current.extents;
        }
        return new Bounds(body.position + hitboxCenterOffset, hitboxExtents * 2f);
    }

    public void Reverse() { direction = -direction; }
    public void SetMovementEnabled(bool enabled) { movementEnabled = enabled; }
    public void SetVisualDirection(Vector3 moveDirection) { CurrentMoveDirection = moveDirection; }
}

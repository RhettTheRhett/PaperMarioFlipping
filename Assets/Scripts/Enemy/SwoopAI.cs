using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyDimension), typeof(EnemyGravity))]
[RequireComponent(typeof(EnemyStun))]
public class SwoopAI : MonoBehaviour, IEnemyMovementSource
{
    private enum State { Waiting, Circling, Diving, Returning, Cooldown }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 5f;
    [SerializeField, Min(0f)] private float repeatDelay = 1.5f;
    [SerializeField] private LayerMask obstacleLayers = 1 << 6;
    [SerializeField] private bool showDetectionRange = true;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float patrolDistance = 1.5f;
    [SerializeField, Min(0f)] private float patrolSpeed = 1.2f;

    [Header("Warning Circle")]
    [SerializeField, Min(0.05f)] private float circleRadius = 0.35f;
    [SerializeField, Min(0.1f)] private float circleDuration = 1f;

    [Header("Swoop")]
    [SerializeField, Min(0.1f)] private float diveDuration = 1.25f;
    [SerializeField, Min(0.1f)] private float returnDuration = 1.5f;
    [SerializeField, Min(0f)] private float arcHeight = 0.75f;

    private Rigidbody body;
    private Collider hitbox;
    private EnemyHealth health;
    private EnemyDimension dimension;
    private EnemyStun stun;
    private Transform player;
    private Vector3 homePosition;
    private Vector3 attackHome;
    private Vector3 attackStart;
    private Vector3 attackTarget;
    private Vector3 attackControl;
    private State state;
    private float stateTime;
    private int patrolDirection = 1;
    private Vector3 hitboxCenterOffset;
    private Vector3 hitboxExtents;

    public Vector3 CurrentMoveDirection { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<Collider>();
        health = GetComponent<EnemyHealth>();
        dimension = GetComponent<EnemyDimension>();
        stun = GetComponent<EnemyStun>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        Bounds initialBounds = hitbox.bounds;
        hitboxCenterOffset = initialBounds.center - body.position;
        hitboxExtents = initialBounds.extents;
    }

    private void Start()
    {
        homePosition = body.position;
        FindPlayer();
    }

    private void FixedUpdate()
    {
        if (!health.IsAlive || !dimension.CanSimulate || stun.IsStunned)
        {
            CurrentMoveDirection = Vector3.zero;
            return;
        }
        if (player == null) FindPlayer();

        stateTime += Time.fixedDeltaTime;
        Vector3 nextPosition = body.position;

        switch (state)
        {
            case State.Waiting:
                if (CanSeePlayer())
                {
                    attackHome = body.position;
                    ChangeState(State.Circling);
                    return;
                }
                nextPosition = PatrolPosition();
                break;

            case State.Circling:
                nextPosition = CirclePosition(Mathf.Clamp01(stateTime / circleDuration));
                if (stateTime >= circleDuration && !BeginDive())
                    ChangeState(State.Waiting);
                break;

            case State.Diving:
                nextPosition = QuadraticBezier(attackStart, attackControl, attackTarget,
                    SmoothProgress(stateTime / diveDuration));
                if (stateTime >= diveDuration) BeginReturn(nextPosition);
                break;

            case State.Returning:
                nextPosition = QuadraticBezier(attackStart, attackControl, attackHome,
                    SmoothProgress(stateTime / returnDuration));
                if (stateTime >= returnDuration)
                {
                    nextPosition = attackHome;
                    ChangeState(State.Cooldown);
                }
                break;

            case State.Cooldown:
                if (stateTime >= repeatDelay) ChangeState(State.Waiting);
                nextPosition = PatrolPosition();
                break;
        }

        bool blocked;
        nextPosition = GetUnobstructedPosition(nextPosition, out blocked);
        if (blocked) HandleBlockedMovement(nextPosition);

        CurrentMoveDirection = (nextPosition - body.position) / Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
    }

    private bool BeginDive()
    {
        if (!CanSeePlayer()) return false;

        Vector3 planeAxis = GetHorizontalPlaneAxis();
        attackStart = attackHome;
        Vector3 toPlayer = player != null ? player.position - attackHome : planeAxis * detectionRange;
        attackTarget = attackHome + planeAxis * Vector3.Dot(toPlayer, planeAxis);
        attackTarget.y = player != null ? player.position.y : attackHome.y - 1f;
        attackControl = Vector3.Lerp(attackStart, attackTarget, 0.5f) + Vector3.up * arcHeight;
        ChangeState(State.Diving);
        return true;
    }

    private void BeginReturn(Vector3 currentPosition)
    {
        attackStart = currentPosition;
        attackControl = Vector3.Lerp(attackStart, attackHome, 0.5f) + Vector3.up * arcHeight;
        ChangeState(State.Returning);
    }

    private Vector3 CirclePosition(float progress)
    {
        float angle = progress * Mathf.PI * 2f;
        Vector3 sideways = GetHorizontalPlaneAxis() * (Mathf.Sin(angle) * circleRadius);
        Vector3 vertical = Vector3.up * ((1f - Mathf.Cos(angle)) * circleRadius);
        return attackHome + sideways + vertical;
    }

    private Vector3 PatrolPosition()
    {
        Vector3 axis = GetHorizontalPlaneAxis();
        float offset = Vector3.Dot(body.position - homePosition, axis);
        if (offset >= patrolDistance) patrolDirection = -1;
        else if (offset <= -patrolDistance) patrolDirection = 1;

        float nextOffset = Mathf.Clamp(offset + patrolDirection * patrolSpeed * Time.fixedDeltaTime,
            -patrolDistance, patrolDistance);
        Vector3 position = homePosition + axis * nextOffset;
        position.y = homePosition.y;
        return position;
    }

    private Vector3 GetHorizontalPlaneAxis()
    {
        if (dimension.DimensionPresence != EnemyDimension.Presence.Flat3DOnly) return Vector3.right;
        Vector3 axis = transform.right;
        axis.y = 0f;
        return axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.forward;
    }

    private void FindPlayer()
    {
        PlayerHealth found = FindObjectOfType<PlayerHealth>();
        player = found != null ? found.transform : null;
    }

    private bool CanSeePlayer()
    {
        if (player == null || Vector3.Distance(body.position, player.position) > detectionRange)
            return false;

        Vector3 origin = GetHitboxBounds().center;
        Collider playerCollider = player.GetComponent<Collider>();
        Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position;
        return !HasSolidObstacle(origin, target);
    }

    private Vector3 GetUnobstructedPosition(Vector3 desiredPosition, out bool blocked)
    {
        Vector3 movement = desiredPosition - body.position;
        float distance = movement.magnitude;
        blocked = false;
        if (distance < 0.0001f) return desiredPosition;

        Bounds bounds = GetHitboxBounds();
        float nearest = distance;
        foreach (RaycastHit hit in Physics.BoxCastAll(bounds.center, bounds.extents * 0.95f,
                     movement / distance, transform.rotation, distance + 0.02f, obstacleLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.attachedRigidbody == body || IsIgnoredObstacle(hit.collider)) continue;
            nearest = Mathf.Min(nearest, Mathf.Max(0f, hit.distance - 0.02f));
            blocked = true;
        }

        return body.position + movement.normalized * nearest;
    }

    private bool HasSolidObstacle(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance < 0.0001f) return false;

        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction / distance, distance,
                     obstacleLayers, QueryTriggerInteraction.Ignore))
            if (!IsIgnoredObstacle(hit.collider)) return true;
        return false;
    }

    private static bool IsIgnoredObstacle(Collider candidate)
    {
        if (candidate.GetComponentInParent<EnemyHealth>() != null) return true;
        if (candidate.GetComponentInParent<PlayerHealth>() != null) return true;
        return candidate.CompareTag("Coin") || candidate.CompareTag("CoinFlat") ||
               candidate.CompareTag("CoinFlipped");
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

    private void HandleBlockedMovement(Vector3 safePosition)
    {
        if (state == State.Waiting || state == State.Cooldown)
        {
            patrolDirection = -patrolDirection;
            return;
        }

        if (state == State.Circling || state == State.Diving)
        {
            BeginReturn(safePosition);
            return;
        }

        // If the route home changed while returning, adopt the safe point
        // rather than repeatedly trying to pass through the obstacle.
        attackHome = safePosition;
        homePosition = new Vector3(safePosition.x, homePosition.y, safePosition.z);
        ChangeState(State.Cooldown);
    }

    private void OnDrawGizmos()
    {
        if (!showDetectionRange) return;

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 axis = Application.isPlaying && dimension != null
            ? GetHorizontalPlaneAxis()
            : transform.right.normalized;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
        Gizmos.DrawLine(center - axis * patrolDistance, center + axis * patrolDistance);
        Gizmos.DrawWireSphere(center - axis * patrolDistance, 0.08f);
        Gizmos.DrawWireSphere(center + axis * patrolDistance, 0.08f);

        PlayerHealth found = Application.isPlaying ? FindObjectOfType<PlayerHealth>() : null;
        if (found == null) return;
        if (Vector3.Distance(transform.position, found.transform.position) > detectionRange) return;

        bool blocked = HasSolidObstacle(transform.position, found.transform.position);
        Gizmos.color = blocked ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, found.transform.position);
    }

    private void ChangeState(State next)
    {
        state = next;
        stateTime = 0f;
    }

    private static float SmoothProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        return progress * progress * (3f - 2f * progress);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float progress)
    {
        float remaining = 1f - progress;
        return remaining * remaining * start + 2f * remaining * progress * control +
               progress * progress * end;
    }
}

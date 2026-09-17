using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyGravity), typeof(EnemyDimension), typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyStun))]
public class CleftAI : MonoBehaviour
{
    private enum State { Patrolling, Hopping, Dashing, Skidding }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float attackRange = 4f;
    [SerializeField, Range(-1f, 1f)] private float frontDotThreshold = 0.25f;
    [SerializeField, Min(0f)] private float maximumAttackHeightDifference = 1.25f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;
    [SerializeField] private LayerMask obstacleLayers = 1 << 6;

    [Header("Hop")]
    [SerializeField, Min(0f)] private float hopSpeed = 3.5f;
    [SerializeField, Min(0.05f)] private float hopDuration = 0.3f;

    [Header("Dash")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 5f;
    [SerializeField, Min(0.1f)] private float maximumDashDuration = 1f;
    [SerializeField, Min(0.05f)] private float skidDuration = 0.4f;

    private Rigidbody body;
    private Collider hitbox;
    private EnemyMovement movement;
    private EnemyGravity gravity;
    private EnemyDimension dimension;
    private EnemyHealth health;
    private EnemyStun stun;
    private Transform player;
    private State state;
    private float stateTime;
    private float skidSpeed;
    private float nextAttackTime;
    private Vector3 dashDirection;
    private Vector3 dashTarget;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<Collider>();
        movement = GetComponent<EnemyMovement>();
        gravity = GetComponent<EnemyGravity>();
        dimension = GetComponent<EnemyDimension>();
        health = GetComponent<EnemyHealth>();
        stun = GetComponent<EnemyStun>();
    }

    private void Start() { FindPlayer(); }

    private void FixedUpdate()
    {
        if (!health.IsAlive || !dimension.CanSimulate)
        {
            movement.SetVisualDirection(Vector3.zero);
            gravity.ResetVerticalMotion();
            return;
        }
        if (player == null) FindPlayer();

        if (stun.IsStunned)
        {
            movement.SetVisualDirection(Vector3.zero);
            if (!movement.enabled) MoveSpecial(Vector3.up *
                gravity.CalculateVerticalDisplacement(Time.fixedDeltaTime));
            return;
        }

        if (state == State.Patrolling)
        {
            if (CanAttackPlayer()) BeginHop();
            return;
        }

        stateTime += Time.fixedDeltaTime;
        Vector3 displacement = Vector3.up * gravity.CalculateVerticalDisplacement(Time.fixedDeltaTime);

        if (state == State.Hopping)
        {
            if (stateTime >= hopDuration && gravity.IsGrounded) BeginDash();
        }
        else if (state == State.Dashing)
        {
            displacement += dashDirection * (dashSpeed * Time.fixedDeltaTime);
            bool passedPlayer = Vector3.Dot(body.position - dashTarget, dashDirection) >= 0f;
            if (passedPlayer || stateTime >= maximumDashDuration) BeginSkid();
        }
        else
        {
            skidSpeed = Mathf.MoveTowards(skidSpeed, 0f,
                dashSpeed / skidDuration * Time.fixedDeltaTime);
            displacement += dashDirection * (skidSpeed * Time.fixedDeltaTime);
            if (skidSpeed <= 0.01f)
            {
                ResumePatrol(false);
                return;
            }
        }

        movement.SetVisualDirection(displacement);
        if (!MoveSpecial(displacement)) ResumePatrol(true);
    }

    private void BeginHop()
    {
        state = State.Hopping;
        stateTime = 0f;
        dashDirection = movement.CurrentMoveDirection.sqrMagnitude > 0.001f
            ? movement.CurrentMoveDirection.normalized
            : transform.right;
        dashDirection.y = 0f;
        gravity.SetVerticalSpeed(hopSpeed);
        movement.enabled = false;
    }

    private void BeginDash()
    {
        state = State.Dashing;
        stateTime = 0f;
        Vector3 toPlayer = player != null ? player.position - body.position : dashDirection;
        toPlayer.y = 0f;
        if (dimension.DimensionPresence == EnemyDimension.Presence.Flat2DOnly) toPlayer.z = 0f;
        if (toPlayer.sqrMagnitude > 0.001f) dashDirection = toPlayer.normalized;
        dashTarget = player != null ? player.position : body.position + dashDirection * attackRange;
        movement.SetVisualDirection(dashDirection);
    }

    private void BeginSkid()
    {
        state = State.Skidding;
        stateTime = 0f;
        skidSpeed = dashSpeed;
    }

    private void ResumePatrol(bool reverse)
    {
        if (reverse) movement.Reverse();
        state = State.Patrolling;
        stateTime = 0f;
        skidSpeed = 0f;
        nextAttackTime = Time.time + attackCooldown;
        movement.enabled = true;
    }

    private bool CanAttackPlayer()
    {
        if (player == null || Time.time < nextAttackTime) return false;

        Vector3 toPlayer = player.position - body.position;
        if (toPlayer.magnitude > attackRange) return false;
        if (Mathf.Abs(toPlayer.y) > maximumAttackHeightDifference) return false;

        Vector3 facing = movement.CurrentMoveDirection;
        if (facing.sqrMagnitude < 0.001f) return false;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f ||
            Vector3.Dot(facing.normalized, toPlayer.normalized) < frontDotThreshold) return false;

        return !HasSolidObstacle(movement.CollisionBounds.center, player.position);
    }

    private bool MoveSpecial(Vector3 displacement)
    {
        float distance = displacement.magnitude;
        if (distance < 0.0001f) return true;

        float nearest = distance;
        bool blocked = false;
        Bounds bounds = movement.CollisionBounds;
        foreach (RaycastHit hit in Physics.BoxCastAll(bounds.center,
                     bounds.extents * 0.95f, displacement / distance, transform.rotation,
                     distance + 0.02f, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.attachedRigidbody == body || IsIgnoredObstacle(hit.collider)) continue;
            if (hit.normal.y > 0.6f && Mathf.Abs(displacement.x) + Mathf.Abs(displacement.z) > 0f)
                continue;
            nearest = Mathf.Min(nearest, Mathf.Max(0f, hit.distance - 0.02f));
            blocked = true;
        }

        body.MovePosition(body.position + displacement.normalized * nearest);
        return !blocked;
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

    private void OnCollisionEnter(Collision collision) { HandlePlayerCollision(collision); }
    private void OnCollisionStay(Collision collision) { HandlePlayerCollision(collision); }

    private void HandlePlayerCollision(Collision collision)
    {
        if (state == State.Patrolling) return;
        if (collision.collider.GetComponentInParent<PlayerHealth>() != null)
            ResumePatrol(state == State.Dashing);
    }

    private void FindPlayer()
    {
        PlayerHealth found = FindObjectOfType<PlayerHealth>();
        player = found != null ? found.transform : null;
    }
}

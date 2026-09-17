using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth), typeof(EnemyDimension))]
[RequireComponent(typeof(EnemyStun), typeof(EnemyAnimator))]
[RequireComponent(typeof(EnemyContactCombat))]
public class SquigletAI : MonoBehaviour
{
    private enum State { Patrol, Startled, Shooting }

    [Header("Detection Box")]
    [SerializeField, Min(0.1f)] private float detectionDistance = 5f;
    [SerializeField, Min(0.1f)] private float detectionHeight = 1.5f;
    [SerializeField, Min(0.1f)] private float detectionDepth = 1.5f;
    [SerializeField] private bool showDetectionRange = true;

    [Header("Attack Timing")]
    [SerializeField, Min(0f)] private float startleDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float timeBetweenShots = 1.5f;

    [Header("Projectile")]
    [SerializeField] private CoconutProjectile coconutPrefab;
    [SerializeField] private Transform mouthPoint;
    [Tooltip("Enable after adding ReleaseCoconut as an event in the shoot animation.")]
    [SerializeField] private bool releaseFromAnimationEvent;

    private EnemyMovement movement;
    private EnemyHealth health;
    private EnemyDimension dimension;
    private EnemyStun stun;
    private EnemyAnimator enemyAnimator;
    private Collider hitbox;
    private PlayerHealth player;
    private State state;
    private float timer;

#if UNITY_EDITOR
    private void Reset()
    {
        coconutPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CoconutProjectile>(
            "Assets/PREFABS/CoconutProjectile.prefab");
    }
#endif

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
        dimension = GetComponent<EnemyDimension>();
        stun = GetComponent<EnemyStun>();
        enemyAnimator = GetComponent<EnemyAnimator>();
        hitbox = GetComponent<Collider>();
    }

    private void Start()
    {
        player = FindObjectOfType<PlayerHealth>();
    }

    private void OnDisable()
    {
        if (movement != null) movement.SetMovementEnabled(true);
    }

    private void Update()
    {
        if (!health.IsAlive) return;
        if (!dimension.IsInteractive)
        {
            if (state != State.Patrol) ResumePatrol();
            return;
        }
        if (player == null) player = FindObjectOfType<PlayerHealth>();
        if (player == null || !player.IsAlive) return;
        if (stun.IsStunned) return;

        switch (state)
        {
            case State.Patrol:
                movement.SetMovementEnabled(true);
                if (IsPlayerInFront()) BeginStartle();
                break;

            case State.Startled:
                movement.SetMovementEnabled(false);
                timer -= Time.deltaTime;
                if (timer > 0f) break;

                if (IsPlayerInFront())
                {
                    state = State.Shooting;
                    Fire();
                }
                else
                {
                    ResumePatrol();
                }
                break;

            case State.Shooting:
                movement.SetMovementEnabled(false);
                if (!IsPlayerInFront())
                {
                    ResumePatrol();
                    break;
                }

                timer -= Time.deltaTime;
                if (timer <= 0f) Fire();
                break;
        }
    }

    private void BeginStartle()
    {
        state = State.Startled;
        timer = startleDuration;
        movement.SetMovementEnabled(false);
        enemyAnimator.PlayStartle();
    }

    private void Fire()
    {
        timer = timeBetweenShots;
        enemyAnimator.PlayAttack();
        if (!releaseFromAnimationEvent) ReleaseCoconut();
    }

    // Add an Animation Event with this exact function name to choose the
    // frame on which the coconut leaves Squiglet's mouth.
    public void ReleaseCoconut()
    {
        if (coconutPrefab == null)
        {
            Debug.LogWarning("Squiglet needs a Coconut Projectile prefab.", this);
            return;
        }

        Vector3 direction = GetAttackDirection();
        Vector3 position = mouthPoint != null ? mouthPoint.position : GetFallbackMouthPosition(direction);
        CoconutProjectile coconut = Instantiate(coconutPrefab, position, Quaternion.identity);
        coconut.Launch(direction, gameObject);
    }

    private void ResumePatrol()
    {
        state = State.Patrol;
        movement.SetMovementEnabled(true);
    }

    private bool IsPlayerInFront()
    {
        Vector3 direction = GetAttackDirection();
        Bounds playerBounds = GetPlayerBounds();
        Vector3 origin = hitbox != null ? hitbox.bounds.center : transform.position;
        Vector3 offset = playerBounds.center - origin;

        float forwardDistance = Vector3.Dot(offset, direction);
        if (forwardDistance < 0f || forwardDistance > detectionDistance) return false;
        if (Mathf.Abs(offset.y) > detectionHeight * 0.5f + playerBounds.extents.y) return false;

        Vector3 sideways = Vector3.Cross(Vector3.up, direction).normalized;
        return Mathf.Abs(Vector3.Dot(offset, sideways)) <=
               detectionDepth * 0.5f + Vector3.Dot(playerBounds.extents, Abs(sideways));
    }

    private Vector3 GetAttackDirection()
    {
        Vector3 direction = movement.FacingDirection;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.right;
    }

    private Bounds GetPlayerBounds()
    {
        Collider playerCollider = player.GetComponent<Collider>();
        return playerCollider != null
            ? playerCollider.bounds
            : new Bounds(player.transform.position, Vector3.zero);
    }

    private Vector3 GetFallbackMouthPosition(Vector3 direction)
    {
        if (hitbox == null) return transform.position + direction * 0.5f;
        Bounds bounds = hitbox.bounds;
        float horizontalExtent = Vector3.Dot(bounds.extents, Abs(direction));
        return bounds.center + direction * (horizontalExtent + 0.1f) + Vector3.up * bounds.extents.y * 0.15f;
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDetectionRange) return;

        EnemyMovement foundMovement = Application.isPlaying ? movement : GetComponent<EnemyMovement>();
        Collider foundHitbox = Application.isPlaying ? hitbox : GetComponent<Collider>();
        Vector3 direction = foundMovement != null ? foundMovement.FacingDirection : transform.right;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = transform.right;
        direction.Normalize();

        Vector3 origin = foundHitbox != null ? foundHitbox.bounds.center : transform.position;
        Vector3 center = origin + direction * (detectionDistance * 0.5f);
        Vector3 size = Mathf.Abs(direction.x) >= Mathf.Abs(direction.z)
            ? new Vector3(detectionDistance, detectionHeight, detectionDepth)
            : new Vector3(detectionDepth, detectionHeight, detectionDistance);

        Gizmos.color = state == State.Shooting ? Color.red : new Color(1f, 0.65f, 0f);
        Gizmos.DrawWireCube(center, size);

        Vector3 mouth = mouthPoint != null ? mouthPoint.position : GetFallbackMouthPosition(direction);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(mouth, 0.08f);
    }
}

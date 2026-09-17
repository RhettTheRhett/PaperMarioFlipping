using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(SpriteRenderer))]
public class CoconutProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 3f;
    [SerializeField, Min(0.1f)] private float lifetime = 8f;
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float knockbackForce = 5f;
    [SerializeField, Min(0f)] private float knockbackUpwardForce = 2f;
    [SerializeField, Min(0f)] private float movementLockTime = 0.2f;
    [SerializeField, Min(0f)] private float stompBounceSpeed = 7f;
    [SerializeField, Min(0f)] private float stompHeightTolerance = 0.08f;
    [SerializeField] private LayerMask environmentLayers = 1 << 6;

    private Rigidbody body;
    private Vector3 direction;
    private GameObject owner;
    private EnemyDimension ownerDimension;
    private float expiresAt;
    private bool consumed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Launch(Vector3 launchDirection, GameObject projectileOwner)
    {
        direction = launchDirection.normalized;
        owner = projectileOwner;
        ownerDimension = owner != null ? owner.GetComponent<EnemyDimension>() : null;
        expiresAt = Time.time + lifetime;

        if (owner == null) return;
        Collider projectileCollider = GetComponent<Collider>();
        foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(projectileCollider, ownerCollider);
    }

    private void FixedUpdate()
    {
        if (consumed) return;
        if (Time.time >= expiresAt || (ownerDimension != null && !ownerDimension.IsInteractive))
        {
            Destroy(gameObject);
            return;
        }

        body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed || (owner != null && other.transform.IsChildOf(owner.transform))) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            consumed = true;
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            Collider projectileCollider = GetComponent<Collider>();
            bool stomped = playerBody != null &&
                           playerBody.velocity.y < -0.1f &&
                           other.bounds.min.y >=
                           projectileCollider.bounds.max.y - stompHeightTolerance;

            if (stomped)
            {
                Vector3 velocity = playerBody.velocity;
                velocity.y = Mathf.Max(velocity.y, stompBounceSpeed);
                playerBody.velocity = velocity;
            }
            else
            {
                player.TakeHit(damage, transform.position, knockbackForce,
                    knockbackUpwardForce, movementLockTime);
            }

            Destroy(gameObject);
            return;
        }

        if (other.GetComponentInParent<EnemyHealth>() != null) return;
        if ((environmentLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }
}

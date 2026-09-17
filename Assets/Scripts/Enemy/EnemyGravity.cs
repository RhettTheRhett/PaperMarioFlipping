using UnityEngine;

public class EnemyGravity : MonoBehaviour
{
    [SerializeField] private bool gravityEnabled = true;
    [SerializeField, Min(0f)] private float gravityScale = 1f;
    [SerializeField, Min(0f)] private float maximumFallSpeed = 20f;
    [SerializeField, Min(0f)] private float groundSkin = 0.03f;
    [SerializeField] private LayerMask groundLayers = 1 << 6;

    private Rigidbody body;
    private Collider hitbox;
    private EnemyDimension dimension;
    private float verticalSpeed;
    private Vector3 hitboxCenterOffset;
    private Vector3 hitboxExtents;

    public bool GravityEnabled { get { return gravityEnabled; } }
    public float VerticalSpeed { get { return verticalSpeed; } }
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<Collider>();
        dimension = GetComponent<EnemyDimension>();
        if (hitbox != null && body != null)
        {
            Bounds initialBounds = hitbox.bounds;
            hitboxCenterOffset = initialBounds.center - body.position;
            hitboxExtents = initialBounds.extents;
        }
    }

    private void FixedUpdate()
    {
        if (!CanSimulate()) ResetVerticalMotion();
    }

    private void OnDisable()
    {
        ResetVerticalMotion();
    }

    public float CalculateVerticalDisplacement(float deltaTime)
    {
        if (!CanSimulate())
        {
            ResetVerticalMotion();
            return 0f;
        }

        verticalSpeed = Mathf.Max(verticalSpeed + Physics.gravity.y * gravityScale * deltaTime,
            -maximumFallSpeed);
        float fallDistance = Mathf.Max(0f, -verticalSpeed * deltaTime);
        if (fallDistance <= 0f)
        {
            IsGrounded = false;
            return verticalSpeed * deltaTime;
        }

        float groundDistance;
        if (TryGetGroundDistance(fallDistance + groundSkin, out groundDistance))
        {
            verticalSpeed = 0f;
            IsGrounded = true;
            return -Mathf.Max(0f, groundDistance);
        }

        IsGrounded = false;
        return -fallDistance;
    }

    public void SetVerticalSpeed(float speed)
    {
        verticalSpeed = CanSimulate() ? speed : 0f;
        if (verticalSpeed > 0f) IsGrounded = false;
    }

    public void ResetVerticalMotion()
    {
        verticalSpeed = 0f;
        IsGrounded = false;
    }

    private bool CanSimulate()
    {
        return gravityEnabled && body != null && hitbox != null &&
               (dimension == null || dimension.CanSimulate);
    }

    private bool TryGetGroundDistance(float maximumDistance, out float groundDistance)
    {
        Bounds bounds = GetHitboxBounds();
        Vector3 origin = bounds.center;
        float rayLength = bounds.extents.y + maximumDistance;
        float nearest = float.PositiveInfinity;

        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, rayLength, groundLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == hitbox || hit.collider.attachedRigidbody == body) continue;
            float distanceBelowFeet = Mathf.Max(0f, hit.distance - bounds.extents.y);
            if (distanceBelowFeet < nearest) nearest = distanceBelowFeet;
        }

        groundDistance = nearest;
        return nearest <= maximumDistance;
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
}

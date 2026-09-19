using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class PlayerBounce : MonoBehaviour
{
    private Rigidbody body;
    private Vector3 horizontalVelocity;
    private float duration;
    private float timeRemaining;

    public static void Apply(Rigidbody body, Vector3 sourcePosition, float upwardSpeed,
        float horizontalPush, float horizontalDuration)
    {
        if (body == null) return;

        PlayerStateManager movement = body.GetComponent<PlayerStateManager>();
        Vector3 velocity = body.velocity;
        Vector3 away = body.worldCenterOfMass - sourcePosition;
        away.y = 0f;
        if (movement != null && movement.is2d) away.z = 0f;

        // A perfectly centered landing keeps pushing in the player's current
        // travel direction instead of arbitrarily choosing left or right.
        if (away.sqrMagnitude < 0.0001f)
        {
            away = new Vector3(velocity.x, 0f,
                movement != null && movement.is2d ? 0f : velocity.z);
        }

        velocity.y = Mathf.Max(velocity.y, Mathf.Max(0f, upwardSpeed));
        body.velocity = velocity;

        PlayerBounce bounce = body.GetComponent<PlayerBounce>();
        if (bounce == null) bounce = body.gameObject.AddComponent<PlayerBounce>();
        bounce.Begin(body, away, horizontalPush, horizontalDuration);
    }

    private void Begin(Rigidbody targetBody, Vector3 direction, float speed, float seconds)
    {
        body = targetBody;
        horizontalVelocity = direction.sqrMagnitude > 0.0001f
            ? direction.normalized * Mathf.Max(0f, speed)
            : Vector3.zero;
        duration = Mathf.Max(Time.fixedDeltaTime, seconds);
        timeRemaining = seconds > 0f ? duration : 0f;
    }

    private void FixedUpdate()
    {
        if (body == null || timeRemaining <= 0f) return;

        // Player movement writes its normal controllable velocity first.
        // Add a smooth influence afterward instead of replacing or locking it.
        float normalizedTime = Mathf.Clamp01(timeRemaining / duration);
        float strength = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
        Vector3 velocity = body.velocity;
        velocity.x += horizontalVelocity.x * strength;
        velocity.z += horizontalVelocity.z * strength;
        body.velocity = velocity;

        timeRemaining -= Time.fixedDeltaTime;
    }
}

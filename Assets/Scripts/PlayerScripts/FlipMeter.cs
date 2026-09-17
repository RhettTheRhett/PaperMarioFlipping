using System;
using UnityEngine;

// The 3D timer: it drains while flipped, refills while flat, and costs a heart
// when it empties instead of forcing the player back to 2D.
public class FlipMeter : MonoBehaviour
{
    [Header("Capacity")]
    public int segments = 5;
    public float secondsPerSegment = 2f;
    [Tooltip("Real seconds needed to refill one segment while flat.")]
    public float refillSecondsPerSegment = 1f;

    [Header("Rules")]
    [Tooltip("Never drains, the way it behaves once Mario has his own Catch Card.")]
    public bool unlimited = false;
    public int damageWhenEmpty = 1;
    [Tooltip("Refuse a flip into 3D while the meter is empty. Off matches the real game.")]
    public bool blockFlipWhenEmpty = false;

    [Header("Debug")]
    public bool showDebugMeter = true;

    private WorldStateManager worldStateManager;
    private PlayerHealth health;
    private bool subscribed;

    public float Seconds { get; private set; }

    public event Action OnEmptied;

    public float Capacity { get { return Mathf.Max(0.01f, segments * secondsPerSegment); } }
    public float Normalized { get { return Mathf.Clamp01(Seconds / Capacity); } }
    public int SegmentsRemaining { get { return Mathf.CeilToInt(Normalized * segments); } }
    public bool IsEmpty { get { return Seconds <= 0f; } }
    public bool CanFlipTo3d { get { return unlimited || !blockFlipWhenEmpty || !IsEmpty; } }

    private void Awake()
    {
        Seconds = Capacity;
        worldStateManager = WorldStateManager.Get();
    }

    private void OnEnable()
    {
        EnsureHealth();
    }

    private void OnDisable()
    {
        if (health != null && subscribed) health.OnDamaged -= HandleDamaged;
        subscribed = false;
    }

    // The health component may be added after this one, so keep looking until
    // it shows up rather than silently never dealing the empty-meter damage.
    private void EnsureHealth()
    {
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (health == null || subscribed) return;

        health.OnDamaged += HandleDamaged;
        subscribed = true;
    }

    private void Update()
    {
        if (worldStateManager == null) return;
        EnsureHealth();

        if (worldStateManager.worldState == WorldState.Flipped3d)
        {
            if (unlimited) return;

            Seconds -= Time.deltaTime;
            if (Seconds > 0f) return;

            Seconds = Capacity;
            if (health != null) health.TakeDamage(damageWhenEmpty, true);
            if (OnEmptied != null) OnEmptied();
            return;
        }

        float refillPerSecond = secondsPerSegment / Mathf.Max(0.01f, refillSecondsPerSegment);
        Seconds = Mathf.Min(Capacity, Seconds + refillPerSecond * Time.deltaTime);
    }

    public void Refill()
    {
        Seconds = Capacity;
    }

    private void HandleDamaged(int amount, int remaining)
    {
        Refill();
    }

    private void OnGUI()
    {
        if (!showDebugMeter) return;
        if (worldStateManager == null) return;
        if (worldStateManager.worldState == WorldState.Flat2d && Normalized >= 1f) return;

        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.Box(new Rect(10f, 10f, 204f, 20f), GUIContent.none);

        GUI.color = Normalized > 0.34f ? new Color(0.3f, 0.8f, 1f, 0.9f) : new Color(1f, 0.4f, 0.3f, 0.9f);
        GUI.Box(new Rect(12f, 12f, 200f * Normalized, 16f), GUIContent.none);

        GUI.color = previous;
    }
}

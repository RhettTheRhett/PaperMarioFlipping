using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] public int maxHp = 10;
    [SerializeField, Min(0)] public int hp = 10;
    [SerializeField, Min(0)] private int defense;
    [Tooltip("Seconds of immunity after any hit.")]
    [SerializeField, Min(0f)] public float invulnerabilityTime = 1f;

    private float invulnerableUntil;

    public event Action<int, int> OnDamaged;
    public event Action<int, int> OnHealed;
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDefenseChanged;
    public event Action OnDied;

    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
    public bool IsAlive { get { return hp > 0; } }
    public int CurrentHealth { get { return hp; } }
    public int MaxHealth { get { return maxHp; } }
    public int Defense { get { return defense; } }

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        hp = Mathf.Clamp(hp <= 0 ? maxHp : hp, 0, maxHp);
    }

    public bool TakeDamage(int amount, bool ignoreInvulnerability = false)
    {
        if (amount <= 0 || !IsAlive) return false;
        if (!ignoreInvulnerability && IsInvulnerable) return false;

        int appliedDamage = Mathf.Max(1, amount - defense);
        hp = Mathf.Max(0, hp - appliedDamage);
        invulnerableUntil = Time.time + invulnerabilityTime;

        if (OnDamaged != null) OnDamaged(appliedDamage, hp);
        if (OnHealthChanged != null) OnHealthChanged(hp, maxHp);
        if (hp == 0 && OnDied != null) OnDied();
        return true;
    }

    public bool TakeHit(int amount, Vector3 sourcePosition, float knockbackForce, float upwardForce,
        float movementLockTime)
    {
        if (!TakeDamage(amount)) return false;

        Rigidbody body = GetComponent<Rigidbody>();
        PlayerStateManager movement = GetComponent<PlayerStateManager>();
        if (body != null)
        {
            Vector3 away = body.worldCenterOfMass - sourcePosition;
            away.y = 0f;
            if (movement != null && movement.is2d) away.z = 0f;
            if (away.sqrMagnitude < 0.001f)
                away = movement != null && !movement.is2d ? -transform.forward : -transform.right;

            Vector3 knockback = away.normalized * Mathf.Max(0f, knockbackForce);
            knockback.y = Mathf.Max(body.velocity.y, Mathf.Max(0f, upwardForce));
            body.velocity = knockback;
        }

        if (movement != null) movement.LockMovement(movementLockTime);
        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int before = hp;
        hp = Mathf.Min(maxHp, hp + amount);
        if (hp == before) return;
        if (OnHealed != null) OnHealed(hp - before, hp);
        if (OnHealthChanged != null) OnHealthChanged(hp, maxHp);
    }

    public void RestoreToFull()
    {
        int before = hp;
        hp = maxHp;
        if (hp == before) return;
        if (OnHealed != null) OnHealed(hp - before, hp);
        if (OnHealthChanged != null) OnHealthChanged(hp, maxHp);
    }

    public void UpgradeDefense(int amount)
    {
        if (amount <= 0) return;
        defense += amount;
        if (OnDefenseChanged != null) OnDefenseChanged(defense);
    }
}

using System;
using UnityEngine;

public interface IDamageable
{
    bool TakeDamage(int amount);
}

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0)] private int defense;
    [SerializeField] private bool immuneToDamage;
    [SerializeField] private bool disableOnDeath = true;

    private int currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnDamaged;
    public event Action OnDied;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public int Defense { get { return defense; } }
    public bool IsAlive { get { return currentHealth > 0; } }
    public bool IsImmuneToDamage { get { return immuneToDamage; } }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive || immuneToDamage) return false;

        int appliedDamage = Mathf.Max(0, amount - defense);
        if (appliedDamage == 0) return false;

        currentHealth = Mathf.Max(0, currentHealth - appliedDamage);
        if (OnDamaged != null) OnDamaged(appliedDamage, currentHealth);
        if (OnHealthChanged != null) OnHealthChanged(currentHealth, maxHealth);
        if (currentHealth != 0) return true;

        if (OnDied != null) OnDied();
        if (disableOnDeath) gameObject.SetActive(false);
        else Destroy(gameObject);
        return true;
    }

    public void RestoreToFull()
    {
        currentHealth = maxHealth;
        if (OnHealthChanged != null) OnHealthChanged(currentHealth, maxHealth);
    }

    public void SetDefense(int amount)
    {
        defense = Mathf.Max(0, amount);
    }
}

using System;
using UnityEngine;

// Owns the player's upgradeable attack strength. Other attacks can use this stat later.
public class PlayerDamage : MonoBehaviour
{
    [SerializeField, Min(1)] private int stompDamage = 1;
    [Tooltip("Stun applied when a stomp deals no damage.")]
    [SerializeField, Min(0f)] private float noDamageStunDuration = 0.5f;

    public int StompDamage { get { return stompDamage; } }
    public float NoDamageStunDuration { get { return noDamageStunDuration; } }
    public event Action<int> OnStompDamageChanged;

    public void UpgradeStompDamage(int amount)
    {
        if (amount <= 0) return;
        stompDamage += amount;
        if (OnStompDamageChanged != null) OnStompDamageChanged(stompDamage);
    }
}

using UnityEngine;

// A Goomba is now only a named enemy type. All reusable behavior lives in the
// generic components beside it, so variants can mix different movement,
// health, and stomp settings without copying this class.
[RequireComponent(typeof(EnemyHealth), typeof(EnemyDimension), typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyContactCombat))]
public class Goomba : MonoBehaviour
{
    private EnemyHealth health;
    private EnemyContactCombat combat;

    public int Health { get { return health.CurrentHealth; } }
    public int ContactDamage { get { return combat.ContactDamage; } }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        combat = GetComponent<EnemyContactCombat>();
    }
}

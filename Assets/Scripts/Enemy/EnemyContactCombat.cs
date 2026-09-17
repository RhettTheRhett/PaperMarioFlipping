using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyDimension), typeof(EnemyStun))]
public class EnemyContactCombat : MonoBehaviour
{
    public enum StompResponse
    {
        TakeDamageAndBounce,
        BounceWithoutDamage,
        HurtPlayer
    }

    [Header("Player Contact")]
    [SerializeField, Min(0)] private int contactDamage = 1;
    [SerializeField, Min(0f)] private float knockbackForce = 7f;
    [SerializeField, Min(0f)] private float knockbackUpwardForce = 3f;
    [SerializeField, Min(0f)] private float movementLockTime = 0.25f;

    [Header("Stomp")]
    [SerializeField] private StompResponse stompResponse = StompResponse.TakeDamageAndBounce;
    [SerializeField, Min(0f)] private float stompBounceSpeed = 7f;
    [SerializeField, Min(0f)] private float stompStunDuration = 0.5f;
    [SerializeField, Min(0f)] private float stompHeightTolerance = 0.15f;

    private readonly HashSet<Rigidbody> activeStompers = new HashSet<Rigidbody>();
    private EnemyHealth health;
    private EnemyDimension dimension;
    private EnemyStun stun;
    private Collider hitbox;

    public int ContactDamage { get { return contactDamage; } }
    public StompResponse StompBehavior { get { return stompResponse; } }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        dimension = GetComponent<EnemyDimension>();
        stun = GetComponent<EnemyStun>();
        hitbox = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision) { HandlePlayerContact(collision); }
    private void OnCollisionStay(Collision collision) { HandlePlayerContact(collision); }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null) activeStompers.Remove(collision.rigidbody);
    }

    private void HandlePlayerContact(Collision collision)
    {
        if (!health.IsAlive || !dimension.IsInteractive) return;

        PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive) return;

        Rigidbody playerBody = playerHealth.GetComponent<Rigidbody>();
        Collider playerCollider = collision.collider;
        if (playerBody == null || playerCollider == null) return;

        bool stomped = IsStomp(collision, playerBody);
        if (stomped && stompResponse != StompResponse.HurtPlayer)
        {
            if (!activeStompers.Add(playerBody)) return;

            PlayerDamage playerDamage = playerHealth.GetComponent<PlayerDamage>();
            bool dealtDamage = false;
            if (stompResponse == StompResponse.TakeDamageAndBounce && playerDamage != null)
            {
                dealtDamage = health.TakeDamage(playerDamage.StompDamage);
            }

            // Every valid stomp interrupts the enemy. Armored enemies may use
            // the player's no-damage duration when it is longer.
            float stunDuration = stompStunDuration;
            if (!dealtDamage && playerDamage != null)
                stunDuration = Mathf.Max(stunDuration, playerDamage.NoDamageStunDuration);
            stun.Stun(stunDuration);

            Vector3 velocity = playerBody.velocity;
            velocity.y = Mathf.Max(velocity.y, stompBounceSpeed);
            playerBody.velocity = velocity;
            return;
        }

        playerHealth.TakeHit(contactDamage, hitbox.bounds.center, knockbackForce,
            knockbackUpwardForce, movementLockTime);
    }

    private bool IsStomp(Collision collision, Rigidbody playerBody)
    {
        if (playerBody.velocity.y > 0.05f) return false;

        Bounds enemyBounds = hitbox.bounds;
        bool playerIsAbove = playerBody.worldCenterOfMass.y > enemyBounds.center.y;
        if (!playerIsAbove) return false;

        foreach (ContactPoint contact in collision.contacts)
            if (contact.point.y >= enemyBounds.center.y - stompHeightTolerance &&
                Mathf.Abs(contact.normal.y) >= 0.5f) return true;
        return false;
    }
}

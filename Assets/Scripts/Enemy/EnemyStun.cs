using UnityEngine;

public class EnemyStun : MonoBehaviour
{
    private float stunnedUntil;

    public bool IsStunned { get { return Time.time < stunnedUntil; } }

    public void Stun(float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0f, duration));
    }
}

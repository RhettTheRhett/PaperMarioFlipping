using UnityEngine;

public class PlayerFlippingState : PlayerBaseState
{
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float elapsed;
    private bool useGravity;
    private RigidbodyConstraints constraints;

    public override void EnterState(PlayerStateManager player)
    {
        player.rb.velocity = Vector3.zero;
        player.currentlyFlipping = true;
        useGravity = player.rb.useGravity;
        constraints = player.rb.constraints;
        player.rb.useGravity = false;
        player.rb.constraints = RigidbodyConstraints.FreezeAll;
        startRotation = player.transform.rotation;
        elapsed = 0f;

        // Toggle once, regardless of whether the player was idle or moving.
        WorldState target = player.is2d ? WorldState.Flipped3d : WorldState.Flat2d;
        targetRotation = target == WorldState.Flat2d
            ? (player.facingRight ? player.flipLeftFlat : player.flipRightFlat)
            : (player.facingRight ? player.flipLeftFlip : player.flipRightFlip);
        player.dimension.BeginFlip();
        player.worldStateManager.BeginFlip(target, player.flipDuration);
        player.currentWorldState = target;
        player.is2d = target == WorldState.Flat2d;
    }

    public override void ExitState(PlayerStateManager player)
    {
        player.rb.useGravity = useGravity;
        player.rb.constraints = constraints;
        player.currentlyFlipping = false;
        player.dimension.CompleteFlip();
    }

    public override void UpdateState(PlayerStateManager player)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, player.flipDuration));
        float eased = Mathf.SmoothStep(0f, 1f, t);
        player.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
        player.worldStateManager.ReportFlipProgress(eased);
        if (t >= 1f) player.SwitchState(player.idleState);
    }

    public override void FixedUpdateState(PlayerStateManager player) { }
    public override void OnCollisionEnter(PlayerStateManager player, Collision collision) { }
}

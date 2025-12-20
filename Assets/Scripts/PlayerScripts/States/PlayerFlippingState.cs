using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFlippingState : PlayerBaseState
{
    private float flipDuration = 1f;
    
    private Quaternion startRotation;
    private Quaternion targetRotation;
    
    public override void EnterState(PlayerStateManager player)
    {
        //player.StartCoroutine(FlipRoutine(player));
        player.rb.velocity = Vector3.zero;
        
        player.currentlyFlipping = true;

        startRotation = player.playerHolder.rotation;

        // Determine direction
        float angle = player.is2d ? 90f : -90f;

        targetRotation = startRotation * Quaternion.Euler(0f, angle, 0f);

        // Toggle dimension immediately (logic), visuals catch up
        player.is2d = !player.is2d;
        
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        player.transform.rotation = Quaternion.Slerp(
            player.transform.rotation,
            targetRotation,
            player.flipSpeed * Time.fixedDeltaTime
        );

        if (Quaternion.Angle(player.transform.rotation, targetRotation) < 0.25f)
        {
            player.transform.rotation = targetRotation;
            player.currentlyFlipping = false;

            // Return to appropriate idle/move state
            player.SwitchState(player.idleState);
        }
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
    
    IEnumerator FlipRoutine(PlayerStateManager player)
    {
        yield return new WaitForSeconds(flipDuration);

        player.is2d = !player.is2d;

        player.SwitchState(player.previousState);
    }
    
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFlippedMoveState : PlayerBaseState
{
    
    
    public override void EnterState(PlayerStateManager player)
    {
        
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        player.moveInput.y = Input.GetAxis("Horizontal") * -1;
        player.moveInput.x = Input.GetAxis("Vertical");
        
        if (player.rb.velocity.magnitude < 0.1f && !player.currentlyFlipping)
        {
            player.SwitchState(player.idleState);
        }
        //is the player trying to jump from idle
        
        else if (Input.GetKey(KeyCode.E) && player.isGrounded)
        {
            player.SwitchState(player.flippingState);
        }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        Vector3 move = new Vector3(player.moveInput.x * player.moveSpeed, player.rb.velocity.y, player.moveInput.y * player.moveSpeed);
        player.rb.velocity = move;
        FlippedFlip(player);
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
    
    void FlippedFlip(PlayerStateManager player)
    {
        // Decide facing direction 
        if (!player.facingRight && player.moveInput.y < 0 && player.isGrounded)
        {
            player.facingRight = true;
        }
        else if (player.facingRight && player.moveInput.y > 0 && player.isGrounded)
        {
            player.facingRight = false;
        }

        // Pick target rotation 
        Quaternion targetRotation;

        if (player.facingRight)
        {
            targetRotation = player.flipLeftFlip;
        }
        else
        {
            targetRotation = player.flipRightFlip;
        }

        
        player.currentlyFlipping = true;

        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, player.flipSpeed * Time.deltaTime);

        
        if (Quaternion.Angle(player.transform.rotation, targetRotation) < 0.5f)
        {
            player.transform.rotation = targetRotation; 
            player.currentlyFlipping = false;
        }
    }
}

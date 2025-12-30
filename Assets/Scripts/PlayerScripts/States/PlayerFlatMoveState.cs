using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFlatMoveState : PlayerBaseState
{
    public override void EnterState(PlayerStateManager player)
    {
        
    }

    public override void ExitState(PlayerStateManager player)
    {
        //player.rb.velocity = Vector3.zero;
    }

    public override void UpdateState(PlayerStateManager player)
    {
        player.moveInput.x = Input.GetAxis("Horizontal");
        player.moveInput.y = Input.GetAxis("Vertical");

        if (player.rb.velocity.x == 0 && !player.currentlyFlipping )
        {
            player.SwitchState(player.idleState);
        }
        //is the player trying to jump from idle
        else if (Input.GetKey(KeyCode.Space) && player.isGrounded)
        {
            player.SwitchState(player.jumpState);
        }
        else if (Input.GetKey(KeyCode.E) && player.isGrounded)
        {
            player.SwitchState(player.flippingState);
        }

        if (!player.IsObstacleInZAxis(player) && player.is2d) {
            //player.rb.position = new Vector3(player.rb.position.x, player.rb.position.y, 0);
        }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        player.rb.velocity = new Vector3(
            player.moveInput.x * player.moveSpeed,
            player.rb.velocity.y,
            0
        );

        FlatFlip(player);
        
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }

    void FlatFlip(PlayerStateManager player)
    {
        // Decide facing direction 
        if (!player.facingRight && player.moveInput.x < 0 && player.isGrounded)
        {
            player.facingRight = true;
        }
        else if (player.facingRight && player.moveInput.x > 0 && player.isGrounded)
        {
            player.facingRight = false;
        }

        // Pick target rotation 
        Quaternion targetRotation;

        if (player.facingRight)
        {
            targetRotation = player.flipLeftFlat;
        }
        else
        {
            targetRotation = player.flipRightFlat;
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

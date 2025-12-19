using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    private bool hasJumped;
    public override void EnterState(PlayerStateManager player)
    {
        Jump(player);
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        //handleJump(player);
        //JumpGravity(player);
        if (player.isGrounded)
        {
            if (Mathf.Abs(player.rb.velocity.x) > 0.1f)
                player.SwitchState(player.flatMoveState);
            else
                player.SwitchState(player.idleState);
        }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        float moveX = Input.GetAxis("Horizontal");

        player.rb.velocity = new Vector3(
            moveX * player.moveSpeed,
            player.rb.velocity.y,
            0
        );
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
    
    private void handleJump(PlayerStateManager player) {

        if (Input.GetKeyDown(KeyCode.Space)) {
            player.jumpBufferCounter = player.jumpBufferTime;
        } else {
            player.jumpBufferCounter -= Time.deltaTime;
        }

        if (player.isGrounded) {
            player.coyoteTimeCounter = player.coyoteTime;
        } else {
            player.coyoteTimeCounter -= Time.deltaTime;
        }

        //lets the player jump if grounded and spcae is pressed
        //changed to lets the player jump if jump buffer is > 0 and coyotetimer counter is >0
        if (player.jumpBufferCounter > 0 && player.coyoteTimeCounter > 0f) {
            player.jumpInput = true;
            player.coyoteTimeCounter = 0f;
        }

        if (Physics.CheckBox(player.groundCheck.position, player.boxSize, player.transform.rotation, player.ground)) {
            player.isGrounded = true;
        } else {
            player.isGrounded = false;
        }
        
        Jump(player);
        
    }

    void Jump(PlayerStateManager player) { 
        player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);                    // Vector3.up * (jumpForce) ; 
        player.jumpInput = false;
        player.lastGroundedPos = player.transform.position;
        //Debug.Log(player.lastGroundedPos);
        
    }
    
}

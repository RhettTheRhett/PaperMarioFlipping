using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    private bool hasJumped;
    public override void EnterState(PlayerStateManager player)
    {
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        handleJump(player);
                if (player.rb.velocity.x <= 0)
                {
                    player.SwitchState(player.idleState);
                }
                else if (player.rb.velocity.x > 0)
                {
                    player.SwitchState(player.flatMoveState);
                }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        
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

        /*
         * old raycast ground checker
         * 
        RaycastHit hit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, rayLength, ground)) {
            isGrounded = true;
        } else {
            isGrounded = false;
        }
        */

        //Debug.DrawRay(groundCheck.position, Vector2.down, Color.red);


        
        Jump(player);
        
        JumpGravity(player);
    }

    void Jump(PlayerStateManager player) { 
        player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);                    // Vector3.up * (jumpForce) ; 
        player.jumpInput = false;
        player.lastGroundedPos = player.transform.position;
        Debug.Log(player.lastGroundedPos);
        
    }

    void JumpGravity(PlayerStateManager player) {
        if (player.rb.velocity.y < 0) {
            player.rb.velocity += Vector3.up * player.fallGravity * Time.deltaTime;
        } 
        else if ((player.rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))) {
            player.rb.velocity += Vector3.up * player.normalGravity * Time.deltaTime;
        
        }
    }

    void CheckPlayerFalling(PlayerStateManager player) {
        if (!player.isGrounded && player.checkLastPos) {
            player.checkLastPos = false;
            if (player.facingRight) {
                player.lastGroundedPos = new Vector3(player.transform.position.x + player.transform.localScale.x, player.transform.position.y, player.transform.position.z);
            } else {
                player.lastGroundedPos = new Vector3(player.transform.position.x - player.transform.localScale.x, player.transform.position.y, player.transform.position.z);
            }
            
            Debug.Log(player.lastGroundedPos);
        } else if (player.isGrounded) {
            player.checkLastPos = true;
        }

    }

    void SavePlayer(PlayerStateManager player) {
        player.transform.position = player.lastGroundedPos;
    }
    
}

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
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        Vector3 move = new Vector3(player.moveInput.x * player.moveSpeed, player.rb.velocity.y, player.moveInput.y * player.moveSpeed);
        player.rb.velocity = move;
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
}

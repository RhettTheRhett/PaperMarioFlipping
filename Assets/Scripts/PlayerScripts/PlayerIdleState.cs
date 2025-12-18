using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public override void EnterState(PlayerStateManager player)
    {
        
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        //is the player starting to move while flat
        float moveX = Input.GetAxis("Horizontal");

        if (moveX != 0)
        {
            player.SwitchState(player.flatMoveState);
        }
        //is the player trying to jump from idle
        else if (Input.GetKey(KeyCode.Space) && player.isGrounded)
        {
            player.SwitchState(player.jumpState);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            player.SwitchState(player.flippingState);
        }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public override void EnterState(PlayerStateManager player)
    {
        player.rb.velocity = new Vector3(0f, player.rb.velocity.y, 0f);
        
    }

    public override void ExitState(PlayerStateManager player)
    {
        
    }

    public override void UpdateState(PlayerStateManager player)
    {
        //is the player starting to move while flat
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        

        
        if (moveX != 0 || (!player.is2d && moveY != 0))
        {
            switch (player.is2d)
            {
                case true:

                    player.SwitchState(player.flatMoveState);
                    break;
                case false:

                    player.SwitchState(player.flippedMoveState);
                    break;
            }
            
        }
        //is the player trying to jump from idle


    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
}

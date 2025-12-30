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
        float moveY = Input.GetAxis("Vertical");
        
        if (!player.IsObstacleInZAxis(player) && player.is2d) {
            //player.rb.position = new Vector3(player.rb.position.x, player.rb.position.y, 0);
        }
        
        if (moveX != 0 || moveY != 0)
        {
            switch (player.is2d)
            {
                case true:
                    Debug.Log(player.is2d);
                    player.SwitchState(player.flatMoveState);
                    break;
                case false:
                    Debug.Log(player.is2d);
                    player.SwitchState(player.flippedMoveState);
                    break;
            }
            
        }
        //is the player trying to jump from idle
        else if (Input.GetKey(KeyCode.Space) && player.isGrounded)
        {
            player.SwitchState(player.jumpState);
        }
        else if (Input.GetKey(KeyCode.E) && player.isGrounded)
        {
            player.SwitchState(player.flippingState);
            if (player.currentWorldState == WorldState.Flipped3d)
            {
                player.worldStateManager.ChangeWorldState(WorldState.Flat2d);
            }
            else if (player.currentWorldState == WorldState.Flat2d)
            {
                player.worldStateManager.ChangeWorldState(WorldState.Flipped3d);
            }
            
        }
    }

    public override void FixedUpdateState(PlayerStateManager player)
    {
        
    }

    public override void OnCollisionEnter(PlayerStateManager player, Collision collision)
    {
        
    }
}

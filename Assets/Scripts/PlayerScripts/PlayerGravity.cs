using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGravity : MonoBehaviour
{
    public GameObject player;
    public PlayerStateManager playerStateManager;
    public int magicnum = 10;
    
    private void Awake()
    {
        player = GameObject.Find("Player");
        playerStateManager = player.GetComponent<PlayerStateManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        playerStateManager.fallGravity = Physics.gravity.y * playerStateManager.gravityMultiplier;
        playerStateManager.normalGravity = Physics.gravity.y - magicnum;
    }

    // Update is called once per frame
    void Update()
    {
        JumpGravity(playerStateManager);
    }
    
    public void JumpGravity(PlayerStateManager player) {
        if (player.rb.velocity.y < 0) {
            player.rb.velocity += Vector3.up * player.fallGravity * Time.deltaTime;
        } 
        else if ((player.rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))) {
            player.rb.velocity += Vector3.up * player.normalGravity * Time.deltaTime;
        
        }
    }
}

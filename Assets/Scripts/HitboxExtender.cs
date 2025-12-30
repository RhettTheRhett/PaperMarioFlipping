using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Update = UnityEngine.PlayerLoop.Update;

public class HitboxExtender : MonoBehaviour
{
   public WorldStateManager worldStateManager;
   public GameObject worldStateManagerObject;
   public WorldState currentWorldState;
   
   public BoxCollider extendedHitboxCollider;
   private void Awake()
   {
      worldStateManagerObject =  GameObject.Find("WorldStateManager");
      worldStateManager = worldStateManagerObject.GetComponent<WorldStateManager>();
      extendedHitboxCollider = GetComponent<BoxCollider>();
   }

   private void FixedUpdate()
   {
      currentWorldState = worldStateManager.GetWorldState();
      if (currentWorldState == WorldState.Flat2d)
      {
         extendedHitboxCollider.enabled = true;
      }
      else
      {
         extendedHitboxCollider.enabled = false;
      }
   }
}

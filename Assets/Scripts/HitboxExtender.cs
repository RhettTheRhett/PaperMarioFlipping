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
   private PaneManager paneManager;
   private Pane pane;
   private void Awake()
   {
      worldStateManagerObject = WorldStateManager.Get().gameObject;
      worldStateManager = worldStateManagerObject.GetComponent<WorldStateManager>();
      extendedHitboxCollider = GetComponent<BoxCollider>();
      paneManager = PaneManager.Get();
      pane = GetComponentInParent<Pane>();
   }

   private void OnEnable()
   {
      worldStateManager.OnWorldStateChanged += ApplyState;
      worldStateManager.OnFlipCompleted += ApplyState;
      paneManager.OnCurrentPaneChanged += ApplyPane;
      ApplyState(worldStateManager.GetWorldState());
   }

   private void OnDisable()
   {
      worldStateManager.OnWorldStateChanged -= ApplyState;
      worldStateManager.OnFlipCompleted -= ApplyState;
      paneManager.OnCurrentPaneChanged -= ApplyPane;
      if (extendedHitboxCollider != null) extendedHitboxCollider.enabled = false;
   }

   private void ApplyPane(Pane previous, Pane current) { ApplyState(worldStateManager.GetWorldState()); }

   private void ApplyState(WorldState state)
   {
      currentWorldState = worldStateManager.GetWorldState();
      if (currentWorldState == WorldState.Flat2d && !worldStateManager.IsFlipping && paneManager.IsCurrent(pane))
      {
         extendedHitboxCollider.enabled = true;
      }
      else
      {
         extendedHitboxCollider.enabled = false;
      }
   }
}

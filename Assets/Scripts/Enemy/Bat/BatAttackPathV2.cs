using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatAttackPathV2 : MonoBehaviour
{

    private Transform playerTransform;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")){
            playerTransform = other.transform;
            Attack(playerTransform);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerTransform = null;
            
        }
    }

    private void Attack(Transform attackTransform) {
        if (playerTransform != null) {

        }
    }
}

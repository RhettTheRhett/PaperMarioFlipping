using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatChase : MonoBehaviour
{

    private Transform playerTransform;
    public Transform batTransform;

    private bool batCanMove = false;

    private Vector3 zOffset;
    private float speed = 0.1f;
    private float distance;



    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerTransform = other.transform;

            batCanMove = true;
            
        }
    }

    private Vector3 ChasePlayer(Transform playerTransform) {
        if (playerTransform != null) { 
            Vector3 batPos = batTransform.position - new Vector3(batTransform.position.x,0,batTransform.position.z);
            Vector3 movePos = Vector3.Slerp(batPos, playerTransform.position, speed);

            return movePos;
        }
        return Vector3.zero;
    }

    private IEnumerator moveBat() {
        while(distance > 1f) {

            batTransform.position = Vector3.MoveTowards(transform.position, ChasePlayer(playerTransform), speed);

            yield return null;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerTransform = null;

        }
    }


    void Update() {
        distance = Vector3.Distance(batTransform.position, playerTransform.position);
        if (batCanMove) {
            StartCoroutine(moveBat());
        } else {
            StopCoroutine(moveBat());
        }
    }
}

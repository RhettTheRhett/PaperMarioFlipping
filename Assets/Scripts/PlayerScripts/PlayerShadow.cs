using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShadow : MonoBehaviour
{
    public GameObject shadow;
    public GameObject player;

    private Vector3 shadowOffset = new Vector3(0f,0.1f,0f);

    // Update is called once per frame
    void Update()
    {
        MoveShadow();
    }

    void MoveShadow() {
    RaycastHit hit;
        if (shadow) {
            if (Physics.Raycast(player.transform.position, Vector3.down, out hit)) {
                transform.position = hit.point + shadowOffset;

            }
        }
    }
}

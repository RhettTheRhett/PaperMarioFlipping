using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform target;
    public Camera camera;
    
    public float boundX = 2.0f, boundY = 2.0f, boundZ = 2.0f;
    
    public float smoothSpeed = 2f;
    private Vector3 targetPos;
    
    private readonly Vector3 vec0 = Vector3.zero;
    public Vector3 offset = new Vector3(0,10,-10);

    public GameObject player;
    public PlayerStateManager playerStateManager;

    private void Awake()
    {
        camera = GetComponent<Camera>();
        player = GameObject.Find("Player");
        playerStateManager = player.GetComponent<PlayerStateManager>();
    }

    private void LateUpdate()
    {
        if (playerStateManager != null)
        {
            if (playerStateManager.is2d)
            {
                flatCam();
            }
            else
            {
                flippedCam();
            }
        }
    }

    private void flatCam()
    {
        camera.orthographic = true;
        Vector3 delta = vec0;
        //x
        float dx = target.position.x - transform.position.x;
        if (dx > boundX || dx < -boundX)
        {
            if (transform.position.x < target.position.x)
            {
                delta.x = dx - boundX;
            } 
            else 
            {
                delta.x = dx + boundX; 
            }
        }
        //y
        float dy = target.position.y - transform.position.y;
        if (dy > boundX || dy < -boundX)
        {
            if (transform.position.y < target.position.y)
            {
                delta.y = dy - boundY;
            } 
            else 
            {
                delta.y = dy + boundY; 
            }
        }
        //z
        float dz = target.position.z - transform.position.z;
        if (dz > boundZ || dz < -boundZ)
        {
            if (transform.position.z < target.position.z)
            {
                delta.z = dz - boundZ - 10;
            }
            else
            {
                delta.z = dz + boundZ - 10;
            }
        }
        
        targetPos = transform.position + delta;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    private void flippedCam()
    {
        
        camera.orthographic = false;
        Vector3 delta = vec0;
        //x
        float dx = target.position.x - transform.position.x;
        if (dx > boundX || dx < -boundX)
        {
            if (transform.position.x < target.position.x)
            {
                delta.x = dx - boundX;
            } 
            else 
            {
                delta.x = dx + boundX; 
            }
        }
        //y
        float dy = target.position.y - transform.position.y;
        if (dy > boundX || dy < -boundX)
        {
            if (transform.position.y < target.position.y)
            {
                delta.y = dy - boundY;
            } 
            else 
            {
                delta.y = dy + boundY; 
            }
        }
        //z
        float dz = target.position.z - transform.position.z;
        if (dz > boundZ || dz < -boundZ)
        {
            if (transform.position.z < target.position.z)
            {
                delta.z = dz - boundZ;
            }
            else
            {
                delta.z = dz + boundZ;
            }
        }
        
        targetPos = transform.position + delta + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}

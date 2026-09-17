using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform target;
    public new Camera camera;
    
    public float boundX = 2.0f, boundY = 2.0f, boundZ = 2.0f;
    
    public float smoothSpeed = 2f;
    private Vector3 targetPos;
    
    private readonly Vector3 vec0 = Vector3.zero;
    public Vector3 offset = new Vector3(0,10,-10);
    public Vector3 flippedOffset = new Vector3(-5,2,0);

    public GameObject player;
    public PlayerStateManager playerStateManager;
    private Vector3 flipStartPosition;
    private Quaternion flipStartRotation;
    private bool wasFlipping;

    private void Awake()
    {
        camera = GetComponent<Camera>();
        if (player == null) player = GameObject.Find("Player");
        if (player != null) playerStateManager = player.GetComponent<PlayerStateManager>();
        if (target == null && playerStateManager != null) target = playerStateManager.transform;
    }

    private void LateUpdate()
    {
        if (playerStateManager != null && target != null)
        {
            WorldStateManager world = playerStateManager.worldStateManager;
            if (world.IsFlipping)
            {
                if (!wasFlipping)
                {
                    flipStartPosition = transform.position;
                    flipStartRotation = transform.rotation;
                }
                wasFlipping = true;
                bool flat = world.FlipTo == WorldState.Flat2d;
                Vector3 destination = target.position + (flat ? offset : flippedOffset);
                Quaternion rotation = flat ? Quaternion.identity : Quaternion.Euler(25f, 90f, 0f);
                transform.position = Vector3.Lerp(flipStartPosition, destination, world.FlipProgress);
                transform.rotation = Quaternion.Slerp(flipStartRotation, rotation, world.FlipProgress);
                camera.orthographic = false;
                return;
            }
            wasFlipping = false;
            if (playerStateManager.currentWorldState == WorldState.Flat2d)
            {

                flatCam();
            }
            else if (playerStateManager.currentWorldState == WorldState.Flipped3d)
            {

                flippedCam();
            }
        }
    }

    private void flatCam()
    {
        camera.orthographic = true;
        Vector3 delta = vec0;
        Quaternion lookRotation = Quaternion.Euler(0f,0f,0f);
        //x
        float dx = target.position.x + offset.x - transform.position.x;
        if (dx > boundX || dx < -boundX)
        {
            if (transform.position.x < target.position.x + offset.x)
            {
                delta.x = dx - boundX;
            } 
            else 
            {
                delta.x = dx + boundX; 
            }
        }
        //y
        float dy = target.position.y + offset.y - transform.position.y;
        if (dy > boundY || dy < -boundY)
        {
            if (transform.position.y < target.position.y + offset.y)
            {
                delta.y = dy - boundY;
            } 
            else 
            {
                delta.y = dy + boundY; 
            }
        }
        //z
        float dz = target.position.z + offset.z - transform.position.z;
        if (dz > boundZ || dz < -boundZ)
        {
            if (transform.position.z < target.position.z + offset.z)
            {
                delta.z = dz;
            }
            else
            {
                delta.z = dz;
            }
        }
        
        targetPos = transform.position + delta;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, smoothSpeed * Time.deltaTime);
    }

    private void flippedCam()
    {
        
        camera.orthographic = false;
        Vector3 delta = vec0;
        Quaternion lookRotation = Quaternion.Euler(25f,90f,0f);
        //x
        float dx = target.position.x + flippedOffset.x - transform.position.x;
        if (dx > boundX || dx < -boundX)
        {
            if (transform.position.x < target.position.x + flippedOffset.x)
            {
                delta.x = dx - boundX;
            } 
            else 
            {
                delta.x = dx + boundX; 
            }
        }
        //y
        float dy = target.position.y + flippedOffset.y - transform.position.y;
        if (dy > boundY || dy < -boundY)
        {
            if (transform.position.y < target.position.y + flippedOffset.y)
            {
                delta.y = dy - boundY;
            } 
            else 
            {
                delta.y = dy + boundY; 
            }
        }
        //z
        float dz = target.position.z + flippedOffset.z - transform.position.z;
        if (dz > boundZ || dz < -boundZ)
        {
            if (transform.position.z < target.position.z + flippedOffset.z)
            {
                delta.z = dz - boundZ;
            }
            else
            {
                delta.z = dz + boundZ;
            }
        }
        
        targetPos = transform.position + delta;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, smoothSpeed * Time.deltaTime);
    }
}

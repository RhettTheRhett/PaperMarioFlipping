using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    
    public float moveSpeed = 7f;

    Vector2 moveInput;

    public float jumpForce = 9f;
    public float gravityMultiplier = 1.5f;
    public float fallGravity;
    public float normalGravity;

    public float coyoteTime = 0.2f;
    public float coyoteTimeCounter;

    public float jumpBufferTime = 0.2f;
    public float jumpBufferCounter;

    [SerializeField] private bool jumpInput;

    public Transform groundCheck;
    public LayerMask ground;
    public float rayLength = 0.3f;
    public Vector3 boxSize;
    public bool isGrounded = true;
    public Vector3 lastGroundedPos;
    public bool checkLastPos = false;

    private float checkDistance = 2;

    Rigidbody rb;

    public bool flipped;
    public float flipSpeed = 7f;

    private Quaternion flipLeftFlat = Quaternion.Euler(0f, -180f, 0f);
    private Quaternion flipRightFlat = Quaternion.Euler(0f, 0f, 0f);

    private Quaternion flipLeftFlip = Quaternion.Euler(0f, -90f, 0f);
    private Quaternion flipRightFlip = Quaternion.Euler(0f, -270f, 0f);

    private Quaternion flipView = Quaternion.Euler(0f, -90f, 0f);

    private Camera cam;
    [SerializeField] private CinemachineVirtualCamera flatCam;
    [SerializeField] private CinemachineVirtualCamera flipCam;

    public bool is2d = true;
    [SerializeField] public Transform camPos1;
    [SerializeField] public Transform camPos2;

   

    // Start is called before the first frame update
    void Start()
    {
        Camera.main.orthographic = true;
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;

        fallGravity = Physics.gravity.y * gravityMultiplier;
        normalGravity = Physics.gravity.y - 10;
    }   

    private void Update()
    {
        handleMove();
        handleJump();
    }

    private void FixedUpdate() {
        flipCamera();
        flipPlayer();
        CheckPlayerFalling();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("OutOfBounds")) {
            
            Invoke("SavePlayer",1);
            //Debug.Log("saved");
           
        }
    }

    private void handleMove() {
        
            if (!is2d) {
                moveInput.y = Input.GetAxis("Horizontal") * -1;
                moveInput.x = Input.GetAxis("Vertical");

                rb.velocity = new Vector3(moveInput.x * moveSpeed, rb.velocity.y, moveInput.y * moveSpeed);
        } else {
                moveInput.x = Input.GetAxis("Horizontal");
                moveInput.y = Input.GetAxis("Vertical");

                rb.velocity = new Vector3(moveInput.x * moveSpeed, rb.velocity.y, 0);

                //move this to playerflippingstate
            if (!IsObstacleInZAxis()) {
                rb.position = new Vector3(rb.position.x, rb.position.y, 0);
            }
        }

            

    }

    private void flipPlayer() {

        // Flips the player based on flipped bool
        ///////////////////////////////////////////////////

        if (is2d) {

            //this.transform.parent.transform.rotation = Quaternion.identity;

            if (!flipped && moveInput.x < 0 && isGrounded) {
                flipped = true;
            } else if (flipped && moveInput.x > 0 && isGrounded) {
                flipped = false;
            }

            if (flipped) {
                transform.rotation = Quaternion.Slerp(transform.rotation, flipLeftFlat, flipSpeed * Time.deltaTime);
            } else if (!flipped) {
                transform.rotation = Quaternion.Slerp(transform.rotation, flipRightFlat, flipSpeed  * Time.deltaTime);
            }

        } 
        /*
         * 3d player flipping currently borken
         * 
        else if (!is2d) {

            //transform.rotation = flipView;

            if (!flipped && moveInput.x < 0 && isGrounded) {

                flipped = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, flipLeftFlip, flipSpeed  * Time.deltaTime);

            } else if (flipped && moveInput.x > 0 && isGrounded) {
                flipped = false;
                transform.rotation = Quaternion.Slerp(transform.rotation, flipRightFlip, flipSpeed  * Time.deltaTime);
            }

          
        }
        */


        ///////////////////////////////////////////////////
    }

    //moe to playerflippingstate
    private void flipCamera() {
        if (Input.GetKeyDown(KeyCode.E) && isGrounded) {
            if (is2d) {
                //transform.rotation = flipView;
                
                Camera.main.fieldOfView = 70f;
                flipCam.transform.position = camPos1.transform.position;
                // flipCam.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, flipView, flipSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, flipView, flipSpeed);// * Time.deltaTime * 10);
                
                flatCam.enabled = false;
                flipCam.enabled = true;
                Camera.main.orthographic = false;
                is2d = false;
            } else if (!is2d) {
                //transform.rotation = Quaternion.identity;
                Camera.main.orthographic = true;
                flatCam.transform.position = camPos2.transform.position;
                //flatCam.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, flipRight, flipSpeed * Time.deltaTime);
                flatCam.enabled = true;
                flipCam.enabled = false;
                is2d = true;
            }
        }
    }

    private void handleJump() {

        if (Input.GetKeyDown(KeyCode.Space)) {
            jumpBufferCounter = jumpBufferTime;
        } else {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (isGrounded) {
            coyoteTimeCounter = coyoteTime;
        } else {
            coyoteTimeCounter -= Time.deltaTime;
        }

        //lets the player jump if grounded and spcae is pressed
        //changed to lets the player jump if jump buffer is > 0 and coyotetimer counter is >0
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0f) {
            jumpInput = true;
            coyoteTimeCounter = 0f;
        }

        if (Physics.CheckBox(groundCheck.position, boxSize, transform.rotation, ground)) {
            isGrounded = true;
        } else {
            isGrounded = false;
        }

        /*
         * old raycast ground checker
         * 
        RaycastHit hit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, rayLength, ground)) {
            isGrounded = true;
        } else {
            isGrounded = false;
        }
        */

        //Debug.DrawRay(groundCheck.position, Vector2.down, Color.red);


        if (jumpInput) {
            Jump();
        }
        JumpGravity();
    }

    void Jump() { 
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);                    // Vector3.up * (jumpForce) ; 
        jumpInput = false;
        lastGroundedPos = transform.position;
        Debug.Log(lastGroundedPos);
        
    }

    void JumpGravity() {
        if (rb.velocity.y < 0) {
            rb.velocity += Vector3.up * fallGravity * Time.deltaTime;
        } 
        else if ((rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))) {
            rb.velocity += Vector3.up * normalGravity * Time.deltaTime;
        
        }
    }

    void CheckPlayerFalling() {
        if (!isGrounded && checkLastPos) {
            checkLastPos = false;
            if (flipped) {
                lastGroundedPos = new Vector3(transform.position.x + transform.localScale.x, transform.position.y, transform.position.z);
            } else {
                lastGroundedPos = new Vector3(transform.position.x - transform.localScale.x, transform.position.y, transform.position.z);
            }
            
            Debug.Log(lastGroundedPos);
        } else if (isGrounded) {
            checkLastPos = true;
        }

    }

    void SavePlayer() {
        transform.position = lastGroundedPos;
    }

    private bool IsObstacleInZAxis() {
        //float checkDistance = 1.75f;  
        Vector3 frontCheckPos = transform.position + Vector3.forward * checkDistance;
        Vector3 backCheckPos = transform.position - Vector3.forward * checkDistance;

        // Check for obstacles in front or behind the player
        bool frontBlocked = Physics.CheckBox(frontCheckPos, transform.localScale , Quaternion.identity, ground);
        bool backBlocked = Physics.CheckBox(backCheckPos, transform.localScale , Quaternion.identity, ground);

        Debug.Log(frontBlocked);
        Debug.Log(backBlocked);

        return frontBlocked || backBlocked;
    }

    private void OnDrawGizmos() {
        //Gizmos.DrawWireCube(groundCheck.transform.position, boxSize );
        
        //float checkDistance = 1.75f;  
        Vector3 frontCheckPos = transform.position + Vector3.forward * checkDistance;
        Vector3 backCheckPos = transform.position - Vector3.forward * checkDistance;
        Vector3 boxSize = new Vector3(1, 1, checkDistance * 2);

        Gizmos.color = Color.red;  
        Gizmos.DrawWireCube(frontCheckPos, boxSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(backCheckPos, boxSize);
        
    }

}

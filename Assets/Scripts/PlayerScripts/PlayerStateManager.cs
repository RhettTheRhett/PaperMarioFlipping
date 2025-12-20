using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    #region Variables
    
    
    [Header("States")]
    public PlayerBaseState currentState;
    public PlayerBaseState  previousState;
    public PlayerIdleState idleState = new PlayerIdleState();
    public PlayerFlatMoveState flatMoveState = new PlayerFlatMoveState();
    public PlayerFlippedMoveState flippedMoveState =  new PlayerFlippedMoveState();
    public PlayerFlippingState flippingState =  new PlayerFlippingState();
    public PlayerJumpState jumpState = new PlayerJumpState();
    
    [Header("Jump")]
    public float jumpForce = 9f;
    public float gravityMultiplier = 1.5f;
    public float fallGravity;
    public float normalGravity;

    public float coyoteTime = 0.2f;
    public float coyoteTimeCounter;

    public float jumpBufferTime = 0.2f;
    public float jumpBufferCounter;

    [SerializeField] public bool jumpInput;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask ground;
    public float rayLength = 0.3f;
    public Vector3 boxSize;
    public bool isGrounded = true;
    public Vector3 lastGroundedPos;
    public bool checkLastPos = false;
    
    [Header("General")]
    public Rigidbody rb;
    public Transform playerHolder;
    public bool is2d = true;
    
    [Header("Flip Flop")]
    public bool facingRight;
    public bool currentlyFlipping = false;
    public float flipSpeed = 7f;
    public Quaternion flipLeftFlat = Quaternion.Euler(0f, -180f, 0f);
    public Quaternion flipRightFlat = Quaternion.Euler(0f, 0f, 0f);

    public Quaternion flipLeftFlip = Quaternion.Euler(0f, 270f, 0f);
    public Quaternion flipRightFlip = Quaternion.Euler(0f, 90f, 0f);

    public Quaternion flipView = Quaternion.Euler(0f, -90f, 0f);

    [Header("Movement")]
    public float moveSpeed = 7f;
    public Vector2 moveInput;
    public float checkDistance = 2;
    
    /*
    [Header("Camera")]
    public Camera cam;
    [SerializeField] private CinemachineVirtualCamera flatCam;
    [SerializeField] private CinemachineVirtualCamera flipCam;

    
    [SerializeField] public Transform camPos1;
    [SerializeField] public Transform camPos2;
    */
    
    #endregion
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fallGravity = Physics.gravity.y * gravityMultiplier;
        normalGravity = Physics.gravity.y - 10;
    }

    void Start()
    {
        currentState = idleState;
        
        currentState.EnterState(this);
    }
    
    void Update()
    {
        currentState.UpdateState(this);
        
        
        //Debug.Log(coyoteTimeCounter);
        if (Input.GetKeyDown(KeyCode.Space)) 
            jumpBufferCounter = jumpBufferTime;
        else 
            jumpBufferCounter -= Time.deltaTime;

        // Trigger Jump State
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            if (currentState != jumpState) // Don't restart if already jumping
            {
                SwitchState(jumpState);
            }
        }
        //jumpState.JumpGravity(this);
        CheckPlayerFalling(this);
    }

    void FixedUpdate()
    {
        currentState.FixedUpdateState(this);
        
    }

    void OnCollisionEnter(Collision collision)
    {
        currentState.OnCollisionEnter(this, collision);
        if (collision.gameObject.CompareTag("OutOfBounds"))
        {
            SavePlayer(this);
        }
    }

    public void SwitchState(PlayerBaseState state)
    {
        Debug.Log($"{currentState} → {state}");
        previousState = currentState;
        currentState.ExitState(this);
        currentState = state;
        state.EnterState(this);
    }
    
    void CheckPlayerFalling(PlayerStateManager player) {
        
        if (Physics.CheckBox(player.groundCheck.position, player.boxSize, player.transform.rotation, player.ground)) {
            player.isGrounded = true;
        } else {
            player.isGrounded = false;
        }
        
        if (player.isGrounded) {
            player.coyoteTimeCounter = player.coyoteTime;
        } else {
            player.coyoteTimeCounter -= Time.deltaTime;
        }

        //lets the player jump if grounded and spcae is pressed
        //changed to lets the player jump if jump buffer is > 0 and coyotetimer counter is >0
        if (player.jumpBufferCounter > 0 && player.coyoteTimeCounter > 0f) {
            player.jumpInput = true;
            player.coyoteTimeCounter = 0f;
        }
        
        if (!player.isGrounded && player.checkLastPos) {
            player.checkLastPos = false;
            if (player.facingRight) {
                player.lastGroundedPos = new Vector3(player.transform.position.x + player.transform.localScale.x, player.transform.position.y, player.transform.position.z);
            } else {
                player.lastGroundedPos = new Vector3(player.transform.position.x - player.transform.localScale.x, player.transform.position.y, player.transform.position.z);
            }
            
            //Debug.Log(player.lastGroundedPos);
        } else if (player.isGrounded) {
            player.checkLastPos = true;
        }

    }

    void SavePlayer(PlayerStateManager player) {
        player.transform.position = player.lastGroundedPos;
    }
    
    public bool IsObstacleInZAxis(PlayerStateManager player) {
        //float checkDistance = 1.75f;  
        Vector3 frontCheckPos = player.transform.position + Vector3.forward * player.checkDistance;
        Vector3 backCheckPos = player.transform.position - Vector3.forward * player.checkDistance;

        // Check for obstacles in front or behind the player
        bool frontBlocked = Physics.CheckBox(frontCheckPos, player.transform.localScale , Quaternion.identity, player.ground);
        bool backBlocked = Physics.CheckBox(backCheckPos, player.transform.localScale , Quaternion.identity, player.ground);

        Debug.Log(frontBlocked);
        Debug.Log(backBlocked);

        return frontBlocked || backBlocked;
    }
    
    private void OnDrawGizmos() {
        
        Gizmos.DrawWireCube(groundCheck.transform.position, this.boxSize );
        
        Vector3 frontCheckPos = transform.position + Vector3.forward * checkDistance;
        Vector3 backCheckPos = transform.position - Vector3.forward * checkDistance;
        Vector3 boxSize = new Vector3(1, 1, checkDistance * 2);

        Gizmos.color = Color.red;  
        Gizmos.DrawWireCube(frontCheckPos, boxSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(backCheckPos, boxSize);
    }
}

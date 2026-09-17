using UnityEngine;

[RequireComponent(typeof(PlayerDimension))]
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

    [Header("World State")] 
    public GameObject worldStateManagerObject;
    public WorldStateManager worldStateManager;
    public WorldState currentWorldState;
    public bool is2d = true;
    
    [Header("Flip Flop")]
    public bool facingRight;
    public bool currentlyFlipping = false;
    public float flipDuration = 0.35f;
    public PlayerDimension dimension;
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
    [SerializeField, Min(0f)] private float knockbackDeceleration = 28f;
    private float movementLockedUntil;

    public bool IsMovementLocked { get { return Time.time < movementLockedUntil; } }
    
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
        worldStateManagerObject = WorldStateManager.Get().gameObject;
        worldStateManager = worldStateManagerObject.GetComponent<WorldStateManager>();
        worldStateManager.ChangeWorldState(WorldState.Flat2d);
        rb = GetComponent<Rigidbody>();
        dimension = GetComponent<PlayerDimension>();
        if (dimension == null) dimension = gameObject.AddComponent<PlayerDimension>();
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
        currentWorldState = worldStateManager.GetWorldState();
        is2d = currentWorldState == WorldState.Flat2d;
        CheckPlayerFalling(this);

        if (IsMovementLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Read the button once per rendered frame, before movement can change states.
        if (Input.GetKeyDown(KeyCode.E)) TryFlip();

        currentState.UpdateState(this);
        
        //Debug.Log(coyoteTimeCounter);
        if (Input.GetKeyDown(KeyCode.Space)) 
            jumpBufferCounter = jumpBufferTime;
        else 
            jumpBufferCounter -= Time.deltaTime;

        // Trigger Jump State
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !currentlyFlipping)
        {
            if (currentState != jumpState) // Don't restart if already jumping
            {
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                SwitchState(jumpState);
            }
        }

        if (currentWorldState == WorldState.Flipped3d)
        {
            is2d = false;
        }
        else if (currentWorldState == WorldState.Flat2d)
        {
            is2d = true;
        }
        //jumpState.JumpGravity(this);
    }

    void FixedUpdate()
    {
        if (IsMovementLocked)
        {
            Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero,
                knockbackDeceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(horizontal.x, rb.velocity.y, is2d ? 0f : horizontal.z);
            return;
        }
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
        
        previousState = currentState;
        currentState.ExitState(this);
        currentState = state;
        state.EnterState(this);
    }

    public bool TryFlip()
    {
        if (!isGrounded || currentlyFlipping || IsMovementLocked) return false;
        SwitchState(flippingState);
        return true;
    }

    public void LockMovement(float duration)
    {
        movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + Mathf.Max(0f, duration));
        moveInput = Vector2.zero;
    }

    public void ContinueFacingRotation()
    {
        Quaternion targetRotation;
        if (is2d)
            targetRotation = facingRight ? flipLeftFlat : flipRightFlat;
        else
            targetRotation = facingRight ? flipLeftFlip : flipRightFlip;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
            flipSpeed * Time.fixedDeltaTime);
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            transform.rotation = targetRotation;
    }
    
    void CheckPlayerFalling(PlayerStateManager player) {
        
        if (dimension.IsGrounded()) {
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
        player.rb.position = player.lastGroundedPos;
        player.rb.velocity = Vector3.zero;
        dimension.ResetDepth();
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
        /*
        Gizmos.DrawWireCube(groundCheck.transform.position, this.boxSize );
        
        Vector3 frontCheckPos = transform.position + Vector3.forward * checkDistance;
        Vector3 backCheckPos = transform.position - Vector3.forward * checkDistance;
        Vector3 boxSize = new Vector3(1, 1, checkDistance * 2);

        Gizmos.color = Color.red;  
        Gizmos.DrawWireCube(frontCheckPos, boxSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(backCheckPos, boxSize);
        */
    }
}

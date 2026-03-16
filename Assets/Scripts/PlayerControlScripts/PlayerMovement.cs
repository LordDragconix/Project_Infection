using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed, sprintSpeed, sneakSpeed;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce, jumpCooldown, airMultiplier = 0.6f;
    bool readyToJump = true;

    [Header("Crouching")]
    public float crouchSpeed, crouchYScale = 0.6f;
    float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space, sprintKey = KeyCode.LeftShift,
                   sneakKey = KeyCode.LeftAlt, crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;
    RaycastHit slopeHit;

    public Transform orientation;

    float horizontalInput, verticalInput, moveSpeed;
    Vector3 moveDirection;
    Rigidbody rb;

    public enum MovementState { walking, sprinting, sneaking, crouching, air }
    public MovementState state;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        // Optional but recommended:
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Client-authority: only owner simulates physics
        rb.isKinematic = !IsOwner;
        enabled = IsOwner; // only owner reads input / drives motion

        startYScale = transform.localScale.y;
    }

    void Update()
    {
        if (!IsOwner) return;

        grounded = Physics.Raycast(transform.position, Vector3.down,
                   playerHeight * 0.5f + 0.2f, whatIsGround);

        ReadInput();
        StateHandler();
        SpeedControl();

        rb.linearDamping = grounded ? groundDrag : 0f;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
    }

    void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        if (Input.GetKeyUp(crouchKey))
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }

    void StateHandler()
    {
        if (Input.GetKey(crouchKey)) { state = MovementState.crouching; moveSpeed = crouchSpeed; }
        else if (grounded && Input.GetKey(sprintKey)) { state = MovementState.sprinting; moveSpeed = sprintSpeed; }
        else if (grounded && Input.GetKey(sneakKey)) { state = MovementState.sneaking; moveSpeed = sneakSpeed; }
        else if (grounded) { state = MovementState.walking; moveSpeed = walkSpeed; }
        else { state = MovementState.air; }
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded && OnSlope())
        {
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limited = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void ResetJump() => readyToJump = true;

    bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle > 0f && angle < maxSlopeAngle;
        }
        return false;
    }

    Vector3 GetSlopeMoveDirection() => Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
}

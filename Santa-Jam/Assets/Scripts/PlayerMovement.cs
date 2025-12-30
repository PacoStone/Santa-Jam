using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputManager))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float moveThreshold = 0.01f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.8f;

    [Header("Jump Helpers")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallJumpForce = 6f;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1f, 1.2f);
    [SerializeField] private float wallJumpLockTime = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeCheckDistance = 0.6f;
    [SerializeField] private float ledgeHeight = 1.6f;
    [SerializeField] private Vector3 ledgeClimbOffset = new Vector3(0f, 1.2f, 0.5f);
    [SerializeField] private LayerMask ledgeLayer;

    private CharacterController controller;
    private InputManager input;

    private float verticalVelocity;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isWallJumping;
    private float wallJumpTimer;

    private bool isLedgeGrabbing;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool SprintHeld => input != null && input.sprintHeld;
    public bool JumpPressed => input != null && input.jumpPressed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();
    }
    public bool IsMoving
    {
        get
        {
            if (input == null)
                return false;

            return input.move.sqrMagnitude > moveThreshold;
        }
    }

    private void Update()
    {
        GroundCheck();
        WallCheck();
        Jump();
        Move();
        Gravity();
    }

    private void LateUpdate()
    {
        HandleTimers();

        if (isLedgeGrabbing)
        {
            HandleLedgeClimb();
            return;
        }
    }

    private void HandleTimers()
    {
        if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.deltaTime;
        }
        else
        {
            isWallJumping = false;
        }
    }

    private void Move()
    {
        if (input == null || isWallJumping)
            return;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 moveDirection = forward * input.move.y + right * input.move.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float speed = moveSpeed;

        if (input.sprintHeld)
        {
            speed *= sprintMultiplier;
        }

        controller.Move(moveDirection * speed * Time.deltaTime);
    }

    private void Jump()
    {
        if (input == null)
            return;

        if (input.jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void WallCheck()
    {
        if (IsGrounded || isWallJumping)
            return;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, wallCheckDistance, wallLayer))
        {
            if (jumpBufferTimer > 0f)
            {
                Vector3 jumpDir = (-hit.normal + Vector3.up * wallJumpDirection.y).normalized;

                verticalVelocity = wallJumpForce;
                controller.Move(jumpDir * wallJumpForce * Time.deltaTime);

                isWallJumping = true;
                wallJumpTimer = wallJumpLockTime;
                jumpBufferTimer = 0f;
            }
        }
        CheckLedge(hit);
    }

    private void CheckLedge(RaycastHit wallHit)
    {
        Vector3 ledgeRayOrigin = transform.position + Vector3.up * ledgeHeight;

        if (!Physics.Raycast(ledgeRayOrigin, transform.forward, ledgeCheckDistance, ledgeLayer))
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, ledgeCheckDistance, ledgeLayer))
            {
                StartLedgeGrab(hit);
            }
        }
    }

    private void StartLedgeGrab(RaycastHit hit)
    {
        isLedgeGrabbing = true;
        verticalVelocity = 0f;

        controller.enabled = false;
        transform.position = hit.point - transform.forward * 0.3f;
        controller.enabled = true;
    }

    private void HandleLedgeClimb()
    {
        if (!input.jumpPressed)
            return;

        controller.enabled = false;
        transform.position += transform.forward * ledgeClimbOffset.z;
        transform.position += Vector3.up * ledgeClimbOffset.y;
        controller.enabled = true;

        isLedgeGrabbing = false;
    }

    private void Gravity()
    {
        if (isLedgeGrabbing)
            return;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void GroundCheck()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            coyoteTimer = coyoteTime;
        }
    }
}

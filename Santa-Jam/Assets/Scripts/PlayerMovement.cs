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

    private CharacterController controller;
    private InputManager input;

    private float verticalVelocity;
    private float coyoteTimer;
    private float jumpBufferTimer;

    public bool IsGrounded => controller != null && controller.isGrounded;

    public bool IsMoving
    {
        get
        {
            if (input == null) 
                return false;

            return 
                input.move.sqrMagnitude > moveThreshold;
        }
    }

    public bool SprintHeld => input != null && input.sprintHeld;

    public bool JumpPressed => input != null && input.jumpPressed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();
    }

    private void Update()
    {
        /*
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;
        */

        GroundCheck();
        Jump();
        Move();
        Gravity();
    }

    private void Move()
    {
        if (input == null)
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

    private void Gravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void GroundCheck()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            coyoteTimer = coyoteTime;
        }
    }
}

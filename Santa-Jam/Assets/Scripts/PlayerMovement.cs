using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputManager))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.8f;

    [Header("Jump Helpers")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float stickSensitivity = 180f; // grados/seg aprox
    [SerializeField] private float stickLookSmoothing = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController controller;
    private InputManager input;

    private float verticalVelocity;
    private float cameraPitch;

    // Timers (coyote + buffer)
    private float coyoteTimer;
    private float jumpBufferTimer;

    // Stick smoothing
    private Vector2 smoothedStickLook;
    private Vector2 stickLookVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
        GroundCheck();
        Jump();
        Move();
        Gravity();
    }

    private void Move()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 moveDirection = forward * input.move.y + right * input.move.x;

        // Evita boost en diagonal
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        float speed = moveSpeed;
        if (input.sprintHeld)
            speed *= sprintMultiplier;

        controller.Move(moveDirection * speed * Time.deltaTime);
    }

    private void Look()
    {
        if (cameraTransform == null)
            return;

        Vector2 lookInput = input.look;

        float yawDelta;
        float pitchDelta;

        if (input.usingGamepad)
        {
            // Stick: suavizado + deltaTime
            smoothedStickLook = Vector2.SmoothDamp(smoothedStickLook, lookInput, ref stickLookVelocity, stickLookSmoothing);

            Vector2 stick = smoothedStickLook * stickSensitivity * Time.deltaTime;

            yawDelta = stick.x;
            pitchDelta = stick.y;
        }
        else
        {
            // Mouse: delta por frame, normalmente NO se multiplica por deltaTime
            Vector2 mouse = lookInput * mouseSensitivity;

            yawDelta = mouse.x;
            pitchDelta = mouse.y;
        }

        cameraPitch -= pitchDelta;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up * yawDelta);
    }

    private void Jump()
    {
        // Jump Buffer: si pulsas antes de tocar suelo
        if (input.jumpPressed)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // Coyote: se “recarga” en GroundCheck
        coyoteTimer -= Time.deltaTime;

        // Ejecuta salto si hay buffer + estás en coyote (o grounded)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Consumir buffer/coyote para evitar doble salto accidental
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
            // “Pegado al suelo” típico con CharacterController
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            // Recarga coyote mientras estás grounded
            coyoteTimer = coyoteTime;
        }
    }
}

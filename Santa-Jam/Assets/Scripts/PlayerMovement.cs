using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputManager))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera vcam;

    [Header("Pause UI")]
    [SerializeField] private GameObject pauseMenu;       // "Menú de pausa"
    [SerializeField] private GameObject pauseBackground; // "fondo de pausa"

    [Header("Camera Distance (Sprint)")]
    [SerializeField] private bool syncBaseFromCinemachine = true;
    [SerializeField] private float baseCameraDistance = 2.5f;
    [SerializeField] private float sprintDistanceDelta = 0.5f; // 2.5 -> 3.0
    [SerializeField] private float cameraDistanceSmooth = 8f;

    [Header("Camera Distance (Aim)")]
    [SerializeField] private float aimCameraDistance = 0f;

    [Header("Shoulder Y Bob (Walk/Sprint)")]
    [SerializeField] private float walkBobAmplitude = 0.05f;
    [SerializeField] private float walkBobFrequency = 7f;

    [SerializeField] private float sprintBobAmplitude = 0.10f;
    [SerializeField] private float sprintBobFrequency = 11f;

    [SerializeField] private float shoulderSmooth = 12f;
    [SerializeField] private float moveThreshold = 0.01f;

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
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private float stickLookSmoothing = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController controller;
    private InputManager input;

    private float verticalVelocity;
    private float cameraPitch;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private Vector2 smoothedStickLook;
    private Vector2 stickLookVelocity;

    private CinemachineThirdPersonFollow thirdPersonFollow;

    private float sprintCameraDistance;

    private float baseShoulderY;

    private bool isPaused;

    // Debug (evitar spam)
    private bool wasWalking;
    private bool wasSprinting;
    private bool wasAiming;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (vcam != null)
        {
            thirdPersonFollow = vcam.GetComponent<CinemachineThirdPersonFollow>();

            if (thirdPersonFollow != null && syncBaseFromCinemachine)
            {
                baseCameraDistance = thirdPersonFollow.CameraDistance;
                baseShoulderY = thirdPersonFollow.ShoulderOffset.y;
            }
            else if (thirdPersonFollow != null)
            {
                baseShoulderY = thirdPersonFollow.ShoulderOffset.y;
            }
        }

        sprintCameraDistance = baseCameraDistance + sprintDistanceDelta;

        ApplyPauseUI(false);
    }

    private void Update()
    {
        Pause();

        if (isPaused)
            return;

        Look();
        GroundCheck();
        Jump();
        Move();
        Gravity();
        CameraEffects();
        Shoot();
        DebugTransitions();
    }

    private void Pause()
    {
        if (!input.pausePressed)
            return;

        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;

        ApplyPauseUI(isPaused);

        Debug.Log(isPaused ? "PAUSA: Activada" : "PAUSA: Desactivada");
    }

    private void ApplyPauseUI(bool active)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(active);
        }

        if (pauseBackground != null)
        {
            pauseBackground.SetActive(active);
        }
    }

    private bool IsMoving()
    {
        return 
            input.move.sqrMagnitude > moveThreshold;
    }

    private bool CanAim()
    {
        if (!controller.isGrounded)
            return false;

        if (input.sprintHeld)
            return false;

        if (input.jumpPressed)
            return false;

        // Aquí ya hemos bloqueado sprint, así que con estar grounded vale.
        return true;
    }

    private bool IsAiming()
    {
        return input.aimHeld && CanAim();
    }

    private void Move()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 moveDirection = forward * input.move.y + right * input.move.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float speed = moveSpeed;

        bool isAiming = IsAiming();

        // Si estás apuntando, sprint bloqueado.
        bool canSprint = !isAiming;

        if (input.sprintHeld && canSprint)
        {
            speed *= sprintMultiplier;
        }

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
            smoothedStickLook = Vector2.SmoothDamp(smoothedStickLook, lookInput, ref stickLookVelocity, stickLookSmoothing);

            Vector2 stick = smoothedStickLook * stickSensitivity * Time.deltaTime;
            yawDelta = stick.x;
            pitchDelta = stick.y;
        }
        else
        {
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

    private void CameraEffects()
    {
        if (thirdPersonFollow == null)
            return;

        bool isMoving = IsMoving();
        bool isAiming = IsAiming();
        bool isSprinting = isMoving && input.sprintHeld && !isAiming;

        // Distancia:
        // - Si apuntas: 0
        // - Si sprint: distancia de sprint
        // - Si normal: base
        float targetDistance = baseCameraDistance;

        if (isAiming)
        {
            targetDistance = aimCameraDistance;
        }
        else if (isSprinting)
        {
            targetDistance = sprintCameraDistance;
        }

        thirdPersonFollow.CameraDistance = Mathf.Lerp(thirdPersonFollow.CameraDistance, targetDistance,
            Time.deltaTime * cameraDistanceSmooth);

        // Shoulder Y bob: Walk vs Sprint. Si apuntas -> sin bob (vuelve a base)
        float targetShoulderY = baseShoulderY;

        if (!isAiming && isMoving)
        {
            float amp = isSprinting ? sprintBobAmplitude : walkBobAmplitude;
            float freq = isSprinting ? sprintBobFrequency : walkBobFrequency;

            float bob = Mathf.Sin(Time.time * freq) * amp;
            targetShoulderY = baseShoulderY + bob;
        }

        Vector3 shoulder = thirdPersonFollow.ShoulderOffset;
        shoulder.y = Mathf.Lerp(shoulder.y, targetShoulderY, Time.deltaTime * shoulderSmooth);
        thirdPersonFollow.ShoulderOffset = shoulder;
    }

    private void Shoot()
    {
        // “Se pueda disparar mientras se apunta”
        // Aquí lo dejo como trigger mínimo (log) para que lo conectes a tu sistema de armas.
        if (!input.attackPressed)
            return;

        if (!IsAiming())
            return;

        Debug.Log("Shoot: ON (disparo mientras apuntas)");
    }

    private void DebugTransitions()
    {
        bool isMoving = IsMoving();
        bool isAiming = IsAiming();
        bool isSprinting = isMoving && input.sprintHeld && !isAiming;
        bool isWalking = isMoving && !isSprinting && !isAiming;

        if (isWalking && !wasWalking)
            Debug.Log("Walk: ON (ShoulderOffset.y bob activo)");
        if (!isWalking && wasWalking)
            Debug.Log("Walk: OFF");

        if (isSprinting && !wasSprinting)
            Debug.Log("Sprint: ON (ShoulderOffset.y bob más rápido/agitado)");
        if (!isSprinting && wasSprinting)
            Debug.Log("Sprint: OFF");

        if (isAiming && !wasAiming)
            Debug.Log("Aim: ON (CameraDistance -> 0)");
        if (!isAiming && wasAiming)
            Debug.Log("Aim: OFF (CameraDistance -> normal)");

        wasWalking = isWalking;
        wasSprinting = isSprinting;
        wasAiming = isAiming;
    }
}

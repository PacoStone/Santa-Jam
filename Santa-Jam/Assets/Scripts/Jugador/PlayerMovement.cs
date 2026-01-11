using System;
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
    [SerializeField] private float wallJumpForce = 6f; // componente vertical
    [SerializeField] private float wallAwayForce = 7f; // componente perpendicular a la pared
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1f, 1.2f);
    [SerializeField] private float wallJumpLockTime = 0.25f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Wall Jump - Control")]
    [SerializeField, Range(0.15f, 0.75f)]
    private float minIntoWallDot = 0.35f; // "tengo intención de ir hacia la pared"
    [SerializeField, Range(0f, 1f)]
    private float lateralControl = 0.65f; // 0 = solo perpendicular, 1 = mucho lateral

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeCheckDistance = 0.6f;
    [SerializeField] private float ledgeHeight = 1.6f;
    [SerializeField] private Vector3 ledgeClimbOffset = new Vector3(0f, 1.2f, 0.5f);
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private bool allowLedgeShimmy = false;
    [SerializeField] private float ledgeShimmySpeed = 2.2f;
    [SerializeField] private float ledgeShimmyProbeDistance = 0.6f;
    [SerializeField] private float ledgeHangBackOffset = 0.3f; // separa el player de la pared

    // ====== Events para animación/UI ======
    public event Action OnJumped;
    public event Action OnLanded;

    private CharacterController controller;
    private InputManager input;

    private float verticalVelocity;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isWallJumping;
    private float wallJumpTimer;

    private bool isLedgeGrabbing;
    private Vector3 ledgeWallNormal;
    private Vector3 ledgeShimmyDir;

    private bool wasGrounded;

    public bool SprintHeld => input != null && input.sprintHeld;
    public bool IsGrounded => controller.isGrounded;
    public bool IsJumping => !controller.isGrounded;


    public bool IsMoving
    {
        get
        {
            if (input == null) return false;
            return input.move.sqrMagnitude > moveThreshold;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();
        wasGrounded = controller != null && controller.isGrounded;
    }

    private void Update()
    {
        GroundCheck();     // también actualiza coyote
        HandleLandEvent(); // detecta aterrizaje por transición

        HandleTimers();
        WallCheck();

        Jump();  // salto normal (coyote+buffer)
        Move();
        Gravity();
    }

    private void LateUpdate()
    {
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
        if (input == null || isWallJumping || isLedgeGrabbing)
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
        if (input == null || isLedgeGrabbing)
            return;

        // Buffer
        if (input.jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Coyote ya lo actualiza GroundCheck(), aquí solo lo consumimos
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            DoGroundJump();
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void DoGroundJump()
    {
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        OnJumped?.Invoke();
    }

    private void WallCheck()
    {
        if (input == null)
            return;

        if (IsGrounded || isWallJumping || isLedgeGrabbing)
            return;

        // Raycast frontal para detectar pared
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, wallCheckDistance, wallLayer))
            return;

        // Dirección de movimiento (mismo criterio que Move())
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 moveDir = (forward * input.move.y) + (right * input.move.x);

        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        float intoWall = (moveDir.sqrMagnitude > 0.0001f) ? Vector3.Dot(moveDir, -hit.normal) : 0f;

        // Wall jump si hay buffer y estás “yendo hacia la pared”
        if (jumpBufferTimer > 0f && intoWall > minIntoWallDot)
        {
            Vector3 awayFromWall = hit.normal.normalized;

            Vector3 tangent = Vector3.ProjectOnPlane(moveDir, awayFromWall);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                tangent.Normalize();
            }

            Vector3 horizontalDir = awayFromWall;

            if (tangent.sqrMagnitude > 0.0001f)
            {
                horizontalDir = Vector3.Lerp(awayFromWall, (awayFromWall + tangent).normalized, lateralControl).normalized;
            }

            Vector3 impulse = (horizontalDir * wallAwayForce * wallJumpDirection.x) + (Vector3.up * wallJumpForce * wallJumpDirection.y);

            verticalVelocity = 0f;
            controller.Move(impulse * Time.deltaTime);

            verticalVelocity = wallJumpForce * wallJumpDirection.y;

            isWallJumping = true;
            wallJumpTimer = wallJumpLockTime;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            OnJumped?.Invoke();
        }

        // Ledge grab check
        CheckLedge(hit);
    }

    private void CheckLedge(RaycastHit wallHit)
    {
        Vector3 ledgeRayOrigin = transform.position + Vector3.up * ledgeHeight;

        // Arriba no hay pared -> potencial filo
        if (!Physics.Raycast(ledgeRayOrigin, transform.forward, ledgeCheckDistance, ledgeLayer))
        {
            // A la altura normal sí hay pared -> confirmamos borde
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

        ledgeWallNormal = hit.normal;
        ledgeShimmyDir = Vector3.Cross(Vector3.up, ledgeWallNormal).normalized;

        controller.enabled = false;

        Vector3 hangPos = hit.point + ledgeWallNormal * ledgeHangBackOffset;
        hangPos.y = transform.position.y;
        transform.position = hangPos;

        controller.enabled = true;
    }

    private void HandleLedgeClimb()
    {
        if (input == null)
            return;

        // Soltarse: hacia atrás
        if (input.move.y < -0.2f)
        {
            isLedgeGrabbing = false;
            verticalVelocity = -2f;
            return;
        }

        // Shimmy opcional: solo izquierda/derecha por el filo
        if (allowLedgeShimmy)
        {
            float x = input.move.x;

            if (Mathf.Abs(x) > 0.2f)
            {
                Vector3 dir = ledgeShimmyDir * Mathf.Sign(x);

                Vector3 probeOrigin = transform.position + Vector3.up * (controller.height * 0.5f);
                bool hasWall = Physics.Raycast(probeOrigin, -ledgeWallNormal, ledgeShimmyProbeDistance, ledgeLayer);

                Vector3 topProbeOrigin = transform.position + Vector3.up * ledgeHeight;
                bool hasFreeTop = !Physics.Raycast(topProbeOrigin, -ledgeWallNormal, ledgeShimmyProbeDistance, ledgeLayer);

                if (hasWall && hasFreeTop)
                {
                    controller.Move(dir * ledgeShimmySpeed * Time.deltaTime);
                }
            }
        }

        // Subir: salto
        if (input.jumpPressed)
        {
            controller.enabled = false;

            transform.position += transform.forward * ledgeClimbOffset.z;
            transform.position += Vector3.up * ledgeClimbOffset.y;

            controller.enabled = true;

            isLedgeGrabbing = false;

            // Considera esto como “acción de salto” a nivel de animación
            OnJumped?.Invoke();
        }
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
                verticalVelocity = -2f;

            coyoteTimer = coyoteTime;
        }
        else
        {
            // Si estás en el aire, el coyote baja (pero solo aquí; no lo “machacamos” en Jump())
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void HandleLandEvent()
    {
        bool groundedNow = controller != null && controller.isGrounded;

        // “Landed” = venías en aire y ahora tocas suelo
        if (!wasGrounded && groundedNow && !isLedgeGrabbing)
        {
            OnLanded?.Invoke();
        }

        wasGrounded = groundedNow;
    }
}

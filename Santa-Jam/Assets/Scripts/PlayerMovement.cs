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

    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool SprintHeld => input != null && input.sprintHeld;
    public bool JumpPressed => input != null && input.jumpPressed;

    public bool IsMoving
    {
        get
        {
            if (input == null)
                return false;

            return input.move.sqrMagnitude > moveThreshold;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<InputManager>();
    }

    private void Update()
    {
        GroundCheck();
        HandleTimers();
        WallCheck();
        Jump();
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

        if (jumpBufferTimer > 0f && intoWall > minIntoWallDot)
        {
            Vector3 awayFromWall = hit.normal.normalized;

            // Tangente de la pared según input (permite salida lateral/diagonal)
            Vector3 tangent = Vector3.ProjectOnPlane(moveDir, awayFromWall);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                tangent.Normalize();
            }

            // Mezcla: siempre despega (away) + control lateral (tangent)
            // lateralControl = 0 -> solo perpendicular
            // lateralControl = 1 -> mucha componente lateral (si hay input)
            Vector3 horizontalDir = awayFromWall;

            if (tangent.sqrMagnitude > 0.0001f)
            {
                horizontalDir = Vector3.Lerp(awayFromWall, (awayFromWall + tangent).normalized, lateralControl).normalized;
            }

            // Impulso final usando tus variables del inspector
            Vector3 impulse = (horizontalDir * wallAwayForce * wallJumpDirection.x) + (Vector3.up * wallJumpForce * wallJumpDirection.y);

            verticalVelocity = 0f;
            controller.Move(impulse * Time.deltaTime);

            // Para que Gravity() siga con un arco consistente
            verticalVelocity = wallJumpForce * wallJumpDirection.y;

            isWallJumping = true;
            wallJumpTimer = wallJumpLockTime;

            // Consumimos buffer
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
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
    }
}

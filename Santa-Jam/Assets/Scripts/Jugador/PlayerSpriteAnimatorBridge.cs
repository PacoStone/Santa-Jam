using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sources (auto if null)")]
    [SerializeField] private InputManager input;
    [SerializeField] private PlayerMovementController movement;
    [SerializeField] private PlayerHealthController health;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string sprintParam = "Sprinting";
    [SerializeField] private string aimParam = "Aiming";
    [SerializeField] private string deadParam = "Dead";
    [SerializeField] private string hurt = "Hurt";

    public enum SpeedMode
    {
        SignedInputY,      // +forward / -back from input (good for simple setups)
        SignedWorldMotion  // +forward / -back from actual movement (recommended in 3D)
    }

    [Header("Tuning")]
    [SerializeField] private SpeedMode speedMode = SpeedMode.SignedWorldMotion;

    [Tooltip("Noise filter for Speed and flip. Keep near your Animator thresholds (0.1).")]
    [SerializeField] private float deadZone = 0.12f;

    [Tooltip("If true, the animator 'Grounded' bool will represent AIRBORNE instead of grounded. " +
             "Enable this with your current graph (Idle -> Grounded_Prota when Grounded==true).")]
    [SerializeField] private bool animatorGroundedMeansAirborne = true;

    [Header("Sprite Facing")]
    [SerializeField] private bool flipWithMoveX = true;

    private Transform _root;
    private Vector3 _lastRootPos;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (input == null) input = GetComponentInParent<InputManager>();
        if (movement == null) movement = GetComponentInParent<PlayerMovementController>();
        if (health == null) health = GetComponentInParent<PlayerHealthController>();

        _root = movement != null ? movement.transform : transform;
        _lastRootPos = _root.position;
    }

    private void Update()
    {
        if (animator == null || input == null || movement == null)
            return;

        // 1) Speed (signed)
        float speed = ComputeSignedSpeed();
        if (Mathf.Abs(speed) < deadZone)
        {
            speed = 0f;
        }
        animator.SetFloat(speedParam, speed);

        // 2) Grounded
        bool isGrounded = movement.IsGrounded;
        bool animatorGrounded = animatorGroundedMeansAirborne ? !isGrounded : isGrounded;
        animator.SetBool(groundedParam, animatorGrounded);

        // 3) Sprint / Aim
        animator.SetBool(sprintParam, movement.SprintHeld);
        animator.SetBool(aimParam, input.aimHeld);

        // 4) Dead (simple, no reflection)
        // Si tu PlayerHealthController no expone IsDead, esta línea no compilará.
        // En ese caso, te digo abajo cómo dejarlo compatible.
        bool isDead = GetIsDeadSafe();
        animator.SetBool(deadParam, isDead);

        // 5) Sprite flip (solo con input X)
        if (flipWithMoveX && spriteRenderer != null)
        {
            float x = input.move.x;
            if (Mathf.Abs(x) > deadZone)
                spriteRenderer.flipX = x < 0f;
        }
    }

    private float ComputeSignedSpeed()
    {
        switch (speedMode)
        {
            case SpeedMode.SignedInputY:
                {
                    // Respeta exactamente tu Animator: Speed > 0.1 forward, Speed < -0.1 back
                    return input.move.y;
                }

            case SpeedMode.SignedWorldMotion:
            default:
                {
                    // Movimiento real proyectado en el forward del jugador (solo en plano XZ)
                    Vector3 delta = _root.position - _lastRootPos;
                    _lastRootPos = _root.position;

                    Vector3 planar = new Vector3(delta.x, 0f, delta.z);
                    if (planar.sqrMagnitude < 0.0000005f)
                        return 0f;

                    Vector3 fwd = _root.forward;
                    fwd.y = 0f;
                    fwd.Normalize();

                    // [-1..1] aprox (según dirección). Esto encaja perfecto con umbrales +/-0.1
                    return Vector3.Dot(planar.normalized, fwd);
                }
        }
    }

    private bool GetIsDeadSafe()
    {
        // Opción 1 (ideal): que tu PlayerHealthController tenga una propiedad pública IsDead.
        // Si existe, úsala.
        // Si no existe, devuelve false y no bloquea animaciones.
        if (health == null) return false;

        // Intenta leer "IsDead" si existe, sin reflection pesada (usamos SendMessage? no; esto es simple).
        // Para mantenerlo “humano” y estable, no infiero por nombres raros.
        // Si quieres, te lo adapto exactamente a tu PlayerHealthController cuando me pegues su código.
        var type = health.GetType();
        var prop = type.GetProperty("IsDead");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(health);

        return false;
    }
}

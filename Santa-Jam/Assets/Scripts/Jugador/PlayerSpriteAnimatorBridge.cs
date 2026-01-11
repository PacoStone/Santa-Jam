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
    [SerializeField] private string jumpingParam = "Jumping";
    [SerializeField] private string sprintParam = "Sprinting";
    [SerializeField] private string aimParam = "Aiming";
    [SerializeField] private string deadParam = "Dead";
    [SerializeField] private string hurtParam = "Hurt";

    [Header("Tuning")]
    [SerializeField] private float deadZone = 0.1f;

    private Transform root;
    private Vector3 lastRootPos;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        if (!input) input = GetComponentInParent<InputManager>();
        if (!movement) movement = GetComponentInParent<PlayerMovementController>();
        if (!health) health = GetComponentInParent<PlayerHealthController>();

        root = movement.transform;
        lastRootPos = root.position;
    }

    private void Update()
    {
        if (!animator || !movement)
            return;

        UpdateSpeed();
        UpdateGroundJumpState();
        UpdateMiscStates();
        UpdateFlip();
    }

    private void UpdateSpeed()
    {
        Vector3 delta = root.position - lastRootPos;
        lastRootPos = root.position;

        Vector3 planar = new Vector3(delta.x, 0f, delta.z);
        float speed = planar.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        if (speed < deadZone)
            speed = 0f;

        animator.SetFloat(speedParam, speed);
    }

    private void UpdateGroundJumpState()
    {
        bool grounded = movement.IsGrounded;
        bool jumping = movement.IsJumping;

        animator.SetBool(groundedParam, grounded);
        animator.SetBool(jumpingParam, jumping);
    }

    private void UpdateMiscStates()
    {
        animator.SetBool(sprintParam, movement.SprintHeld);
        animator.SetBool(aimParam, input.aimHeld);

        if (health != null)
            animator.SetBool(deadParam, health.IsDead);
    }

    private void UpdateFlip()
    {
        if (!spriteRenderer)
            return;

        float x = input.move.x;

        if (Mathf.Abs(x) > deadZone)
            spriteRenderer.flipX = x < 0f;
    }

    public void TriggerHurt()
    {
        if (animator && !string.IsNullOrEmpty(hurtParam))
            animator.SetTrigger(hurtParam);
    }
}

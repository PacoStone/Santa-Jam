using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteAnimatorBridge : MonoBehaviour
{
    [Serializable]
    public struct WeaponAnimatorOverride
    {
        public WeaponDa weapon;
        public AnimatorOverrideController overrideController;
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sources (auto if null)")]
    [SerializeField] private InputManager input;
    [SerializeField] private PlayerMovementController movement;
    [SerializeField] private PlayerHealthController health;
    [SerializeField] private WeaponInventoryController weaponInventory;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string sprintParam = "Sprinting";
    [SerializeField] private string aimParam = "Aiming";
    [SerializeField] private string deadParam = "Dead";
    [SerializeField] private string hurt = "Hurt";

    public enum SpeedMode
    {
        SignedInputY,
        SignedWorldMotion
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

    [Header("Weapon ? Animator Override")]
    [Tooltip("Si no hay match, se usa el controller base (el que tenga el Animator al arrancar).")]
    [SerializeField] private WeaponAnimatorOverride[] weaponOverrides;

    private Transform _root;
    private Vector3 _lastRootPos;

    private RuntimeAnimatorController _baseController;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (input == null) input = GetComponentInParent<InputManager>();
        if (movement == null) movement = GetComponentInParent<PlayerMovementController>();
        if (health == null) health = GetComponentInParent<PlayerHealthController>();
        if (weaponInventory == null) weaponInventory = GetComponentInParent<WeaponInventoryController>();

        _root = movement != null ? movement.transform : transform;
        _lastRootPos = _root.position;

        if (animator != null)
            _baseController = animator.runtimeAnimatorController;
    }

    private void OnEnable()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponDataChanged += HandleWeaponChanged;
    }

    private void OnDisable()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponDataChanged -= HandleWeaponChanged;
    }

    private void Start()
    {
        // Aplica override inicial (si empiezas con arma equipada)
        if (weaponInventory != null)
            HandleWeaponChanged(weaponInventory.CurrentWeaponData, weaponInventory.CurrentIndex);
    }

    private void Update()
    {
        if (animator == null || input == null || movement == null)
            return;

        // 1) Speed (signed)
        float speed = ComputeSignedSpeed();
        if (Mathf.Abs(speed) < deadZone) speed = 0f;
        animator.SetFloat(speedParam, speed);

        // 2) Grounded
        bool isGrounded = movement.IsGrounded;
        bool animatorGrounded = animatorGroundedMeansAirborne ? !isGrounded : isGrounded;
        animator.SetBool(groundedParam, animatorGrounded);

        // 3) Sprint / Aim
        animator.SetBool(sprintParam, movement.SprintHeld);
        animator.SetBool(aimParam, input.aimHeld);

        // 4) Dead
        animator.SetBool(deadParam, GetIsDeadSafe());

        // 5) Sprite flip
        if (flipWithMoveX && spriteRenderer != null)
        {
            float x = input.move.x;
            if (Mathf.Abs(x) > deadZone)
                spriteRenderer.flipX = x < 0f;
        }
    }

    private void HandleWeaponChanged(WeaponDa weapon, int index)
    {
        if (animator == null) return;

        RuntimeAnimatorController target = _baseController;

        if (weapon != null && weaponOverrides != null)
        {
            for (int i = 0; i < weaponOverrides.Length; i++)
            {
                if (weaponOverrides[i].weapon == weapon && weaponOverrides[i].overrideController != null)
                {
                    target = weaponOverrides[i].overrideController;
                    break;
                }
            }
        }

        ApplyControllerPreservingState(target);
    }

    private void ApplyControllerPreservingState(RuntimeAnimatorController target)
    {
        if (animator.runtimeAnimatorController == target || target == null)
            return;

        // Captura estado actual para minimizar “saltos”
        AnimatorStateInfo s0 = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = s0.fullPathHash;
        float normalized = s0.normalizedTime;
        float t = normalized - Mathf.Floor(normalized); // 0..1

        animator.runtimeAnimatorController = target;

        // Reproduce el mismo estado (si existe en el controller base/override; con override suele existir)
        animator.Play(stateHash, 0, t);
        animator.Update(0f); // fuerza evaluación inmediata
    }

    private float ComputeSignedSpeed()
    {
        switch (speedMode)
        {
            case SpeedMode.SignedInputY:
                return input.move.y;

            case SpeedMode.SignedWorldMotion:
            default:
                {
                    Vector3 delta = _root.position - _lastRootPos;
                    _lastRootPos = _root.position;

                    Vector3 planar = new Vector3(delta.x, 0f, delta.z);
                    if (planar.sqrMagnitude < 0.0000005f)
                        return 0f;

                    Vector3 fwd = _root.forward;
                    fwd.y = 0f;
                    fwd.Normalize();

                    return Vector3.Dot(planar.normalized, fwd);
                }
        }
    }

    private bool GetIsDeadSafe()
    {
        if (health == null) return false;

        var type = health.GetType();
        var prop = type.GetProperty("IsDead");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(health);

        return false;
    }

    // Opcional: si quieres disparar Hurt desde otros scripts:
    public void TriggerHurt()
    {
        if (animator != null && !string.IsNullOrEmpty(hurt))
            animator.SetTrigger(hurt);
    }
}

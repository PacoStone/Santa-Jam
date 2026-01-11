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
    [SerializeField] private string jumpingParam = "Jumping";
    [SerializeField] private string sprintParam = "Sprinting";
    [SerializeField] private string aimParam = "Aiming";
    [SerializeField] private string rechargeParam = "Recharging";
    [SerializeField] private string deadParam = "Dead";
    [SerializeField] private string hurtParam = "Hurt";

    [Header("Weapon → Animator Override")]
    [Tooltip("Si no hay match para el arma actual, se usa el controller base (el que tenga el Animator al inicio).")]
    [SerializeField] private WeaponAnimatorOverride[] weaponOverrides;

    [Header("Tuning")]
    [SerializeField] private float deadZone = 0.1f;

    private Transform root;
    private Vector3 lastRootPos;

    private RuntimeAnimatorController baseController;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        if (!input) input = GetComponentInParent<InputManager>();
        if (!movement) movement = GetComponentInParent<PlayerMovementController>();
        if (!health) health = GetComponentInParent<PlayerHealthController>();
        if (!weaponInventory) weaponInventory = GetComponentInParent<WeaponInventoryController>();

        if (animator != null)
            baseController = animator.runtimeAnimatorController;

        root = movement != null ? movement.transform : transform;
        lastRootPos = root.position;
    }

    private void OnEnable()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponDataChanged += HandleWeaponDataChanged;
    }

    private void OnDisable()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponDataChanged -= HandleWeaponDataChanged;
    }

    private void Start()
    {
        // “Seguro” por si el evento se disparó antes de habilitar este componente
        if (weaponInventory != null)
            HandleWeaponDataChanged(weaponInventory.CurrentWeaponData, weaponInventory.CurrentIndex);
    }

    private void Update()
    {
        if (!animator || movement == null)
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
        animator.SetBool(groundedParam, movement.IsGrounded);
        animator.SetBool(jumpingParam, movement.IsJumping);
    }

    private void UpdateMiscStates()
    {
        animator.SetBool(sprintParam, movement.SprintHeld);
        animator.SetBool(aimParam, input != null && input.aimHeld);

        if (health != null)
            animator.SetBool(deadParam, health.IsDead);

        // -------------------------
        // RECARGA (esto es lo nuevo)
        // -------------------------
        bool isReloading = IsReloadingNow();
        animator.SetBool(rechargeParam, isReloading);
    }

    private bool IsReloadingNow()
    {
        if (weaponInventory == null)
            return false;

        int idx = weaponInventory.CurrentIndex;
        if (idx < 0) return false;

        // Tu inventory ya expone el progreso de recarga del arma equipada. :contentReference[oaicite:1]{index=1}
        float t = weaponInventory.GetReloadProgress01(idx);

        // Si t>0, estás en recarga; si vuelve a 0, terminó.
        // (Si tu WeaponRuntime devuelve 0 el primer frame de recarga, dime y meto un “latch” de 0.05s.)
        return t > 0f;
    }

    private void UpdateFlip()
    {
        if (!spriteRenderer || input == null)
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

    // -----------------------------
    // AnimatorOverrideController per weapon
    // -----------------------------
    private void HandleWeaponDataChanged(WeaponDa weapon, int index)
    {
        if (animator == null)
            return;

        RuntimeAnimatorController target = baseController;

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
        if (target == null) return;
        if (animator.runtimeAnimatorController == target) return;

        AnimatorStateInfo s0 = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = s0.fullPathHash;

        float t = s0.normalizedTime;
        t = t - Mathf.Floor(t); // 0..1

        animator.runtimeAnimatorController = target;

        animator.Play(stateHash, 0, t);
        animator.Update(0f);
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
//using UnityEngine.InputSystem.DualSense;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    [HideInInspector] public Vector2 move;
    [HideInInspector] public Vector2 look;

    [HideInInspector] public bool jumpPressed;
    [HideInInspector] public bool sprintHeld;

    [HideInInspector] public bool crouchHeld;
    [HideInInspector] public bool aimHeld;
    [HideInInspector] public bool attackPressed;

    [HideInInspector] public bool pausePressed;

    [HideInInspector] public bool usingGamepad;

    [HideInInspector] public bool showGunsPressed;
    [HideInInspector] public bool reloadPressed;

    // NEW: weapon switching
    [HideInInspector] public bool changeGunLeftPressed;
    [HideInInspector] public bool changeGunRightPressed;

    public enum GlyphSet
    {
        PC_KeyboardMouse,
        PlayStation,
        Xbox
    }

    public GlyphSet CurrentGlyphSet { get; private set; } = GlyphSet.PC_KeyboardMouse;

    /// <summary>Se dispara cuando cambia el esquema/dispositivo y por tanto el set de símbolos.</summary>
    public event Action<GlyphSet> OnGlyphSetChanged;

    public PlayerInput PlayerInput => playerInput;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private InputAction crouchAction;
    private InputAction aimAction;
    private InputAction attackAction;

    private InputAction pauseAction;
    private InputAction showGunsAction;
    private InputAction reloadAction;

    // NEW
    private InputAction changeGunLeftAction;
    private InputAction changeGunRightAction;

    private string _lastControlScheme;
    private GlyphSet _lastGlyphSet;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        var actions = playerInput.actions;

        moveAction = actions.FindAction("Move", true);
        lookAction = actions.FindAction("Look", true);
        jumpAction = actions.FindAction("Jump", true);
        sprintAction = actions.FindAction("Sprint", true);

        crouchAction = actions.FindAction("Crouch", true);
        aimAction = actions.FindAction("Aim", true);
        attackAction = actions.FindAction("Attack", true);

        pauseAction = actions.FindAction("Pausar", true);
        showGunsAction = actions.FindAction("ShowGuns", true);
        reloadAction = actions.FindAction("Reload", true);

        // NEW (deben existir en tu Input Actions)
        changeGunLeftAction = actions.FindAction("ChangeGunLeft", true);
        changeGunRightAction = actions.FindAction("ChangeGunRight", true);

        _lastGlyphSet = CurrentGlyphSet;
        _lastControlScheme = playerInput.currentControlScheme;
    }

    private void Update()
    {
        usingGamepad = playerInput != null && playerInput.currentControlScheme == "Gamepad";

        move = moveAction.ReadValue<Vector2>();
        look = lookAction.ReadValue<Vector2>();

        jumpPressed = jumpAction.WasPressedThisFrame();
        sprintHeld = sprintAction.IsPressed();

        crouchHeld = crouchAction.IsPressed();
        aimHeld = aimAction.IsPressed();
        attackPressed = attackAction.WasPressedThisFrame();

        pausePressed = pauseAction.WasPressedThisFrame();
        showGunsPressed = showGunsAction.WasPressedThisFrame();
        reloadPressed = reloadAction.WasPressedThisFrame();

        // NEW
        changeGunLeftPressed = changeGunLeftAction.WasPressedThisFrame();
        changeGunRightPressed = changeGunRightAction.WasPressedThisFrame();

        UpdateGlyphSetIfNeeded();
    }

    private void UpdateGlyphSetIfNeeded()
    {
        string scheme = playerInput != null ? playerInput.currentControlScheme : string.Empty;

        // Si no hay gamepad: PC
        GlyphSet target = GlyphSet.PC_KeyboardMouse;

        if (usingGamepad)
        {
            // Detecta tipo de gamepad real (PS vs Xbox) usando el device que tiene PlayerInput
            Gamepad gp = GetActiveGamepadFromPlayerInput();

            if (gp is DualSenseGamepadHID || gp is DualShockGamepad)
                target = GlyphSet.PlayStation;
            else
                target = GlyphSet.Xbox;
        }

        CurrentGlyphSet = target;

        bool schemeChanged = scheme != _lastControlScheme;
        bool glyphChanged = CurrentGlyphSet != _lastGlyphSet;

        if (schemeChanged || glyphChanged)
        {
            _lastControlScheme = scheme;
            _lastGlyphSet = CurrentGlyphSet;
            OnGlyphSetChanged?.Invoke(CurrentGlyphSet);
        }
    }

    private Gamepad GetActiveGamepadFromPlayerInput()
    {
        if (playerInput == null) return Gamepad.current;

        // PlayerInput.devices suele contener el device activo según control scheme
        foreach (var d in playerInput.devices)
        {
            if (d is Gamepad g) return g;
        }

        return Gamepad.current;
    }
}

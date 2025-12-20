using UnityEngine;
using UnityEngine.InputSystem;

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

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private InputAction crouchAction;
    private InputAction aimAction;
    private InputAction attackAction;

    private InputAction pauseAction;

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
    }
}

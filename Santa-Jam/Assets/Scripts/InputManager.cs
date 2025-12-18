using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    [HideInInspector] public Vector2 move;
    [HideInInspector] public Vector2 look;
    [HideInInspector] public bool jumpPressed;
    [HideInInspector] public bool sprintHeld;

    [HideInInspector] public bool usingGamepad;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        var actions = playerInput.actions;

        moveAction = actions.FindAction("Move", true);
        lookAction = actions.FindAction("Look", true);
        jumpAction = actions.FindAction("Jump", true);
        sprintAction = actions.FindAction("Sprint", true);
    }

    private void Update()
    {
        // Control scheme: suele ser "Keyboard&Mouse" y "Gamepad" (según tu asset)
        usingGamepad = playerInput != null && playerInput.currentControlScheme == "Gamepad";

        move = moveAction.ReadValue<Vector2>();
        look = lookAction.ReadValue<Vector2>();

        jumpPressed = jumpAction.WasPressedThisFrame();
        sprintHeld = sprintAction.IsPressed();
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WeaponWheelController : MonoBehaviour
{
    public Animator anim;
    private bool weaponWheelSelected = false;

    public Image selectedImage;
    public Sprite noImage;
    public static int weaponID;

    public InputManager input;

    [Header("Action Maps")]
    [SerializeField] private string gameplayMapName = "Jugador";
    [SerializeField] private string uiMapName = "UI";
    [SerializeField] private string showGunsActionName = "ShowGuns";

    private PlayerInput playerInput;
    private InputAction showGunsAction;

    void Awake()
    {
        // Intentamos obtener PlayerInput desde el mismo objeto que el InputManager
        if (input != null)
            playerInput = input.GetComponent<PlayerInput>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        RefreshShowGunsAction();
        ApplyWheelState(false);
    }

    void Update()
    {
        // Por seguridad, si cambió el action map o el action quedó null, lo re-buscamos
        if (playerInput != null && (showGunsAction == null || showGunsAction.actionMap != playerInput.currentActionMap))
            RefreshShowGunsAction();

        if (showGunsAction != null && showGunsAction.WasPressedThisFrame())
        {
            weaponWheelSelected = !weaponWheelSelected;
            ApplyWheelState(weaponWheelSelected);
        }

        switch (weaponID)
        {
            case 0:
                selectedImage.sprite = noImage;
                break;
            case 1:
                Debug.Log("Pistola");
                break;
            case 2:
                Debug.Log("Granada");
                break;
            case 3:
                Debug.Log("Escopeta");
                break;
            case 4:
                Debug.Log("Rifle");
                break;
        }
    }

    private void ApplyWheelState(bool open)
    {
        anim.SetBool("OpenWeaponWheel", open);

        if (playerInput != null)
        {
            // Cambia de action map para que el UI module use “UI” al abrir.
            playerInput.SwitchCurrentActionMap(open ? uiMapName : gameplayMapName);
            RefreshShowGunsAction();
        }

        // Cursor para que el ratón interactúe con el UI
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void RefreshShowGunsAction()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
        {
            showGunsAction = null;
            return;
        }

        showGunsAction = playerInput.currentActionMap.FindAction(showGunsActionName, false);
    }
}

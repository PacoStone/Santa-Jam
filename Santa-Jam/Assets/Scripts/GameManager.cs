using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private InputManager playerInput;

    [Header("Pause UI")]
    [SerializeField] private GameObject pauseMenu; // "Menú de pausa"
    [SerializeField] private GameObject pauseBackground; // "fondo de pausa"

    [Header("Gameplay Toggles")]
    [SerializeField] private bool aimAssistEnabled = true;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Opcional: si se cambia de escena
        // DontDestroyOnLoad(gameObject);

        ApplyPause(false);
    }

    private void Update()
    {
        if (playerInput == null)
            return;

        if (!playerInput.pausePressed)
            return;

        ApplyPause(!IsPaused);
    }

    private void ApplyPause(bool paused)
    {
        IsPaused = paused;

        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        if (pauseMenu != null)
            pauseMenu.SetActive(paused);

        if (pauseBackground != null)
            pauseBackground.SetActive(paused);

        Debug.Log(paused ? "PAUSA: Activada (GameManager)" : "PAUSA: Desactivada (GameManager)");
    }
    public bool AimAssistEnabled
    {
        get => aimAssistEnabled;
        set => aimAssistEnabled = value;
    }

    public void SetAimAssistEnabled(bool enabled)
    {
        aimAssistEnabled = enabled;
    }
}

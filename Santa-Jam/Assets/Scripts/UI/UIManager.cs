using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Refs (Input / Player)")]
    [SerializeField] private InputManager inputManager;     // Tu InputManager
    [SerializeField] private PlayerInput playerInput;       // PlayerInput del jugador

    [Header("HUD - Money (TMP)")]
    [Tooltip("Arrastra aquí el TextMeshProUGUI 'money' de tu Canvas.")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Pause UI Roots")]
    [SerializeField] private GameObject pauseMenuRoot;      // Root general del menú pausa
    [SerializeField] private GameObject basicButtonsRoot;   // EmptyObject: Continuar / Opciones / Salir
    [SerializeField] private GameObject optionsRoot;        // EmptyObject: Fullscreen / Brillo / Volumen / Atrás

    [Header("Action Maps")]
    [SerializeField] private string gameplayMapName = "Player";
    [SerializeField] private string uiMapName = "UI";

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorOnPause = true;

    [Header("Blur (URP Volume)")]
    [Tooltip("Volume que SOLO se usa en pausa o el que quieras manipular para el blur.")]
    [SerializeField] private Volume pauseVolume;
    [SerializeField, Range(0f, 1f)] private float pausedWeight = 1f;
    [SerializeField] private float blendSeconds = 0.12f;

    [Header("Options UI Controls")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("0..1 recomendado.")]
    [SerializeField] private Slider brightnessSlider;
    [Tooltip("0..1 recomendado.")]
    [SerializeField] private Slider volumeSlider;

    [Header("Brightness (URP)")]
    [Tooltip("Volume que contiene Color Adjustments (Post Exposure). Puede ser tu Global Volume.")]
    [SerializeField] private Volume brightnessVolume;
    [Tooltip("Post Exposure mínimo cuando el slider está a 0.")]
    [SerializeField] private float minPostExposure = -2f;
    [Tooltip("Post Exposure máximo cuando el slider está a 1.")]
    [SerializeField] private float maxPostExposure = 2f;

    [Header("Volume (Audio)")]
    [Tooltip("Si lo asignas, se usará AudioMixer. Si no, se usará AudioListener.volume.")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("Nombre del parámetro expuesto en el AudioMixer (ej: \"MasterVolume\").")]
    [SerializeField] private string mixerExposedVolumeParam = "MasterVolume";
    [Tooltip("dB mínimo para slider a 0.")]
    [SerializeField] private float minVolumeDb = -80f;
    [Tooltip("dB máximo para slider a 1.")]
    [SerializeField] private float maxVolumeDb = 0f;

    [Header("Options Slide Animation")]
    [Tooltip("RectTransform del EmptyObject 'options'. Si lo dejas vacío, se obtiene de optionsRoot.")]
    [SerializeField] private RectTransform optionsPanel;

    [Tooltip("Los 4 EmptyObjects (RectTransform) que quieres desplazar al pulsar Opciones (según tu imagen).")]
    [SerializeField] private RectTransform[] otherRootsToShift;

    [Tooltip("Cuánto se desplaza el panel options hacia la derecha al entrar.")]
    [SerializeField] private float optionsSlideX = 650f;

    [Tooltip("Cuánto se desplazan los otros 4 empties (normalmente hacia la izquierda: valor negativo).")]
    [SerializeField] private float othersSlideX = -650f;

    [Tooltip("Duración del deslizamiento.")]
    [SerializeField] private float slideSeconds = 0.25f;

    [Tooltip("Curva del deslizamiento (ease in/out).")]
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Si está activado, durante la animación mantiene ambos grupos activos para que el slide sea visible.")]
    [SerializeField] private bool keepBothGroupsActiveDuringSlide = true;

    [Tooltip("Si tus RectTransforms están en 'Stretch' y anchoredPosition da guerra, usa localPosition.")]
    [SerializeField] private bool slideUsingLocalPosition = false;

    public bool IsPaused { get; private set; }
    private bool pauseLocked;

    private Coroutine _blendRoutine;
    private Coroutine _slideRoutine;

    private ColorAdjustments _colorAdjustments;

    // Posiciones base (para volver “tal y como estaba antes”)
    private Vector3 _optionsBasePos;
    private Vector3[] _othersBasePos;

    private bool _inOptions = false;

    // --------------------------
    //  UNITY LIFECYCLE
    // --------------------------
    private void Awake()
    {
        // UI estado inicial
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
        if (optionsRoot != null) optionsRoot.SetActive(false);

        // Resolve optionsPanel si no está asignado
        if (optionsPanel == null && optionsRoot != null)
            optionsPanel = optionsRoot.GetComponent<RectTransform>();

        CacheBaseSlidePositions();

        // Pause volume preparado (blur)
        if (pauseVolume != null)
        {
            pauseVolume.enabled = true;
            pauseVolume.weight = 0f;
        }

        // Cache de Color Adjustments (Post Exposure) en URP
        if (brightnessVolume != null && brightnessVolume.profile != null)
        {
            brightnessVolume.profile.TryGet(out _colorAdjustments);
        }

        // Listeners UI
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness01);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume01);
    }

    private void OnEnable()
    {
        BindScoreManager();
        RefreshMoneyTextInstant();
    }

    private void OnDisable()
    {
        UnbindScoreManager();
    }

    private void Start()
    {
        // Sincroniza UI con estado actual
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        // Defaults razonables si están a 0 por inspector
        if (brightnessSlider != null && brightnessSlider.value <= 0f)
            brightnessSlider.value = 0.5f;

        if (volumeSlider != null && volumeSlider.value <= 0f)
            volumeSlider.value = 1f;

        // Aplica inmediatamente valores iniciales a settings
        if (brightnessSlider != null) SetBrightness01(brightnessSlider.value);
        if (volumeSlider != null) SetVolume01(volumeSlider.value);

        // Asegura HUD correcto al arrancar
        RefreshMoneyTextInstant();
    }

    private void Update()
    {
        if (pauseLocked) return;

        if (inputManager != null && inputManager.pausePressed)
        {
            TogglePause();
        }
    }

    // --------------------------
    //  SCORE UI
    // --------------------------
    private void BindScoreManager()
    {
        if (ScoreManager.Instance == null) return;

        // Evita doble suscripción
        ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
    }

    private void UnbindScoreManager()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int cents, string formatted)
    {
        if (moneyText == null) return;
        moneyText.text = formatted;
    }

    private void RefreshMoneyTextInstant()
    {
        if (moneyText == null) return;

        if (ScoreManager.Instance != null)
            moneyText.text = ScoreManager.Instance.GetFormattedScore();
    }

    // --------------------------
    //  PAUSE CORE
    // --------------------------
    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        // Menú raíz
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(paused);

        // Al pausar, vuelve al panel básico
        if (paused)
        {
            _inOptions = false;
            ShowBasicMenuInstant(); // importante: resetea posiciones y estados
        }
        else
        {
            HideAllPausePanels();
        }

        // Time
        Time.timeScale = paused ? 0f : 1f;

        // Action Maps
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(paused ? uiMapName : gameplayMapName);

        // Cursor
        if (unlockCursorOnPause)
        {
            Cursor.visible = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // Blur (weight del volume URP)
        if (pauseVolume != null)
        {
            if (_blendRoutine != null) StopCoroutine(_blendRoutine);
            _blendRoutine = StartCoroutine(BlendPauseVolume(paused ? pausedWeight : 0f));
        }
    }

    public void SetPauseLocked(bool locked)
    {
        pauseLocked = locked;
    }

    public void EnterModalUI(bool freezeTime = true)
    {
        pauseLocked = true;

        HideAllPausePanels();

        if (freezeTime)
            Time.timeScale = 0f;

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(uiMapName);

        if (unlockCursorOnPause)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ExitModalUI(bool resumeTime = true)
    {
        if (resumeTime)
            Time.timeScale = 1f;

        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(gameplayMapName);

        if (unlockCursorOnPause)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        pauseLocked = false;
    }

    private IEnumerator BlendPauseVolume(float target)
    {
        if (pauseVolume == null)
            yield break;

        float start = pauseVolume.weight;
        float t = 0f;

        while (t < blendSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = blendSeconds <= 0f ? 1f : (t / blendSeconds);
            pauseVolume.weight = Mathf.Lerp(start, target, a);
            yield return null;
        }

        pauseVolume.weight = target;
    }

    // --------------------------
    //  PANELS
    // --------------------------
    private void HideAllPausePanels()
    {
        if (basicButtonsRoot != null) basicButtonsRoot.SetActive(false);
        if (optionsRoot != null) optionsRoot.SetActive(false);
    }

    private void ShowBasicMenuInstant()
    {
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = null;

        if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
        if (optionsRoot != null) optionsRoot.SetActive(false);

        RestoreBaseSlidePositions();
    }

    // --------------------------
    //  BUTTON CALLBACKS
    // --------------------------
    public void OnContinueClicked()
    {
        SetPaused(false);
    }

    public void OnOptionsClicked()
    {
        if (!IsPaused) return;
        if (_inOptions) return;

        _inOptions = true;

        if (keepBothGroupsActiveDuringSlide)
        {
            if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
            if (optionsRoot != null) optionsRoot.SetActive(true);
        }
        else
        {
            if (optionsRoot != null) optionsRoot.SetActive(true);
        }

        StartOptionsSlide(toOptions: true);
    }

    public void OnBackFromOptionsClicked()
    {
        if (!IsPaused) return;
        if (!_inOptions) return;

        _inOptions = false;

        if (keepBothGroupsActiveDuringSlide)
        {
            if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
            if (optionsRoot != null) optionsRoot.SetActive(true);
        }
        else
        {
            if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
        }

        StartOptionsSlide(toOptions: false);
    }

    // --------------------------
    //  OPTIONS
    // --------------------------
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetBrightness01(float v01)
    {
        if (_colorAdjustments == null)
        {
            if (brightnessVolume != null && brightnessVolume.profile != null)
                brightnessVolume.profile.TryGet(out _colorAdjustments);

            if (_colorAdjustments == null) return;
        }

        float exposure = Mathf.Lerp(minPostExposure, maxPostExposure, Mathf.Clamp01(v01));
        _colorAdjustments.postExposure.overrideState = true;
        _colorAdjustments.postExposure.value = exposure;
    }

    public void SetVolume01(float v01)
    {
        v01 = Mathf.Clamp01(v01);

        if (audioMixer != null && !string.IsNullOrEmpty(mixerExposedVolumeParam))
        {
            float db = (v01 <= 0.0001f)
                ? minVolumeDb
                : Mathf.Lerp(minVolumeDb, maxVolumeDb, Mathf.InverseLerp(0.0001f, 1f, v01));

            audioMixer.SetFloat(mixerExposedVolumeParam, Mathf.Clamp(db, minVolumeDb, maxVolumeDb));
        }
        else
        {
            AudioListener.volume = v01;
        }
    }

    // --------------------------
    //  SLIDE LOGIC (OPTIONS)
    // --------------------------
    private void CacheBaseSlidePositions()
    {
        if (optionsPanel != null)
            _optionsBasePos = GetRTPos(optionsPanel);

        if (otherRootsToShift != null && otherRootsToShift.Length > 0)
        {
            _othersBasePos = new Vector3[otherRootsToShift.Length];
            for (int i = 0; i < otherRootsToShift.Length; i++)
            {
                _othersBasePos[i] = otherRootsToShift[i] != null ? GetRTPos(otherRootsToShift[i]) : Vector3.zero;
            }
        }
        else
        {
            _othersBasePos = new Vector3[0];
        }
    }

    private void RestoreBaseSlidePositions()
    {
        if (optionsPanel != null)
            SetRTPos(optionsPanel, _optionsBasePos);

        for (int i = 0; i < otherRootsToShift.Length; i++)
        {
            if (otherRootsToShift[i] == null) continue;
            SetRTPos(otherRootsToShift[i], _othersBasePos[i]);
        }
    }

    private void StartOptionsSlide(bool toOptions)
    {
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(CoSlideOptions(toOptions));
    }

    private IEnumerator CoSlideOptions(bool toOptions)
    {
        float d = Mathf.Max(0.0001f, slideSeconds);
        float t = 0f;

        Vector3 optionsFrom = optionsPanel != null ? GetRTPos(optionsPanel) : Vector3.zero;

        Vector3[] othersFrom = new Vector3[otherRootsToShift.Length];
        for (int i = 0; i < otherRootsToShift.Length; i++)
            othersFrom[i] = otherRootsToShift[i] != null ? GetRTPos(otherRootsToShift[i]) : Vector3.zero;

        Vector3 optionsTo = _optionsBasePos + (toOptions ? new Vector3(optionsSlideX, 0f, 0f) : Vector3.zero);

        Vector3[] othersTo = new Vector3[otherRootsToShift.Length];
        for (int i = 0; i < otherRootsToShift.Length; i++)
            othersTo[i] = _othersBasePos[i] + (toOptions ? new Vector3(othersSlideX, 0f, 0f) : Vector3.zero);

        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / d);
            float e = (slideEase != null) ? slideEase.Evaluate(a) : a;

            if (optionsPanel != null)
                SetRTPos(optionsPanel, Vector3.LerpUnclamped(optionsFrom, optionsTo, e));

            for (int i = 0; i < otherRootsToShift.Length; i++)
            {
                if (otherRootsToShift[i] == null) continue;
                SetRTPos(otherRootsToShift[i], Vector3.LerpUnclamped(othersFrom[i], othersTo[i], e));
            }

            yield return null;
        }

        if (optionsPanel != null) SetRTPos(optionsPanel, optionsTo);
        for (int i = 0; i < otherRootsToShift.Length; i++)
        {
            if (otherRootsToShift[i] == null) continue;
            SetRTPos(otherRootsToShift[i], othersTo[i]);
        }

        if (toOptions)
        {
            if (basicButtonsRoot != null) basicButtonsRoot.SetActive(false);
            if (optionsRoot != null) optionsRoot.SetActive(true);
        }
        else
        {
            if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
            if (optionsRoot != null) optionsRoot.SetActive(false);
        }

        _slideRoutine = null;
    }

    private Vector3 GetRTPos(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;
        return slideUsingLocalPosition ? rt.localPosition : (Vector3)rt.anchoredPosition;
    }

    private void SetRTPos(RectTransform rt, Vector3 value)
    {
        if (rt == null) return;

        if (slideUsingLocalPosition)
            rt.localPosition = value;
        else
            rt.anchoredPosition = (Vector2)value;
    }
}

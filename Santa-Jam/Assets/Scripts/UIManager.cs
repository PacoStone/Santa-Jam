using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Refs (Input / Player)")]
    [SerializeField] private InputManager inputManager;     // Tu InputManager
    [SerializeField] private PlayerInput playerInput;       // PlayerInput del jugador

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

    public bool IsPaused { get; private set; }

    private Coroutine _blendRoutine;
    private ColorAdjustments _colorAdjustments;

    private void Awake()
    {
        // UI estado inicial
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
        if (basicButtonsRoot != null) basicButtonsRoot.SetActive(true);
        if (optionsRoot != null) optionsRoot.SetActive(false);

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
    }

    private void Update()
    {
        if (inputManager != null && inputManager.pausePressed)
        {
            TogglePause();
        }
    }

    //  PAUSE CORE
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
            ShowBasicMenu();
        }
        else
        {
            HideAllPausePanels();
        }

        // Time
        Time.timeScale = paused ? 0f : 1f;

        // Action Maps (para despausar con la misma tecla, la acción de pausa debe existir también en UI map)
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
            StopCoroutine(_blendRoutine);
            _blendRoutine = StartCoroutine(BlendPauseVolume(paused ? pausedWeight : 0f));
        }
    }

    private IEnumerator BlendPauseVolume(float target)
    {
        if (pauseVolume == null) 
            yield break;

        float start = pauseVolume.weight;
        float t = 0f;

        // Unscaled para funcionar con timeScale=0
        while (t < blendSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = blendSeconds <= 0f ? 1f : (t / blendSeconds);
            pauseVolume.weight = Mathf.Lerp(start, target, a);
            yield return null;
        }

        pauseVolume.weight = target;
    }

    private void HideAllPausePanels()
    {
        basicButtonsRoot.SetActive(false);
        optionsRoot.SetActive(false);
    }

    private void ShowBasicMenu()
    {
        basicButtonsRoot.SetActive(true);
        optionsRoot.SetActive(false);
    }

    private void ShowOptionsMenu()
    {
        basicButtonsRoot.SetActive(false);
        optionsRoot.SetActive(true);
    }

    //  BUTTON CALLBACKS

    // Botón: Continuar
    public void OnContinueClicked()
    {
        SetPaused(false);
    }

    // Botón: Opciones
    public void OnOptionsClicked()
    {
        ShowOptionsMenu();
    }

    // Botón: Atrás (Opciones)
    public void OnBackFromOptionsClicked()
    {
        ShowBasicMenu();
    }

    //  OPTIONS
    // Toggle: Pantalla completa
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // Slider: Brillo (0..1) -> URP Color Adjustments / Post Exposure
    public void SetBrightness01(float v01)
    {
        if (_colorAdjustments == null)
        {
            // Intenta cachear de nuevo por si el profile se asignó después
            if (brightnessVolume != null && brightnessVolume.profile != null)
                brightnessVolume.profile.TryGet(out _colorAdjustments);

            if (_colorAdjustments == null) return;
        }

        float exposure = Mathf.Lerp(minPostExposure, maxPostExposure, Mathf.Clamp01(v01));

        // Asegura que está activo el override y aplica valor
        _colorAdjustments.postExposure.overrideState = true;
        _colorAdjustments.postExposure.value = exposure;
    }

    // Slider: Volumen (0..1)
    public void SetVolume01(float v01)
    {
        v01 = Mathf.Clamp01(v01);

        if (audioMixer != null && !string.IsNullOrEmpty(mixerExposedVolumeParam))
        {
            // 0..1 -> dB (curva log, con corte en 0)
            float db = (v01 <= 0.0001f) ? minVolumeDb : Mathf.Lerp(minVolumeDb, maxVolumeDb, Mathf.InverseLerp(0.0001f, 1f, v01));

            audioMixer.SetFloat(mixerExposedVolumeParam, Mathf.Clamp(db, minVolumeDb, maxVolumeDb));
        }
        else
        {
            AudioListener.volume = v01;
        }
    }
}

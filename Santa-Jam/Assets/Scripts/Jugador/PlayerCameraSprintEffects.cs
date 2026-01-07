using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerMovementController))]
public class PlayerCameraSprintEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Volume globalVolume;

    [Header("Sprint Condition")]
    [SerializeField] private bool requireMovingToApply = true;

    [Header("Motion Blur (URP)")]
    [SerializeField] private bool enableMotionBlur = true;
    [Range(0f, 1f)]
    [SerializeField] private float sprintBlurIntensity = 0.6f;
    [Range(0f, 0.2f)]
    [SerializeField] private float sprintBlurClamp = 0.05f;

    [Header("Dynamic FOV")]
    [SerializeField] private bool enableDynamicFov = true;
    [SerializeField] private float sprintFovBonus = 10f;
    [SerializeField] private float fovSmooth = 10f;

    [Header("Optional Vignette")]
    [SerializeField] private bool enableVignette = false;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteNormal = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteSprint = 0.2f;
    [SerializeField] private float vignetteSmooth = 10f;

    [Header("Smoothing")]
    [SerializeField] private float blurSmooth = 12f;

    private PlayerMovementController movement;

    private MotionBlur motionBlur;
    private Vignette vignette;

    private float baseFov;

    private void Awake()
    {
        movement = GetComponent<PlayerMovementController>();

        SetupCamera();
        SetupVolume();
    }

    private void Update()
    {
        /*
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;
        */

        bool sprinting = IsSprintingNow();

        HandleMotionBlur(sprinting);
        HandleDynamicFov(sprinting);
        HandleVignette(sprinting);
    }

    #region Setup

    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            baseFov = mainCamera.fieldOfView;
        }
    }

    private void SetupVolume()
    {
        if (globalVolume == null)
            return;

        if (globalVolume.profile == null)
            return;

        globalVolume.profile.TryGet(out motionBlur);
        globalVolume.profile.TryGet(out vignette);

        ApplyMotionBlurInstant(0f, sprintBlurClamp);
        ApplyVignetteInstant(vignetteNormal);
    }

    #endregion

    #region Sprint State

    private bool IsSprintingNow()
    {
        if (movement == null)
            return false;

        if (!movement.SprintHeld)
            return false;

        if (requireMovingToApply && !movement.IsMoving)
            return false;

        return true;
    }

    #endregion

    #region Motion Blur

    private void HandleMotionBlur(bool sprinting)
    {
        if (!enableMotionBlur)
            return;

        if (motionBlur == null)
            return;

        float targetIntensity = sprinting ? sprintBlurIntensity : 0f;

        motionBlur.intensity.value = Mathf.Lerp(
            motionBlur.intensity.value,
            targetIntensity,
            Time.deltaTime * blurSmooth
        );

        // Clamp lo dejamos fijo para evitar que el blur “rompa” demasiado la imagen,
        // especialmente en shooters.
        motionBlur.clamp.value = sprintBlurClamp;
    }

    private void ApplyMotionBlurInstant(float intensity, float clamp)
    {
        if (motionBlur == null)
            return;

        motionBlur.intensity.value = intensity;
        motionBlur.clamp.value = clamp;
    }

    #endregion

    #region Dynamic FOV

    private void HandleDynamicFov(bool sprinting)
    {
        if (!enableDynamicFov)
            return;

        if (mainCamera == null)
            return;

        float targetFov = baseFov + (sprinting ? sprintFovBonus : 0f);

        mainCamera.fieldOfView = Mathf.Lerp(
            mainCamera.fieldOfView,
            targetFov,
            Time.deltaTime * Mathf.Max(0.01f, fovSmooth)
        );
    }

    #endregion

    #region Vignette

    private void HandleVignette(bool sprinting)
    {
        if (!enableVignette)
            return;

        if (vignette == null)
            return;

        float target = sprinting ? vignetteSprint : vignetteNormal;

        vignette.intensity.value = Mathf.Lerp(
            vignette.intensity.value,
            target,
            Time.deltaTime * Mathf.Max(0.01f, vignetteSmooth)
        );
    }

    private void ApplyVignetteInstant(float intensity)
    {
        if (vignette == null)
            return;

        vignette.intensity.value = intensity;
    }

    #endregion
}

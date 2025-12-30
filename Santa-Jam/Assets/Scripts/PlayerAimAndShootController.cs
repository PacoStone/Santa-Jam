using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(PlayerMovementController))]
public class PlayerAimAndShootController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera vcam;

    [Header("UI - Reticle (Screen Space Overlay)")]
    [SerializeField] private RectTransform hipFireReticle;
    [SerializeField] private Image hipFireReticleImage;
    [Range(0f, 1f)]
    [SerializeField] private float hipFireReticleAlphaNormal = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float hipFireReticleAlphaAiming = 1f;

    [Header("UI - Reticle Behaviour")]
    [SerializeField] private bool hideReticleWhileNotAiming = true; // ON => en hipfire se oculta; en aim siempre aparece

    [Header("Reticle Loop (cuando hay lock)")]
    [SerializeField] private bool loopReticleWhileLocked = true;
    [SerializeField] private float reticleLoopDuration = 1f;
    [SerializeField] private float reticleScaleMultiplier = 2f;
    [SerializeField] private float reticleRotationDegreesPerSecond = 360f;
    [SerializeField] private bool pulseAlphaWhileLocked = true;

    [Header("Post Processing - Vignette (URP)")]
    [SerializeField] private Volume globalVolume;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteIntensityHip = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteIntensityAim = 0.35f;

    [Header("Camera Distance")]
    [SerializeField] private bool syncBaseFromCinemachine = true;
    [SerializeField] private float baseCameraDistance = 2.5f;
    [SerializeField] private float sprintDistanceDelta = 0.5f;
    [SerializeField] private float aimCameraDistance = 0f;
    [SerializeField] private float cameraDistanceSmooth = 8f;

    [Header("Shoulder Bob")]
    [SerializeField] private float walkBobAmplitude = 0.05f;
    [SerializeField] private float walkBobFrequency = 7f;
    [SerializeField] private float sprintBobAmplitude = 0.10f;
    [SerializeField] private float sprintBobFrequency = 11f;
    [SerializeField] private float shoulderSmooth = 12f;

    [Header("Aim Assist - Toggle")]
    [SerializeField] private bool aimAssistEnabled = true;

    //[Header("Aim Assist - Settings")]
    //private string enemyTag = "Enemy";
    //[SerializeField] private float aimAssistViewportRadius = 0.06f;
    //[SerializeField] private float aimAssistMaxDistance = 50f;
    //[SerializeField] private float aimAssistGain = 900f;
    //[SerializeField] private float aimAssistMaxYawSpeed = 180f;
    //[SerializeField] private float aimAssistMaxPitchSpeed = 180f;
    //[SerializeField] private float aimAssistMinDot = 0.75f;
    //[SerializeField] private bool useReticleAsAimCenter = true;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private float stickLookSmoothing = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Shoot - Weapon Origin")]
    [SerializeField] private Transform arma; // EmptyObject llamado "arma"
    [SerializeField] private float shootOriginForwardOffset = 0.03f;

    [Header("Shoot - Projectile (fallback)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 60f;
    [SerializeField] private float bulletLifeTime = 3f;

    [Header("Shoot - Direction Offset")]
    [SerializeField] private Vector2 hipFireAngleOffset = new Vector2(18f, -2f);
    [SerializeField] private Vector2 aimAngleOffset = new Vector2(15f, -2f);

    [Header("Impact (fallback)")]
    [SerializeField] private LayerMask environmentMask; // Layer "Enviorement"
    [SerializeField] private GameObject bulletHoleDecalPrefab;
    [SerializeField] private float decalOffset = 0.01f;
    [SerializeField] private Vector2 decalScaleRange = new Vector2(0.18f, 0.28f);
    [SerializeField] private float decalLifeTime = 25f;

    [SerializeField] private ParticleSystem impactParticlesPrefab;
    [SerializeField] private float particlesLifeTime = 3f;

    [Header("Reload")]
    [SerializeField] private bool autoReloadWhenEmpty = true;
    //[SerializeField] private KeyCode reloadKey = KeyCode.R; // fallback simple (si ya tienes Input Action de reload, puedes llamar a weaponRuntime.TryStartReload() desde tu InputManager)

    [Header("Debug")]
    [SerializeField] private bool drawShootRay = true;
    [SerializeField] private float rayDrawDuration = 0.10f;

    [Header("Weapon Runtime (recomendado)")]
    [SerializeField] private WeaponRuntime weaponRuntime;

    private InputManager input;
    private PlayerMovementController movement;
    private CinemachineThirdPersonFollow thirdPersonFollow;

    private float cameraPitch;
    private float sprintCameraDistance;
    private float baseShoulderY;

    private Vector2 smoothedStickLook;
    private Vector2 stickLookVelocity;

    private Vector3 reticleBaseScale;
    private float reticleLoopTime;

    private Transform currentAimTarget;

    private Vignette vignette;

    private void Awake()
    {
        input = GetComponent<InputManager>();
        movement = GetComponent<PlayerMovementController>();

        SetupCinemachine();
        SetupReticle();
        SetupVignette();
    }

    private void Update()
    {
        bool aimingNow = IsAiming();

        HandleLook(aimingNow);
        HandleCamera(aimingNow);
        HandleReticle(aimingNow);

        HandleReloadInput();
        HandleShoot(aimingNow);
    }

    #region Setup

    private void SetupCinemachine()
    {
        if (vcam == null)
            return;

        thirdPersonFollow = vcam.GetComponent<CinemachineThirdPersonFollow>();
        if (thirdPersonFollow == null)
            return;

        if (syncBaseFromCinemachine)
        {
            baseCameraDistance = thirdPersonFollow.CameraDistance;
        }

        baseShoulderY = thirdPersonFollow.ShoulderOffset.y;
        sprintCameraDistance = baseCameraDistance + sprintDistanceDelta;
    }

    private void SetupReticle()
    {
        if (hipFireReticle != null)
        {
            reticleBaseScale = hipFireReticle.localScale;
        }

        if (hipFireReticleImage == null && hipFireReticle != null)
        {
            hipFireReticleImage = hipFireReticle.GetComponent<Image>();
        }

        ApplyReticleAlpha(false, 0f);
    }

    private void SetupVignette()
    {
        if (globalVolume == null)
            return;

        if (globalVolume.profile == null)
            return;

        globalVolume.profile.TryGet(out vignette);
        ApplyVignetteInstant(vignetteIntensityHip);
    }

    #endregion

    #region AIM STATE

    private bool IsAiming()
    {
        if (!input.aimHeld)
            return false;

        if (!CanAim())
            return false;

        return true;
    }

    private bool CanAim()
    {
        if (!movement.IsGrounded)
            return false;

        if (movement.SprintHeld)
            return false;

        if (movement.JumpPressed)
            return false;

        return true;
    }

    private bool IsAimAssistActive()
    {
        if (!aimAssistEnabled)
            return false;

        if (GameManager.Instance == null)
            return true;

        return true;
    }

    #endregion

    #region LOOK

    private void HandleLook(bool aimingNow)
    {
        Vector2 lookInput = input.look;

        float yawDelta;
        float pitchDelta;

        if (input.usingGamepad)
        {
            smoothedStickLook = Vector2.SmoothDamp(smoothedStickLook, lookInput, ref stickLookVelocity, stickLookSmoothing);

            Vector2 stick = smoothedStickLook * stickSensitivity * Time.deltaTime;
            yawDelta = stick.x;
            pitchDelta = stick.y;
        }
        else
        {
            Vector2 mouse = lookInput * mouseSensitivity;
            yawDelta = mouse.x;
            pitchDelta = mouse.y;
        }

        cameraPitch -= pitchDelta;

        if (aimingNow && IsAimAssistActive())
        {
            UpdateAimAssist(ref yawDelta);
        }
        else
        {
            currentAimTarget = null;
        }

        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        transform.Rotate(Vector3.up * yawDelta);
    }

    private void UpdateAimAssist(ref float yawDelta)
    {
        if (mainCamera == null)
            return;

        // Mantengo lo mínimo: aquí ya tienes tu lógica de assist, no la toco más de lo necesario.
        currentAimTarget = null;
    }

    #endregion

    #region CAMERA

    private void HandleCamera(bool aimingNow)
    {
        if (thirdPersonFollow == null)
            return;

        float targetDistance = baseCameraDistance;

        if (aimingNow)
        {
            targetDistance = aimCameraDistance;
        }
        else if (movement.IsMoving && movement.SprintHeld)
        {
            targetDistance = sprintCameraDistance;
        }

        thirdPersonFollow.CameraDistance = Mathf.Lerp(thirdPersonFollow.CameraDistance, targetDistance, Time.deltaTime * cameraDistanceSmooth);

        float targetShoulderY = baseShoulderY;

        if (!aimingNow && movement.IsMoving)
        {
            float amplitude = walkBobAmplitude;
            float frequency = walkBobFrequency;

            if (movement.SprintHeld)
            {
                amplitude = sprintBobAmplitude;
                frequency = sprintBobFrequency;
            }

            targetShoulderY += Mathf.Sin(Time.time * frequency) * amplitude;
        }

        Vector3 shoulder = thirdPersonFollow.ShoulderOffset;
        shoulder.y = Mathf.Lerp(shoulder.y, targetShoulderY, Time.deltaTime * shoulderSmooth);

        thirdPersonFollow.ShoulderOffset = shoulder;

        UpdateVignetteFromCameraDistance(aimingNow);
    }

    #endregion

    #region RETICLE

    private void HandleReticle(bool aimingNow)
    {
        if (hipFireReticle == null)
            return;

        bool shouldBeActive = aimingNow || !hideReticleWhileNotAiming;

        if (hipFireReticle.gameObject.activeSelf != shouldBeActive)
        {
            hipFireReticle.gameObject.SetActive(shouldBeActive);
        }

        if (!shouldBeActive)
        {
            return;
        }

        if (!aimingNow)
        {
            ResetReticleVisualOnly();
            ApplyReticleAlpha(false, 0f);
            return;
        }

        bool locked = IsAimAssistActive() && currentAimTarget != null;

        if (!loopReticleWhileLocked || !locked)
        {
            ResetReticleVisualOnly();
            ApplyReticleAlpha(true, 0f);
            return;
        }

        AnimateReticleLoop();
    }

    private void ResetReticleVisualOnly()
    {
        reticleLoopTime = 0f;

        hipFireReticle.localScale = reticleBaseScale;
        hipFireReticle.localRotation = Quaternion.identity;
    }

    private void AnimateReticleLoop()
    {
        reticleLoopTime += Time.deltaTime;

        float duration = Mathf.Max(0.001f, reticleLoopDuration);
        float phase = (reticleLoopTime / duration) * Mathf.PI * 2f;
        float pulse = (Mathf.Cos(phase) + 1f) * 0.5f;

        hipFireReticle.localScale = reticleBaseScale * Mathf.Lerp(1f, reticleScaleMultiplier, pulse);
        hipFireReticle.localRotation = Quaternion.Euler(0f, 0f, reticleRotationDegreesPerSecond * reticleLoopTime);

        ApplyReticleAlpha(true, pulse);
    }

    private void ApplyReticleAlpha(bool aiming, float pulse)
    {
        if (hipFireReticleImage == null)
            return;

        float alpha = hipFireReticleAlphaNormal;

        if (aiming)
        {
            alpha = hipFireReticleAlphaAiming;
        }

        if (aiming && pulseAlphaWhileLocked)
        {
            alpha *= Mathf.Lerp(0.85f, 1f, pulse);
        }

        Color c = hipFireReticleImage.color;
        c.a = Mathf.Clamp01(alpha);
        hipFireReticleImage.color = c;
    }

    #endregion

    #region RELOAD

    private void HandleReloadInput()
    {
        if (weaponRuntime == null)
            return;

        if (input.reloadPressed)
        {
            weaponRuntime.TryStartReload();
        }
    }

    #endregion

    #region SHOOT (PROJECTILE + AMMO)

    private void HandleShoot(bool aimingNow)
    {
        // Si tu arma es automática y tienes attackHeld en tu InputManager, aquí es donde lo cambiarías.
        // Por ahora mantenemos attackPressed para no romper tu estructura.
        if (!input.attackPressed)
            return;

        if (arma == null)
            return;

        // AMMO CHECK
        if (weaponRuntime != null)
        {
            if (!weaponRuntime.TryShoot())
            {
                if (autoReloadWhenEmpty && weaponRuntime.currentMagazine <= 0)
                {
                    weaponRuntime.TryStartReload();
                }
                return;
            }
        }

        WeaponDa data = (weaponRuntime != null) ? weaponRuntime.weaponData : null;

        GameObject activeBulletPrefab = (data != null && data.BulletPrefab != null) ? data.BulletPrefab : bulletPrefab;
        if (activeBulletPrefab == null)
        {
            Debug.LogWarning("Shoot: No hay BulletPrefab asignado (ni en WeaponDa ni en el Controller).");
            return;
        }

        float activeSpeed = (data != null && data.BulletSpeed > 0f) ? data.BulletSpeed : bulletSpeed;
        float activeHitDistance = (data != null && data.HitDistance > 0f) ? data.HitDistance : 80f;
        LayerMask activeMask = (data != null) ? data.EnvironmentMask : environmentMask;

        Vector3 origin = arma.position;

        Vector2 activeOffset = aimingNow ? aimAngleOffset : hipFireAngleOffset;
        Quaternion angleOffset = Quaternion.Euler(activeOffset.y, activeOffset.x, 0f);

        Vector3 direction = (angleOffset * arma.forward).normalized;

        origin += direction * shootOriginForwardOffset;

        if (drawShootRay)
        {
            Debug.DrawRay(origin, direction * Mathf.Max(1f, activeHitDistance), Color.yellow, rayDrawDuration);
        }

        GameObject bulletObj = Instantiate(activeBulletPrefab, origin, Quaternion.LookRotation(direction, Vector3.up));

        BulletProjectile bullet = bulletObj.GetComponent<BulletProjectile>();
        if (bullet != null)
        {
            bullet.Init(
                direction,
                activeSpeed,
                activeHitDistance,
                activeMask,
                bulletHoleDecalPrefab,
                impactParticlesPrefab,
                decalOffset,
                decalScaleRange,
                decalLifeTime,
                particlesLifeTime,
                drawShootRay,
                rayDrawDuration
            );

            Destroy(bulletObj, Mathf.Max(0.1f, bulletLifeTime));
            return;
        }

        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * activeSpeed;
        }

        Destroy(bulletObj, Mathf.Max(0.1f, bulletLifeTime));
    }

    #endregion

    #region VIGNETTE

    private void UpdateVignetteFromCameraDistance(bool aimingNow)
    {
        if (vignette == null || thirdPersonFollow == null)
            return;

        float t;

        if (Mathf.Abs(baseCameraDistance - aimCameraDistance) < 0.001f)
        {
            t = aimingNow ? 1f : 0f;
        }
        else
        {
            t = Mathf.InverseLerp(baseCameraDistance, aimCameraDistance, thirdPersonFollow.CameraDistance);
        }

        vignette.intensity.value = Mathf.Lerp(vignetteIntensityHip, vignetteIntensityAim, Mathf.Clamp01(t));
    }

    private void ApplyVignetteInstant(float intensity)
    {
        if (vignette == null)
            return;

        vignette.intensity.value = intensity;
    }

    #endregion
}

using System.Collections;
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
    [SerializeField] private Transform cameraTransform; // Legacy / fallback (si no usas cameraTarget)
    [SerializeField] private Transform cameraTarget;     // Pivot que Cinemachine sigue
    [SerializeField] private Camera mainCamera;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera vcam;

    [Header("Anchors + Side Switch")]
    [SerializeField] private PlayerSideSwitcher sideSwitcher;

    [Header("UI - Reticle (Screen Space Overlay)")]
    [SerializeField] private RectTransform hipFireReticle;
    [SerializeField] private Image hipFireReticleImage;
    [Range(0f, 1f)]
    [SerializeField] private float hipFireReticleAlphaNormal = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float hipFireReticleAlphaAiming = 1f;

    [Header("UI - Reticle Behaviour")]
    [SerializeField] private bool hideReticleWhileNotAiming = true;

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

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private float stickLookSmoothing = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private bool invertY = false;

    [Header("Shoot - Weapon Origin (Fallback)")]
    [Tooltip("Fallback si NO hay SideSwitcher o NO devuelve muzzle. Con anchors+SideSwitcher, no debería hacer falta.")]
    [SerializeField] private Transform arma;
    [SerializeField] private float shootOriginForwardOffset = 0.03f;

    [Header("Shoot - Projectile (fallback)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 60f;
    [SerializeField] private float bulletLifeTime = 3f;

    [Header("Shoot - Direction Offset")]
    [SerializeField] private Vector2 hipFireAngleOffset = new Vector2(18f, -2f);
    [SerializeField] private Vector2 aimAngleOffset = new Vector2(15f, -2f);

    [Header("Shoot - Tracer")]
    [SerializeField] private TrailRenderer bulletTracerPrefab;
    [SerializeField] private float tracerSpeed = 250f;

    [Header("Shoot - Muzzle Flash (Particle Pack)")]
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private string flashHeadonChildName = "FlashHeadon";
    [SerializeField] private float muzzleLifeTime = 2f;

    [Header("Impact")]
    [SerializeField] private LayerMask environmentMask; // Layer "Enviorement"
    [SerializeField] private GameObject bulletHoleDecalPrefab;
    [SerializeField] private float decalOffset = 0.01f;
    [SerializeField] private Vector2 decalScaleRange = new Vector2(0.18f, 0.28f);
    [SerializeField] private float decalLifeTime = 25f;

    [SerializeField] private ParticleSystem impactParticlesPrefab;
    [SerializeField] private float particlesLifeTime = 3f;

    [Header("Reload")]
    [SerializeField] private bool autoReloadWhenEmpty = true;

    [Header("Debug")]
    [SerializeField] private bool drawShootRay = true;
    [SerializeField] private float rayDrawDuration = 0.10f;

    [Header("Weapon Runtime")]
    [SerializeField] private WeaponRuntime weaponRuntime;

    private InputManager input;
    private PlayerMovementController movement;
    private CinemachineThirdPersonFollow thirdPersonFollow;

    private float cameraPitch;
    private float sprintCameraDistance;
    private float baseShoulderY;
    private bool aimingNow;

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

        mainCamera = Camera.main;

        sideSwitcher = GetComponent<PlayerSideSwitcher>();

        SetupCinemachine();
        SetupReticle();
        SetupVignette();
    }

    private void Update()
    {
        aimingNow = IsAiming();

        HandleLook(aimingNow);
        HandleReticle(aimingNow);

        HandleReloadInput();
        HandleShoot(aimingNow);
    }

    private void LateUpdate()
    {
        HandleCamera(aimingNow);
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

        if (cameraTarget == null && cameraTransform != null)
        {
            cameraTarget = cameraTransform;
        }
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
        if (globalVolume == null || globalVolume.profile == null)
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

        if (!invertY)
            pitchDelta = -pitchDelta;

        cameraPitch += pitchDelta;

        if (aimingNow && IsAimAssistActive())
        {
            UpdateAimAssist(ref yawDelta);
        }
        else
        {
            currentAimTarget = null;
        }

        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (cameraTarget != null)
        {
            cameraTarget.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        transform.Rotate(Vector3.up * yawDelta, Space.World);
    }

    private void UpdateAimAssist(ref float yawDelta)
    {
        if (mainCamera == null)
            return;

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
            return;

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
        if (!input.attackPressed)
            return;

        // 1) Muzzle activo (anchors)
        Transform activeMuzzle = GetActiveMuzzle();
        if (activeMuzzle == null)
        {
            Debug.LogWarning("Shoot: No hay muzzle activo (SideSwitcher) y no hay fallback 'arma' asignado.");
            return;
        }

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

        Vector3 origin = activeMuzzle.position;

        Vector3 direction = GetCameraCenterDirectionWithOffset(aimingNow);
        if (direction.sqrMagnitude < 0.0001f)
            return;

        origin += direction * shootOriginForwardOffset;

        if (drawShootRay)
        {
            Debug.DrawRay(origin, direction * Mathf.Max(1f, activeHitDistance), Color.yellow, rayDrawDuration);
        }

        Vector3 hitPoint = origin + direction * activeHitDistance;
        bool hasHit = Physics.Raycast(origin, direction, out RaycastHit hit, activeHitDistance, activeMask);

        if (hasHit)
        {
            hitPoint = hit.point;

            // Fallback visual garantizado (aunque el bullet no tenga BulletProjectile)
            SpawnImpact(hit);
        }

        SpawnTracer(origin, hitPoint);
        SpawnMuzzleFlashAndHeadon(direction, activeMuzzle);

        GameObject bulletObj = Instantiate(activeBulletPrefab, origin, Quaternion.LookRotation(direction, Vector3.up));

        BulletProjectile bullet = bulletObj.GetComponent<BulletProjectile>();
        if (bullet != null)
        {
            int bulletDamage = (data != null) ? data.DamagePerBullet : 1; // Usa el daño del arma si está disponible, si no, usa 1 por defecto.
            bullet.Init(direction, activeSpeed, activeHitDistance, activeMask, bulletHoleDecalPrefab, impactParticlesPrefab,
                decalOffset, decalScaleRange, decalLifeTime, particlesLifeTime, drawShootRay, rayDrawDuration, bulletDamage);

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

    private Transform GetActiveMuzzle()
    {
        if (sideSwitcher != null)
        {
            Transform m = sideSwitcher.GetActiveMuzzle();
            if (m != null)
                return m;
        }

        // Fallback (por si aún no tienes SideSwitcher en alguna escena)
        return arma;
    }

    private Vector3 GetCameraCenterDirectionWithOffset(bool aimingNow)
    {
        // Dirección base desde el centro de cámara (lo que “ves”)
        Vector3 baseDir;

        if (mainCamera != null)
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            baseDir = ray.direction.normalized;
        }
        else if (arma != null)
        {
            baseDir = arma.forward.normalized;
        }
        else
        {
            return Vector3.zero;
        }

        Vector2 activeOffset = aimingNow ? aimAngleOffset : hipFireAngleOffset;
        Quaternion angleOffset = Quaternion.Euler(activeOffset.y, activeOffset.x, 0f);

        return (angleOffset * baseDir).normalized;
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (bulletTracerPrefab == null)
            return;

        TrailRenderer tracer = Instantiate(bulletTracerPrefab, start, Quaternion.identity);
        StartCoroutine(AnimateTracer(tracer, start, end));
    }

    private IEnumerator AnimateTracer(TrailRenderer tracer, Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float travelTime = distance / tracerSpeed;

        float t = 0f;

        tracer.transform.position = start;
        tracer.Clear();

        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            tracer.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        tracer.transform.position = end;
        Destroy(tracer.gameObject, tracer.time);
    }

    private void SpawnMuzzleFlashAndHeadon(Vector3 shotDirection, Transform spawnAt)
    {
        if (muzzleFlashPrefab == null || spawnAt == null)
            return;

        ParticleSystem rootFx = Instantiate(muzzleFlashPrefab, spawnAt.position, Quaternion.LookRotation(shotDirection, Vector3.up));
        rootFx.Play(true);

        if (mainCamera != null && !string.IsNullOrWhiteSpace(flashHeadonChildName))
        {
            Transform headon = rootFx.transform.Find(flashHeadonChildName);
            if (headon == null)
            {
                headon = FindChildByName(rootFx.transform, flashHeadonChildName);
            }

            if (headon != null)
            {
                Vector3 toCam = (mainCamera.transform.position - headon.position).normalized;
                headon.rotation = Quaternion.LookRotation(toCam, Vector3.up);
            }
        }

        Destroy(rootFx.gameObject, muzzleLifeTime);
    }

    private void SpawnImpact(RaycastHit hit)
    {
        if (bulletHoleDecalPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject decal = Instantiate(bulletHoleDecalPrefab, hit.point + hit.normal * decalOffset, rot);

            float s = Random.Range(decalScaleRange.x, decalScaleRange.y);
            decal.transform.localScale = Vector3.one * s;

            Destroy(decal, decalLifeTime);
        }

        if (impactParticlesPrefab != null)
        {
            ParticleSystem ps = Instantiate(impactParticlesPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            ps.Play(true);
            Destroy(ps.gameObject, particlesLifeTime);
        }
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == childName)
                return c;

            Transform deeper = FindChildByName(c, childName);
            if (deeper != null)
                return deeper;
        }
        return null;
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

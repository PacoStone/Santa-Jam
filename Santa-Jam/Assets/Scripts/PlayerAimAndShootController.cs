using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static UnityEditor.ShaderData;

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

    [Header("Aim Assist - Settings")]
    private string enemyTag = "Enemy";
    [SerializeField] private float aimAssistViewportRadius = 0.06f;
    [SerializeField] private float aimAssistMaxDistance = 50f;
    [SerializeField] private float aimAssistGain = 900f;
    [SerializeField] private float aimAssistMaxYawSpeed = 180f;
    [SerializeField] private float aimAssistMaxPitchSpeed = 180f;
    [SerializeField] private float aimAssistMinDot = 0.75f;
    [SerializeField] private bool useReticleAsAimCenter = true;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float stickSensitivity = 180f;
    [SerializeField] private float stickLookSmoothing = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

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
    private Vector3 currentAimPoint;

    private Vignette vignette;

    [Header("Shoot - Raycast")]
    [SerializeField] private float shootRange = 80f;
    [SerializeField] private LayerMask environmentMask; // asigna aquí la Layer "Enviorement"
    [SerializeField] private bool drawShootRay = true;
    [SerializeField] private float rayDrawDuration = 0.10f;

    [Header("Shoot - Impact FX")]
    [SerializeField] private GameObject bulletHoleDecalPrefab;  // prefab del decal
    [SerializeField] private float decalOffset = 0.01f;         // para evitar z-fighting
    [SerializeField] private Vector2 decalScaleRange = new Vector2(0.18f, 0.28f);
    [SerializeField] private float decalLifeTime = 25f;
    [SerializeField] private Transform shootOrigin; // hijo de "arma"
    [SerializeField] private float shootOriginForwardOffset = 0.03f;

    [SerializeField] private ParticleSystem impactParticlesPrefab; // prefab de partículas
    [SerializeField] private float particlesLifeTime = 3f;


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
        /*
        if (GameManager.Instance.IsPaused)
        {
            return;
        }
        */

        bool aimingNow = IsAiming();

        HandleLook(aimingNow);
        HandleCamera(aimingNow);
        HandleReticle(aimingNow);
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
        {
            return;
        }

        if (globalVolume.profile == null)
        {
            return;
        }

        globalVolume.profile.TryGet(out vignette);
        ApplyVignetteInstant(vignetteIntensityHip);
    }
    #endregion

    #region AIM STATE

    private bool IsAiming()
    {
        if (!input.aimHeld)
        {
            return false;
        }

        if (!CanAim())
        {
            return false;
        }

        return true;
    }

    private bool CanAim()
    {
        if (!movement.IsGrounded)
        {
            return false;
        }

        if (movement.SprintHeld)
        {
            return false;
        }

        if (movement.JumpPressed)
        {
            return false;
        }

        return true;
    }

    private bool IsAimAssistActive()
    {
        if (!aimAssistEnabled)
        {
            return false;
        }

        if (GameManager.Instance == null)
        {
            return true;
        }
        /*
        if (!GameManager.Instance.AimAssistEnabled)
        {
            return false;
        }
        */
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

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * yawDelta);
    }

    private void UpdateAimAssist(ref float yawDelta)
    {
        Vector3 targetPoint;
        Transform target = FindAimAssistTarget(out targetPoint);

        currentAimTarget = target;
        currentAimPoint = targetPoint;

        if (target == null)
            return;

        Vector2 correction = GetAimCorrectionFromViewport(targetPoint);

        yawDelta += correction.x * Time.deltaTime;
        cameraPitch -= correction.y * Time.deltaTime;
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
        if (!aimingNow)
        {
            ResetReticle();
            return;
        }

        bool locked = IsAimAssistActive() && currentAimTarget != null;

        if (!loopReticleWhileLocked || !locked)
        {
            ResetReticle();
            ApplyReticleAlpha(true, 0f);
            return;
        }

        AnimateReticleLoop();
    }

    private void ResetReticle()
    {
        reticleLoopTime = 0f;

        if (hipFireReticle != null)
        {
            hipFireReticle.localScale = reticleBaseScale;
            hipFireReticle.localRotation = Quaternion.identity;
        }

        ApplyReticleAlpha(false, 0f);
    }

    private void AnimateReticleLoop()
    {
        reticleLoopTime += Time.deltaTime;

        float duration = Mathf.Max(0.001f, reticleLoopDuration);
        float phase = (reticleLoopTime / duration) * Mathf.PI * 2f;
        float pulse = (Mathf.Cos(phase) + 1f) * 0.5f;

        if (hipFireReticle != null)
        {
            hipFireReticle.localScale = reticleBaseScale * Mathf.Lerp(1f, reticleScaleMultiplier, pulse);

            hipFireReticle.localRotation = Quaternion.Euler(0f, 0f, reticleRotationDegreesPerSecond * reticleLoopTime);
        }

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

    #region SHOOT

    private void HandleShoot(bool aimingNow)
    {
        if (!aimingNow)
            return;

        if (!input.attackPressed)
            return;

        Debug.Log("Shoot");

        // 1) Dirección EXACTA del apuntado del jugador (cámara)
        Vector2 aimCenter = GetAimCenterViewport();
        Ray aimRay = mainCamera.ViewportPointToRay(new Vector3(aimCenter.x, aimCenter.y, 0f));

        Vector3 shootDir = aimRay.direction.normalized;

        Vector3 origin = shootOrigin != null ? shootOrigin.position : mainCamera.transform.position;

        // Evitar que el ray nazca dentro de colliders
        origin += shootDir * shootOriginForwardOffset;

        // 3) Debug visual
        if (drawShootRay)
        {
            Debug.DrawRay(origin, shootDir * shootRange, Color.yellow, rayDrawDuration);
        }

        // 4) Raycast REAL (misma dirección que el apuntado)
        if (Physics.Raycast(origin, shootDir, out RaycastHit hit, shootRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (drawShootRay)
            {
                Debug.DrawLine(origin, hit.point, Color.red, rayDrawDuration);
            }

            int envLayer = LayerMask.NameToLayer("Enviorement");
            if (envLayer != -1 && hit.collider.gameObject.layer == envLayer)
            {
                SpawnBulletHoleDecal(hit);
                SpawnImpactParticles(hit);
            }
        }
    }

    private void SpawnBulletHoleDecal(RaycastHit hit)
    {
        if (bulletHoleDecalPrefab == null)
            return;

        Vector3 pos = hit.point + hit.normal * decalOffset;
        Quaternion rot = Quaternion.LookRotation(-hit.normal, Vector3.up);

        // Rotación aleatoria alrededor de la normal para que no se repita el patrón
        rot *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        GameObject decal = Instantiate(bulletHoleDecalPrefab, pos, rot);

        float s = Random.Range(decalScaleRange.x, decalScaleRange.y);
        decal.transform.localScale = new Vector3(s, s, s);

        Destroy(decal, Mathf.Max(0.5f, decalLifeTime));
    }

    private void SpawnImpactParticles(RaycastHit hit)
    {
        if (impactParticlesPrefab == null)
            return;

        Quaternion rot = Quaternion.LookRotation(hit.normal);
        ParticleSystem fx = Instantiate(impactParticlesPrefab, hit.point, rot);

        Destroy(fx.gameObject, Mathf.Max(0.5f, particlesLifeTime));
    }

    #endregion

    #region AIM ASSIST CORE

    private Transform FindAimAssistTarget(out Vector3 bestPoint)
    {
        bestPoint = Vector3.zero;

        Vector2 center = GetAimCenterViewport();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            Transform enemy = enemies[i].transform;
            Vector3 point = GetEnemyAimPoint(enemy);

            Vector3 toTarget = point - mainCamera.transform.position;
            float distance = toTarget.magnitude;

            if (distance > aimAssistMaxDistance)
            {
                continue; //corta la iteración actual del bucle y pasa directamente a la siguiente.
                //Si este enemigo está detrás de la cámara, ignóralo completamente y pasa al siguiente enemigo.
            }

            Vector3 dir = toTarget.normalized;

            if (Vector3.Dot(mainCamera.transform.forward, dir) < aimAssistMinDot)
            {
                continue;
            }

            Vector3 viewport = mainCamera.WorldToViewportPoint(point);

            if (viewport.z <= 0f)
            {
                continue;
            }

            float viewportDistance =
                Vector2.Distance(new Vector2(viewport.x, viewport.y), center);

            if (viewportDistance > aimAssistViewportRadius)
            {
                continue;
            }

            float score = viewportDistance * 10f + distance * 0.01f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
                bestPoint = point;
            }
        }

        return bestTarget;
    }

    private Vector3 GetEnemyAimPoint(Transform enemy)
    {
        Collider collider = enemy.GetComponentInChildren<Collider>();

        if (collider != null)
        {
            return collider.bounds.center;
        }

        Renderer renderer = enemy.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            return renderer.bounds.center;
        }

        return enemy.position;
    }

    private Vector2 GetAimCorrectionFromViewport(Vector3 targetPoint)
    {
        Vector3 viewport = mainCamera.WorldToViewportPoint(targetPoint);

        if (viewport.z <= 0f)
        {
            return Vector2.zero;
        }

        Vector2 center = GetAimCenterViewport();
        Vector2 error = new Vector2(
            viewport.x - center.x,
            viewport.y - center.y
        );

        float yawSpeed = error.x * aimAssistGain;
        float pitchSpeed = error.y * aimAssistGain;

        yawSpeed = Mathf.Clamp(yawSpeed, -aimAssistMaxYawSpeed, aimAssistMaxYawSpeed);
        pitchSpeed = Mathf.Clamp(pitchSpeed, -aimAssistMaxPitchSpeed, aimAssistMaxPitchSpeed);

        return new Vector2(yawSpeed, pitchSpeed);
    }

    private Vector2 GetAimCenterViewport()
    {
        if (!useReticleAsAimCenter)
        {
            return 
                new Vector2(0.5f, 0.5f);
        }

        if (hipFireReticle == null)
        {
            return 
                new Vector2(0.5f, 0.5f);
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, hipFireReticle.position);

        return 
            new Vector2(screen.x / Screen.width, screen.y / Screen.height);
    }
    #endregion

    #region VIGNETTE

    private void UpdateVignetteFromCameraDistance(bool aimingNow)
    {
        if (vignette == null)
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

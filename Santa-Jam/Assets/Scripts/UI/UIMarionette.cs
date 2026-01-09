using UnityEngine;

public class UIMarionette : MonoBehaviour
{
    [Header("Targets (UI RectTransforms)")]
    [SerializeField] private RectTransform prota;
    [SerializeField] private RectTransform sombras;

    [Header("Overall")]
    [Tooltip("Velocidad general del movimiento orgánico.")]
    [SerializeField] private float speed = 0.35f;

    [Tooltip("Suavizado al aplicar el offset. 0 = inmediato, 20+ = muy suave.")]
    [Range(0f, 30f)]
    [SerializeField] private float damping = 12f;

    [Header("Prota - Amplitudes")]
    [SerializeField] private Vector2 protaPositionAmplitude = new Vector2(6f, 4f);
    [SerializeField] private float protaRotationAmplitude = 1.6f;

    [Header("Sombras - Amplitudes")]
    [SerializeField] private Vector2 sombrasPositionAmplitude = new Vector2(10f, 6f);
    [SerializeField] private float sombrasRotationAmplitude = 2.2f;

    [Header("Mouse Parallax (Optional)")]
    [Tooltip("Si está activo, responde al ratón como si fueran capas. Recomendado para menús.")]
    [SerializeField] private bool enableMouseParallax = true;

    [Tooltip("Cuánto influye el ratón (en píxeles UI aprox).")]
    [SerializeField] private float mouseStrength = 10f;

    [Tooltip("Multiplicador de profundidad: sombras suele ser mayor que prota.")]
    [SerializeField] private float protaMouseMultiplier = 0.6f;
    [SerializeField] private float sombrasMouseMultiplier = 1.0f;

    [Header("Seeds (Randomize look)")]
    [SerializeField] private int seed = 12345;

    private Vector2 _protaBasePos;
    private float _protaBaseRotZ;

    private Vector2 _sombrasBasePos;
    private float _sombrasBaseRotZ;

    private Vector2 _protaVel;
    private float _protaRotVel;

    private Vector2 _sombrasVel;
    private float _sombrasRotVel;

    private void Awake()
    {
        CacheBaseTransforms();
        if (seed == 0) seed = Random.Range(1, int.MaxValue);
    }

    private void OnEnable()
    {
        CacheBaseTransforms();
    }

    private void CacheBaseTransforms()
    {
        if (prota != null)
        {
            _protaBasePos = prota.anchoredPosition;
            _protaBaseRotZ = prota.localEulerAngles.z;
        }

        if (sombras != null)
        {
            _sombrasBasePos = sombras.anchoredPosition;
            _sombrasBaseRotZ = sombras.localEulerAngles.z;
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime * Mathf.Max(0.0001f, speed);

        // Mouse parallax target (centered -1..1)
        Vector2 mouse01 = Vector2.zero;
        if (enableMouseParallax)
        {
            mouse01 = GetMouseNormalizedCentered();
        }

        // --- PROTA ---
        if (prota != null)
        {
            Vector2 organicPos = new Vector2(
                PerlinSigned(seed + 10, t * 0.70f),
                PerlinSigned(seed + 20, t * 0.55f)
            );

            float organicRot = PerlinSigned(seed + 30, t * 0.45f);

            Vector2 targetOffsetPos =
                new Vector2(organicPos.x * protaPositionAmplitude.x, organicPos.y * protaPositionAmplitude.y);

            float targetOffsetRot = organicRot * protaRotationAmplitude;

            if (enableMouseParallax)
            {
                Vector2 mouseOffset = mouse01 * mouseStrength * protaMouseMultiplier;
                targetOffsetPos += mouseOffset;
                targetOffsetRot += (-mouse01.x) * (protaRotationAmplitude * 0.35f); // leve reacción al eje X
            }

            ApplySmooth(
                prota,
                _protaBasePos + targetOffsetPos,
                _protaBaseRotZ + targetOffsetRot,
                ref _protaVel,
                ref _protaRotVel
            );
        }

        // --- SOMBRAS ---
        if (sombras != null)
        {
            Vector2 organicPos = new Vector2(
                PerlinSigned(seed + 110, t * 0.62f),
                PerlinSigned(seed + 120, t * 0.50f)
            );

            float organicRot = PerlinSigned(seed + 130, t * 0.40f);

            Vector2 targetOffsetPos =
                new Vector2(organicPos.x * sombrasPositionAmplitude.x, organicPos.y * sombrasPositionAmplitude.y);

            float targetOffsetRot = organicRot * sombrasRotationAmplitude;

            if (enableMouseParallax)
            {
                Vector2 mouseOffset = mouse01 * mouseStrength * sombrasMouseMultiplier;
                targetOffsetPos += mouseOffset;
                targetOffsetRot += (-mouse01.x) * (sombrasRotationAmplitude * 0.45f);
            }

            ApplySmooth(
                sombras,
                _sombrasBasePos + targetOffsetPos,
                _sombrasBaseRotZ + targetOffsetRot,
                ref _sombrasVel,
                ref _sombrasRotVel
            );
        }
    }

    private void ApplySmooth(RectTransform rt, Vector2 targetPos, float targetRotZ,
        ref Vector2 posVel, ref float rotVel)
    {
        if (damping <= 0f)
        {
            rt.anchoredPosition = targetPos;
            rt.localEulerAngles = new Vector3(0f, 0f, targetRotZ);
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float smoothTime = 1f / damping;

        Vector2 newPos = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref posVel, smoothTime, Mathf.Infinity, dt);

        float currentZ = rt.localEulerAngles.z;
        // Evitar saltos 0/360
        float newZ = Mathf.SmoothDampAngle(currentZ, targetRotZ, ref rotVel, smoothTime, Mathf.Infinity, dt);

        rt.anchoredPosition = newPos;
        rt.localEulerAngles = new Vector3(0f, 0f, newZ);
    }

    private Vector2 GetMouseNormalizedCentered()
    {
        // Devuelve un vector centrado [-1..1] aprox, basado en la posición del ratón dentro de la pantalla.
        Vector2 mp = Input.mousePosition;

        float nx = (Screen.width > 0) ? (mp.x / Screen.width) : 0.5f;
        float ny = (Screen.height > 0) ? (mp.y / Screen.height) : 0.5f;

        // 0..1 -> -1..1
        return new Vector2((nx - 0.5f) * 2f, (ny - 0.5f) * 2f);
    }

    private float PerlinSigned(int s, float t)
    {
        // Mathf.PerlinNoise = 0..1 -> convertimos a -1..1
        float v = Mathf.PerlinNoise(s * 0.001f, t);
        return (v - 0.5f) * 2f;
    }

#if UNITY_EDITOR
    [ContextMenu("Re-capture Base Transforms")]
    private void EditorRecache()
    {
        CacheBaseTransforms();
    }
#endif
}

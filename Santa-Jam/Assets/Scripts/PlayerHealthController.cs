using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealthController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Tooltip("Si ya tienes HealthRuntime en el player o hijos, este controlador lo usará automáticamente.")]
    [SerializeField] private HealthRuntime healthRuntime;

    [Header("Armour (optional)")]
    [Tooltip("Si existe ArmourRuntime en el player o hijos, el daño se reducirá usando la armadura.")]
    [SerializeField] private ArmourRuntime armourRuntime;

    [Header("Regen")]
    [SerializeField] private float secondsWithoutDamageToRegen = 8f;
    [SerializeField] private float regenPerSecond = 12f;

    [Header("Post Processing (URP)")]
    [SerializeField] private Volume globalVolume;

    [Tooltip("Saturation cuando la vida está en 0 (gris total = -100).")]
    [Range(-100f, 0f)]
    [SerializeField] private float saturationAtZeroHealth = -100f;

    [Tooltip("Vignette smoothness cuando la vida está en 0.")]
    [Range(0f, 1f)]
    [SerializeField] private float vignetteSmoothnessAtZeroHealth = 1f;

    [Tooltip("Suavizado con el que el postproceso persigue el objetivo (visual).")]
    [SerializeField] private float postLerpSpeed = 8f;

    [Header("Death - Disable Components")]
    [Tooltip("Componentes a desactivar al morir. Si lo dejas vacío, intentará auto-detectar los principales.")]
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    public int CurrentHealth => healthRuntime != null ? healthRuntime.CurrentHealth : _currentHealthFallback;
    public int MaxHealth => healthRuntime != null ? healthRuntime.MaxHealth : maxHealth;
    public bool IsDead => _isDead;

    private int _currentHealthFallback;
    private bool _isDead;

    private float _lastDamageTime;

    // URP overrides
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    // Base values (to restore)
    private float _baseSaturation;
    private float _baseVignetteSmoothness;

    // Current post values (for smoothing)
    private float _currentSaturation;
    private float _currentVignetteSmoothness;

    private void Awake()
    {
        // Resolve runtimes from player hierarchy
        healthRuntime = GetComponentInChildren<HealthRuntime>();

        armourRuntime = GetComponentInChildren<ArmourRuntime>();

        // Init health if no runtime exists
        if (healthRuntime == null)
        {
            _currentHealthFallback = maxHealth;
        }
        else
        {
            // Aseguramos que arranque al máximo si por algún motivo viene sin inicializar
            if (healthRuntime.CurrentHealth <= 0)
            {
                healthRuntime.SetHealth(healthRuntime.MaxHealth);
            }
        }

        SetupPostProcessing();
        CacheDeathDisablersIfEmpty();

        _lastDamageTime = Time.time;
        ApplyPostInstantFromHealth();
    }

    private void Update()
    {
        if (_isDead)
            return;

        HandleRegen();
        SmoothPostTowardsTarget();
    }

    #region Public API

    public void TakeDamage(int damage)
    {
        if (_isDead)
            return;

        damage = Mathf.Max(0, damage);
        if (damage == 0)
            return;

        _lastDamageTime = Time.time;

        // Armour reduces damage (if present)
        if (armourRuntime != null)
        {
            damage = armourRuntime.ApplyArmourToDamage(damage);
            if (damage <= 0)
            {
                // Aunque no haya daño final, refrescamos el timer de regen
                return;
            }
        }

        if (healthRuntime != null)
        {
            healthRuntime.TakeDamage(damage);
        }
        else
        {
            _currentHealthFallback = Mathf.Max(0, _currentHealthFallback - damage);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (_isDead)
            return;

        amount = Mathf.Max(0, amount);
        if (amount == 0)
            return;

        if (healthRuntime != null)
        {
            healthRuntime.Heal(amount);
        }
        else
        {
            _currentHealthFallback = Mathf.Clamp(_currentHealthFallback + amount, 0, MaxHealth);
        }
    }

    public void SetHealth(int value)
    {
        if (_isDead)
            return;

        if (healthRuntime != null)
        {
            healthRuntime.SetHealth(value);
        }
        else
        {
            _currentHealthFallback = Mathf.Clamp(value, 0, MaxHealth);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    #endregion

    #region Regen

    private void HandleRegen()
    {
        if (CurrentHealth <= 0)
            return;

        if (CurrentHealth >= MaxHealth)
            return;

        float timeSinceDamage = Time.time - _lastDamageTime;
        if (timeSinceDamage < secondsWithoutDamageToRegen)
            return;

        float healFloat = regenPerSecond * Time.deltaTime;
        int healInt = Mathf.FloorToInt(healFloat);

        // Para no perder curación por los decimales, curamos float acumulando.
        // Solución simple: curar al menos 1 cuando toque (si regenPerSecond es bajo).
        if (healInt <= 0)
        {
            healInt = 1;
        }

        Heal(healInt);
    }

    #endregion

    #region Post Processing

    private void SetupPostProcessing()
    {
        globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume == null || globalVolume.profile == null)
            return;

        globalVolume.profile.TryGet(out _vignette);
        globalVolume.profile.TryGet(out _colorAdjustments);

        _baseSaturation = _colorAdjustments.saturation.value;

        _baseVignetteSmoothness = _vignette.smoothness.value;

        _currentSaturation = _baseSaturation;
        _currentVignetteSmoothness = _baseVignetteSmoothness;
    }

    private void ApplyPostInstantFromHealth()
    {
        if (_colorAdjustments == null && _vignette == null)
            return;

        float t = GetHealth01();

        float targetSat = Mathf.Lerp(saturationAtZeroHealth, _baseSaturation, t);
        float targetSmooth = Mathf.Lerp(vignetteSmoothnessAtZeroHealth, _baseVignetteSmoothness, t);

        _currentSaturation = targetSat;
        _currentVignetteSmoothness = targetSmooth;

        _colorAdjustments.saturation.Override(_currentSaturation);

        _vignette.smoothness.Override(_currentVignetteSmoothness);
    }

    private void SmoothPostTowardsTarget()
    {
        if (_colorAdjustments == null && _vignette == null)
            return;

        float t = GetHealth01();

        float targetSat = Mathf.Lerp(saturationAtZeroHealth, _baseSaturation, t);
        float targetSmooth = Mathf.Lerp(vignetteSmoothnessAtZeroHealth, _baseVignetteSmoothness, t);

        float k = 1f - Mathf.Exp(-postLerpSpeed * Time.deltaTime);

        _currentSaturation = Mathf.Lerp(_currentSaturation, targetSat, k);
        _currentVignetteSmoothness = Mathf.Lerp(_currentVignetteSmoothness, targetSmooth, k);

        _colorAdjustments.saturation.Override(_currentSaturation);

        _vignette.smoothness.Override(_currentVignetteSmoothness);
    }

    private float GetHealth01()
    {
        int max = Mathf.Max(1, MaxHealth);
        return Mathf.Clamp01((float)CurrentHealth / max);
    }

    #endregion

    #region Death

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        Debug.Log("PLAYER DEAD");

        // Forzamos post a estado de 0 vida (por claridad visual)
        ApplyPostInstantFromHealth();

        // Disable movement / shooting / etc.
        if (disableOnDeath != null)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                if (disableOnDeath[i] != null)
                    disableOnDeath[i].enabled = false;
            }
        }
    }

    private void CacheDeathDisablersIfEmpty()
    {
        if (disableOnDeath != null && disableOnDeath.Length > 0)
            return;

        var list = new List<MonoBehaviour>();

        var movement = GetComponent<PlayerMovementController>();
        list.Add(movement);

        var aimShoot = GetComponent<PlayerAimAndShootController>();
        list.Add(aimShoot);

        var sprintFx = GetComponent<PlayerCameraSprintEffects>();
        list.Add(sprintFx);

        // Si quieres bloquear inputs también (opcional)
        var input = GetComponent<InputManager>();
        list.Add(input);

        disableOnDeath = list.ToArray();
    }

    #endregion
}

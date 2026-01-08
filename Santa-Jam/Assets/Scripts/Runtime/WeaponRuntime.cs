using System.Collections;
using UnityEngine;

public class WeaponRuntime : MonoBehaviour
{
    [Header("Weapon")]
    public WeaponDa weaponData;

    [Header("Ammo Runtime")]
    [Tooltip("Balas actuales dentro del cargador.")]
    public int currentMagazine;

    [Tooltip("Balas de reserva disponibles (pool). Se usan para recargar el cargador.")]
    public int currentReserveAmmo;

    [Tooltip("True mientras se está recargando.")]
    public bool reloading;

    // Fire-rate control
    private float nextFireTime;

    // Reload tracking
    private Coroutine reloadCo;
    private float reloadStartTime;
    private float reloadDuration;

    // Retrocompatibilidad (GameManager / SaveData)
    public int currentReserve
    {
        get => currentReserveAmmo;
        set => currentReserveAmmo = Mathf.Max(0, value);
    }

    public void SetAmmo(int magazineBullets, int reserveAmmo)
    {
        currentMagazine = Mathf.Max(0, magazineBullets);
        currentReserveAmmo = Mathf.Max(0, reserveAmmo);
    }

    public float GetReloadProgress01()
    {
        if (!reloading) return 0f;
        if (reloadDuration <= 0f) return 1f;
        return Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration);
    }

    private void Awake()
    {
        Equip(weaponData);
    }

    public void Equip(WeaponDa data)
    {
        // Si cambias de arma durante una recarga, corta la recarga.
        CancelReload();

        weaponData = data;

        if (weaponData == null)
            return;

        currentMagazine = Mathf.Max(0, weaponData.BulletsPerMagazine);
        currentReserveAmmo = Mathf.Max(0, weaponData.MaxReserveAmmo);

        reloading = false;
        nextFireTime = 0f;
    }

    public bool CanShoot()
    {
        if (weaponData == null) return false;
        if (reloading) return false;
        if (currentMagazine <= 0) return false;
        if (Time.time < nextFireTime) return false;
        return true;
    }

    public bool TryShoot()
    {
        if (!CanShoot())
            return false;

        currentMagazine--;

        float fireRate = Mathf.Max(0.1f, weaponData.FireRate);
        nextFireTime = Time.time + (1f / fireRate);

        return true;
    }

    public bool CanReload()
    {
        if (weaponData == null) return false;
        if (reloading) return false;
        if (currentReserveAmmo <= 0) return false;

        int magSize = Mathf.Max(1, weaponData.BulletsPerMagazine);
        if (currentMagazine >= magSize) return false;

        return true;
    }

    public bool TryStartReload()
    {
        if (!CanReload())
            return false;

        CancelReload(); // por seguridad
        reloadCo = StartCoroutine(ReloadRoutine());
        return true;
    }

    public void CancelReload()
    {
        if (reloadCo != null)
        {
            StopCoroutine(reloadCo);
            reloadCo = null;
        }
        reloading = false;
        reloadStartTime = 0f;
        reloadDuration = 0f;
    }

    private IEnumerator ReloadRoutine()
    {
        reloading = true;

        reloadStartTime = Time.time;
        reloadDuration = Mathf.Max(0f, weaponData.ReloadTime);

        if (reloadDuration > 0f)
            yield return new WaitForSeconds(reloadDuration);

        int magSize = Mathf.Max(1, weaponData.BulletsPerMagazine);
        int missing = Mathf.Clamp(magSize - currentMagazine, 0, magSize);

        int toLoad = Mathf.Min(missing, currentReserveAmmo);

        currentMagazine += toLoad;
        currentReserveAmmo -= toLoad;

        reloading = false;
        reloadCo = null;
    }
}

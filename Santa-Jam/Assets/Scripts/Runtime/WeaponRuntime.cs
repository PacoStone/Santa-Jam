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

    private float nextFireTime;

    // --------------------------------------------------------------------
    // Compatibilidad con tu código existente (GameManager / SaveData)
    // --------------------------------------------------------------------

    /// <summary>
    /// Alias retrocompatible para tu GameManager (Data.currentReserve = weapon.currentReserve).
    /// </summary>
    public int currentReserve
    {
        get => currentReserveAmmo;
        set => currentReserveAmmo = Mathf.Max(0, value);
    }

    /// <summary>
    /// Retrocompatible: tu GameManager llama weapon.SetAmmo(mag, reserve).
    /// </summary>
    public void SetAmmo(int magazineBullets, int reserveAmmo)
    {
        currentMagazine = Mathf.Max(0, magazineBullets);
        currentReserveAmmo = Mathf.Max(0, reserveAmmo);
    }

    // --------------------------------------------------------------------

    private void Awake()
    {
        Equip(weaponData);
    }

    public void Equip(WeaponDa data)
    {
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

    /// <summary>
    /// Consume 1 bala del cargador y aplica fire rate.
    /// </summary>
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

        StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        reloading = true;

        float t = Mathf.Max(0f, weaponData.ReloadTime);
        if (t > 0f)
        {
            yield return new WaitForSeconds(t);
        }    

        int magSize = Mathf.Max(1, weaponData.BulletsPerMagazine);
        int missing = Mathf.Clamp(magSize - currentMagazine, 0, magSize);

        int toLoad = Mathf.Min(missing, currentReserveAmmo);

        currentMagazine += toLoad;
        currentReserveAmmo -= toLoad;

        reloading = false;
    }
}

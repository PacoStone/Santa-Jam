using UnityEngine;

public class WeaponRuntime : MonoBehaviour
{
    public WeaponDa weaponData;
    public int currentMagazine;
    public int currentReserve;
    public bool reloading;

    private void Awake()
    {
        Equip(weaponData);
    }

    public void Equip(WeaponDa data)
    {
        weaponData = data;
        if (data == null) return;
        currentMagazine = data.BulletsPerMagazine;
        currentReserve = data.MaxReserveAmmo;
        reloading = false;
    }
}

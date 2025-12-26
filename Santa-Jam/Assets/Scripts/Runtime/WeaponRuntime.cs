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

    public void SetAmmo(int magazine, int reserve)
    {
        currentMagazine = Mathf.Max(0, magazine);
        currentReserve = Mathf.Max(0, reserve);
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

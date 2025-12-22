using UnityEngine;

public class WeaponPickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private WeaponDa weaponToGive;

    protected override bool TryApply(GameObject other)
    {
        WeaponRuntime weapon = other.GetComponentInChildren<WeaponRuntime>();

        if (weapon == null || weaponToGive == null)
            return false;

        weapon.Equip(weaponToGive);
        Debug.Log($"Pickup: arma equipada -> {weaponToGive.ItemName}");
        return true;
    }
}

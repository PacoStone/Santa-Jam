using UnityEngine;

public class WeaponPickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private WeaponDa weaponToGive;

    protected override bool TryApply(GameObject other)
    {
        if (weaponToGive == null)
            return false;

        // Busca inventario en jugador (o parent)
        WeaponInventoryController inv = other.GetComponentInChildren<WeaponInventoryController>();
        if (inv == null) inv = other.GetComponentInParent<WeaponInventoryController>();

        if (inv == null)
        {
            // fallback: comportamiento antiguo
            WeaponRuntime weapon = other.GetComponentInChildren<WeaponRuntime>();
            if (weapon == null) return false;

            weapon.Equip(weaponToGive);
            Debug.Log($"Pickup: arma equipada -> {weaponToGive.ItemName}");
            return true;
        }

        inv.AddOrReplace(weaponToGive);
        Debug.Log($"Pickup: arma añadida al inventario -> {weaponToGive.ItemName}");
        return true;
    }
}

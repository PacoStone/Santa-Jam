using UnityEngine;

public class WeaponPickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private WeaponDa weaponToGive;

    [Header("HUD Reemplazo (CambiaArmas)")]
    [SerializeField] private CambiaArmasHUDController cambiaArmasHUD;

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

        // Si hay hueco o ya la tienes: añade/equipa normal
        if (inv.TryAddOrEquipWithoutReplacing(weaponToGive))
        {
            Debug.Log($"Pickup: arma añadida/equipada -> {weaponToGive.ItemName}");
            return true;
        }

        // Si está lleno (3 armas): abre HUD de reemplazo
        if (cambiaArmasHUD == null)
            cambiaArmasHUD = FindFirstObjectByType<CambiaArmasHUDController>();

        if (cambiaArmasHUD == null)
        {
            Debug.LogWarning("No encuentro CambiaArmasHUDController en escena. Fallback a AddOrReplace.");
            inv.AddOrReplace(weaponToGive);
            return true;
        }

        cambiaArmasHUD.Open(inv, weaponToGive);
        Debug.Log($"Pickup: inventario lleno, abriendo CambiaArmas -> {weaponToGive.ItemName}");
        return true;
    }
}

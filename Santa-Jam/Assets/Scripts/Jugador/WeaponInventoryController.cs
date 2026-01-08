using System;
using UnityEngine;

public class WeaponInventoryController : MonoBehaviour
{
    [Serializable]
    public struct SlotState
    {
        public WeaponDa weapon;
        public int mag;
        public int reserve;
    }

    [Header("Refs")]
    [SerializeField] private WeaponRuntime weaponRuntime;

    [Header("Loadout (3 slots)")]
    [SerializeField] private SlotState[] slots = new SlotState[3];

    [Header("Runtime")]
    [SerializeField] private int currentIndex = -1; // -1 = nada equipado

    public int CurrentIndex => currentIndex;

    public event Action<int> OnWeaponChanged; // newIndex (puede ser -1)

    private void Awake()
    {
        if (slots == null || slots.Length != 3)
            slots = new SlotState[3];

        // Si quieres empezar con armas, detecta el primer slot ocupado
        currentIndex = FindFirstOccupiedSlot();
        EquipCurrentSlot(initial: true);

        OnWeaponChanged?.Invoke(currentIndex);
    }

    public int WeaponCount()
    {
        int c = 0;
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon != null) c++;
        return c;
    }

    public bool HasAtLeastTwoWeapons() => WeaponCount() >= 2;

    public WeaponDa GetWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2) return null;
        return slots[slotIndex].weapon;
    }

    public SlotState GetSlotState(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2) return default;
        return slots[slotIndex];
    }

    /// <summary>Progreso de recarga solo del arma activa (si slotIndex == currentIndex).</summary>
    public float GetReloadProgress01(int slotIndex)
    {
        if (weaponRuntime == null) return 0f;
        if (slotIndex != currentIndex) return 0f;
        return weaponRuntime.GetReloadProgress01();
    }

    public void ChangeLeft()
    {
        if (WeaponCount() < 2) return;
        SetIndex(FindNextOccupiedSlot(currentIndex, -1));
    }

    public void ChangeRight()
    {
        if (WeaponCount() < 2) return;
        SetIndex(FindNextOccupiedSlot(currentIndex, +1));
    }

    public void SetIndex(int newIndex)
    {
        if (newIndex < -1 || newIndex > 2) return;
        if (newIndex == currentIndex) return;

        SaveCurrentSlotState();

        currentIndex = newIndex;
        EquipCurrentSlot(initial: false);

        OnWeaponChanged?.Invoke(currentIndex);
    }

    /// <summary>
    /// Para pickups: mete el arma en el primer slot libre; si no hay, reemplaza el slot actual.
    /// Si era la primera arma, se equipa automáticamente.
    /// </summary>
    public void AddOrReplace(WeaponDa weaponData)
    {
        if (weaponData == null) return;

        // ¿ya existe?
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == weaponData)
            {
                SetIndex(i);
                return;
            }
        }

        bool wasEmpty = WeaponCount() == 0;

        int free = FindFirstEmptySlot();
        int target = free != -1 ? free : (currentIndex != -1 ? currentIndex : 0);

        if (target == currentIndex)
            SaveCurrentSlotState();

        slots[target].weapon = weaponData;
        slots[target].mag = Mathf.Max(0, weaponData.BulletsPerMagazine);
        slots[target].reserve = Mathf.Max(0, weaponData.MaxReserveAmmo);

        // Si era la primera arma, equiparla sí o sí
        if (wasEmpty)
            currentIndex = target;

        SetIndex(target);
    }

    private void EquipCurrentSlot(bool initial)
    {
        if (weaponRuntime == null) return;

        // Si no hay arma equipada, opcionalmente desactiva visual del arma aquí
        if (currentIndex == -1)
        {
            weaponRuntime.CancelReload();
            weaponRuntime.weaponData = null;
            weaponRuntime.currentMagazine = 0;
            weaponRuntime.currentReserveAmmo = 0;
            weaponRuntime.reloading = false;
            return;
        }

        var slot = slots[currentIndex];
        if (slot.weapon == null)
        {
            // Inconsistencia: índice apunta a vacío, corrige
            currentIndex = FindFirstOccupiedSlot();
            if (currentIndex == -1)
            {
                EquipCurrentSlot(initial);
                return;
            }
            slot = slots[currentIndex];
        }

        weaponRuntime.Equip(slot.weapon);
        weaponRuntime.SetAmmo(slot.mag, slot.reserve);

        if (initial)
            weaponRuntime.CancelReload();
    }

    private void SaveCurrentSlotState()
    {
        if (weaponRuntime == null) return;
        if (currentIndex < 0 || currentIndex > 2) return;

        var slot = slots[currentIndex];
        if (slot.weapon == null) return;

        weaponRuntime.CancelReload();

        slot.mag = weaponRuntime.currentMagazine;
        slot.reserve = weaponRuntime.currentReserveAmmo;
        slots[currentIndex] = slot;
    }

    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon == null) return i;
        return -1;
    }

    private int FindFirstOccupiedSlot()
    {
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon != null) return i;
        return -1;
    }

    private int FindNextOccupiedSlot(int fromIndex, int dir)
    {
        // dir: -1 o +1
        int start = fromIndex;
        if (start < 0 || start > 2) start = 0;

        for (int step = 1; step <= 3; step++)
        {
            int i = Mod(start + step * dir, 3);
            if (slots[i].weapon != null)
                return i;
        }
        return fromIndex; // fallback
    }

    private int Mod(int a, int m) => (a % m + m) % m;
}

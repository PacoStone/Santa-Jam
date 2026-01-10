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

    [Header("Slots (4)")]
    [SerializeField] private SlotState[] slots = new SlotState[4];

    [Header("Runtime")]
    [SerializeField] private int currentIndex = -1; // -1 = sin arma

    // EXISTENTE (lo mantengo)
    public event Action<int> OnWeaponChanged; // newIndex (puede ser -1)

    // NUEVO (para animator/visual, UI, etc.)
    public event Action<WeaponDa, int> OnWeaponDataChanged; // weapon, index

    public int CurrentIndex => currentIndex;

    public WeaponDa CurrentWeaponData
    {
        get
        {
            if (currentIndex < 0 || currentIndex >= slots.Length) return null;
            return slots[currentIndex].weapon;
        }
    }

    private void Awake()
    {
        if (slots == null || slots.Length != 3)
            slots = new SlotState[3];
    }

    private void Start()
    {
        EquipCurrentSlot(initial: true);

        // Emite también el evento nuevo al iniciar, por si ya empiezas armado
        OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
    }

    public int WeaponCount()
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon != null) count++;
        return count;
    }

    public WeaponDa GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2) return null;
        return slots[slotIndex].weapon;
    }

    public float GetReloadProgress01(int slotIndex)
    {
        if (weaponRuntime == null) return 0f;
        if (slotIndex != currentIndex) return 0f;
        return weaponRuntime.GetReloadProgress01();
    }
    public bool AddOrReplace(WeaponDa weapon)
    {
        if (weapon == null) return false;

        // 1) Si ya existe en un slot, simplemente equipa ese slot
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == weapon)
            {
                SetIndex(i);
                return true;
            }
        }

        // 2) Busca primer slot vacío
        int empty = -1;
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == null)
            {
                empty = i;
                break;
            }
        }

        // Defaults de munición desde WeaponDa (tu WeaponRuntime usa estos mismos campos en Equip)
        int mag = Mathf.Max(0, weapon.BulletsPerMagazine);
        int reserve = Mathf.Max(0, weapon.MaxReserveAmmo);

        // 3) Si hay hueco: añade en el hueco y equipa
        if (empty != -1)
        {
            slots[empty] = new SlotState
            {
                weapon = weapon,
                mag = mag,
                reserve = reserve
            };

            SetIndex(empty);
            return true;
        }

        // 4) Si no hay hueco: reemplaza el slot actual (o el 0 si no hay arma equipada)
        int replaceIndex = (currentIndex >= 0 && currentIndex < 3) ? currentIndex : 0;

        // Guarda estado del arma actual antes de sobreescribir
        SaveCurrentSlotState();

        slots[replaceIndex] = new SlotState
        {
            weapon = weapon,
            mag = mag,
            reserve = reserve
        };

        // Fuerza equipar aunque sea el mismo índice
        currentIndex = replaceIndex;
        EquipCurrentSlot(initial: false);

        // Eventos (para HUD/Animator, etc.)
        OnWeaponChanged?.Invoke(currentIndex);
        OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);

        return true;
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

        // EVENTOS
        OnWeaponChanged?.Invoke(currentIndex);
        OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
    }

    private void SaveCurrentSlotState()
    {
        if (weaponRuntime == null) return;
        if (currentIndex < 0 || currentIndex > 2) return;
        if (slots[currentIndex].weapon == null) return;

        slots[currentIndex] = new SlotState
        {
            weapon = slots[currentIndex].weapon,
            mag = weaponRuntime.currentMagazine,
            reserve = weaponRuntime.currentReserveAmmo
        };
    }

    private void EquipCurrentSlot(bool initial)
    {
        if (weaponRuntime == null) return;

        // Si no hay arma equipada
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
                // No hay armas realmente
                EquipCurrentSlot(initial);
                return;
            }
            slot = slots[currentIndex];
        }

        weaponRuntime.Equip(slot.weapon);
        weaponRuntime.SetAmmo(slot.mag, slot.reserve);
    }

    private int FindFirstOccupiedSlot()
    {
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon != null) return i;
        return -1;
    }

    private int FindNextOccupiedSlot(int fromIndex, int dir)
    {
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

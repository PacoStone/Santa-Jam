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

    [Header("Slots (3 equipadas)")]
    [SerializeField] private SlotState[] slots = new SlotState[3];

    [Header("Start")]
    [SerializeField] private int startIndex = 0;

    public event Action<int> OnWeaponChanged;
    public event Action<WeaponDa, int> OnWeaponDataChanged;

    private int currentIndex = 0;

    public int CurrentIndex => currentIndex;
    public WeaponDa CurrentWeaponData => (currentIndex >= 0 && currentIndex < 3) ? slots[currentIndex].weapon : null;

    private void Awake()
    {
        EnsureSlotsSize();
        currentIndex = Mathf.Clamp(startIndex, 0, 2);
    }

    private void OnValidate()
    {
        EnsureSlotsSize();
        startIndex = Mathf.Clamp(startIndex, 0, 2);
    }

    private void EnsureSlotsSize()
    {
        if (slots == null || slots.Length != 3)
            slots = new SlotState[3];
    }

    private void Start()
    {
        EquipCurrentSlot(initial: true);
        OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
    }

    public int WeaponCount()
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
            if (slots[i].weapon != null) count++;
        return count;
    }

    public bool HasAtLeastTwoWeapons()
    {
        return WeaponCount() >= 2;
    }

    public WeaponDa GetWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return null;
        return slots[slotIndex].weapon;
    }

    public float GetReloadProgress01(int slotIndex)
    {
        if (weaponRuntime == null) return 0f;
        if (slotIndex != currentIndex) return 0f;
        return weaponRuntime.GetReloadProgress01();
    }

    /// <summary>
    /// Intenta:
    /// 1) Equipar si ya existe
    /// 2) Añadir si hay hueco
    /// Si NO hay hueco (3 llenas) devuelve false (para abrir HUD de reemplazo).
    /// </summary>
    public bool TryAddOrEquipWithoutReplacing(WeaponDa weapon)
    {
        if (weapon == null) return false;

        // Ya existe: equipar
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == weapon)
            {
                SetIndex(i);
                return true;
            }
        }

        // Hueco libre
        int empty = -1;
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == null)
            {
                empty = i;
                break;
            }
        }

        if (empty == -1)
            return false; // Lleno: que el HUD decida qué slot reemplazar

        int mag = Mathf.Max(0, weapon.BulletsPerMagazine);
        int reserve = Mathf.Max(0, weapon.MaxReserveAmmo);

        slots[empty] = new SlotState
        {
            weapon = weapon,
            mag = mag,
            reserve = reserve
        };

        SetIndex(empty);
        return true;
    }

    /// <summary>
    /// Reemplaza un slot específico por el arma nueva, y opcionalmente la equipa.
    /// </summary>
    public bool ReplaceSlot(int slotIndex, WeaponDa newWeapon, bool equipAfter = true)
    {
        if (newWeapon == null) return false;
        if (slotIndex < 0 || slotIndex >= 3) return false;

        // Guarda estado del arma actual antes de sobreescribir
        SaveCurrentSlotState();

        int mag = Mathf.Max(0, newWeapon.BulletsPerMagazine);
        int reserve = Mathf.Max(0, newWeapon.MaxReserveAmmo);

        slots[slotIndex] = new SlotState
        {
            weapon = newWeapon,
            mag = mag,
            reserve = reserve
        };

        if (equipAfter)
        {
            currentIndex = slotIndex;
            EquipCurrentSlot(initial: false);
            OnWeaponChanged?.Invoke(currentIndex);
            OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
        }
        else
        {
            OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
        }

        return true;
    }

    /// <summary>
    /// Método antiguo. Lo dejo por compatibilidad, pero ahora:
    /// - Si está lleno, reemplaza el slot actual.
    /// Si prefieres obligar siempre al HUD, no lo uses.
    /// </summary>
    public bool AddOrReplace(WeaponDa weapon)
    {
        if (weapon == null) return false;

        // 1) Si ya existe: equipa
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == weapon)
            {
                SetIndex(i);
                return true;
            }
        }

        // 2) Hueco
        int empty = -1;
        for (int i = 0; i < 3; i++)
        {
            if (slots[i].weapon == null) { empty = i; break; }
        }

        int mag = Mathf.Max(0, weapon.BulletsPerMagazine);
        int reserve = Mathf.Max(0, weapon.MaxReserveAmmo);

        if (empty != -1)
        {
            slots[empty] = new SlotState { weapon = weapon, mag = mag, reserve = reserve };
            SetIndex(empty);
            return true;
        }

        // 3) Lleno: reemplaza el actual
        int replaceIndex = (currentIndex >= 0 && currentIndex < 3) ? currentIndex : 0;
        SaveCurrentSlotState();

        slots[replaceIndex] = new SlotState { weapon = weapon, mag = mag, reserve = reserve };
        currentIndex = replaceIndex;
        EquipCurrentSlot(initial: false);

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
        if (newIndex < 0 || newIndex >= 3) return;
        if (newIndex == currentIndex) return;
        if (slots[newIndex].weapon == null) return;

        SaveCurrentSlotState();
        currentIndex = newIndex;
        EquipCurrentSlot(initial: false);

        OnWeaponChanged?.Invoke(currentIndex);
        OnWeaponDataChanged?.Invoke(CurrentWeaponData, currentIndex);
    }

    private int FindNextOccupiedSlot(int fromIndex, int dir)
    {
        if (dir == 0) return fromIndex;

        int idx = fromIndex;
        for (int step = 0; step < 3; step++)
        {
            idx = (idx + dir + 3) % 3;
            if (slots[idx].weapon != null) return idx;
        }
        return fromIndex;
    }

    private void SaveCurrentSlotState()
    {
        if (weaponRuntime == null) return;
        if (currentIndex < 0 || currentIndex >= 3) return;
        if (slots[currentIndex].weapon == null) return;

        slots[currentIndex].mag = weaponRuntime.CurrentMagazine;
        slots[currentIndex].reserve = weaponRuntime.CurrentReserve;
    }

    private void EquipCurrentSlot(bool initial)
    {
        if (weaponRuntime == null) return;
        if (currentIndex < 0 || currentIndex >= 3) return;

        var s = slots[currentIndex];
        if (s.weapon == null) return;

        //weaponRuntime.Equip(s.weapon, s.mag, s.reserve, initial);
        weaponRuntime.Equip(s.weapon);
    }
}

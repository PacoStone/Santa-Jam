using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lookup (para rehidratar por ItemName)")]
    [SerializeField] private List<WeaponDa> allWeapons = new List<WeaponDa>();
    [SerializeField] private List<ArmourDa> allArmours = new List<ArmourDa>();
    [SerializeField] private List<EdibleDa> allEdibles = new List<EdibleDa>();
    [SerializeField] private List<HarvestDa> allHarvest = new List<HarvestDa>();

    [Header("Auto-apply")]
    [SerializeField] private bool autoApplyOnSceneLoad = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    [Serializable]
    public struct ItemStack
    {
        public string itemName;
        public int amount;
    }

    [Serializable]
    public class SessionData
    {
        [Header("Vitals")]
        public int currentHealth;

        [Header("Armour")]
        public string armourItemName;
        public int currentArmour;

        [Header("Weapons (loadout 3 slots)")]
        public string weaponSlot0ItemName;
        public string weaponSlot1ItemName;
        public string weaponSlot2ItemName;

        [Tooltip("Qué slot está equipado como arma principal (0..2)")]
        public int equippedSlotIndex;

        // Compat (antiguo). Lo mantenemos para no romper nada que lo lea.
        [Header("Weapon (legacy - compat)")]
        public string weaponItemName;
        public int currentMagazine;
        public int currentReserve;

        [Header("Inventory")]
        public List<ItemStack> inventory = new List<ItemStack>();

        public void Clear()
        {
            currentHealth = 0;

            armourItemName = string.Empty;
            currentArmour = 0;

            weaponSlot0ItemName = string.Empty;
            weaponSlot1ItemName = string.Empty;
            weaponSlot2ItemName = string.Empty;
            equippedSlotIndex = 0;

            weaponItemName = string.Empty;
            currentMagazine = 0;
            currentReserve = 0;

            inventory.Clear();
        }

        public string GetWeaponSlotName(int slot)
        {
            switch (slot)
            {
                case 0: return weaponSlot0ItemName;
                case 1: return weaponSlot1ItemName;
                case 2: return weaponSlot2ItemName;
                default: return string.Empty;
            }
        }

        public void SetWeaponSlotName(int slot, string itemName)
        {
            itemName ??= string.Empty;

            switch (slot)
            {
                case 0: weaponSlot0ItemName = itemName; break;
                case 1: weaponSlot1ItemName = itemName; break;
                case 2: weaponSlot2ItemName = itemName; break;
            }
        }
    }

    public SessionData Data { get; private set; } = new SessionData();

    private Dictionary<string, InGameItem> itemByName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildItemLookup();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void BuildItemLookup()
    {
        itemByName = new Dictionary<string, InGameItem>(StringComparer.Ordinal);

        void AddList<T>(List<T> list) where T : InGameItem
        {
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                T it = list[i];
                if (it == null) continue;
                if (string.IsNullOrWhiteSpace(it.ItemName)) continue;

                if (!itemByName.ContainsKey(it.ItemName))
                    itemByName.Add(it.ItemName, it);
            }
        }

        AddList(allWeapons);
        AddList(allArmours);
        AddList(allEdibles);
        AddList(allHarvest);

        if (verboseLogs)
            Debug.Log($"GameManager: Item lookup listo ({itemByName.Count} items).");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoApplyOnSceneLoad)
            return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            if (verboseLogs)
                Debug.Log("GameManager: No se encontró Player por tag al cargar escena (no se aplica sesión).");
            return;
        }

        ApplyToPlayer(player);
    }

    // =======================
    // CAPTURE / APPLY
    // =======================

    public void CaptureFromPlayer(GameObject player)
    {
        if (player == null)
            return;

        // Health
        HealthRuntime health = player.GetComponentInChildren<HealthRuntime>();
        if (health != null)
            Data.currentHealth = health.CurrentHealth;

        // Armour
        ArmourRuntime armour = player.GetComponentInChildren<ArmourRuntime>();
        if (armour != null)
        {
            Data.currentArmour = armour.CurrentArmour;
            Data.armourItemName = armour.CurrentArmourData != null ? armour.CurrentArmourData.ItemName : string.Empty;
        }

        // Weapons (NEW): desde WeaponInventoryController (3 slots)
        WeaponInventoryController invWeapons = player.GetComponentInChildren<WeaponInventoryController>();
        if (invWeapons != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var w = invWeapons.GetWeapon(i);
                Data.SetWeaponSlotName(i, w != null ? w.ItemName : string.Empty);
            }

            Data.equippedSlotIndex = Mathf.Clamp(invWeapons.CurrentIndex, 0, 2);

            // Compat legacy
            var equipped = invWeapons.GetWeapon(Data.equippedSlotIndex);
            Data.weaponItemName = equipped != null ? equipped.ItemName : string.Empty;
        }
        else
        {
            // Fallback legacy si no hay WeaponInventoryController
            WeaponRuntime weapon = player.GetComponentInChildren<WeaponRuntime>();
            if (weapon != null)
            {
                Data.weaponItemName = weapon.weaponData != null ? weapon.weaponData.ItemName : string.Empty;
                Data.currentMagazine = weapon.currentMagazine;
                Data.currentReserve = weapon.currentReserve;

                // Si vienes de un proyecto anterior: lo guardamos en slot0 para no perderlo
                Data.weaponSlot0ItemName = Data.weaponItemName;
                Data.weaponSlot1ItemName = string.Empty;
                Data.weaponSlot2ItemName = string.Empty;
                Data.equippedSlotIndex = 0;
            }
        }

        // Inventory
        InventoryRuntime inv = player.GetComponentInChildren<InventoryRuntime>();
        if (inv != null)
        {
            Data.inventory.Clear();
            foreach (var kvp in inv.GetSnapshot())
            {
                Data.inventory.Add(new ItemStack
                {
                    itemName = kvp.Key,
                    amount = kvp.Value
                });
            }
        }

        if (verboseLogs)
            Debug.Log("GameManager: Sesión CAPTURADA desde Player.");
    }

    public void ApplyToPlayer(GameObject player)
    {
        if (player == null)
            return;

        // Health
        HealthRuntime health = player.GetComponentInChildren<HealthRuntime>();
        if (health != null && Data.currentHealth > 0)
        {
            health.SetHealth(Data.currentHealth);
        }

        // Armour
        ArmourRuntime armour = player.GetComponentInChildren<ArmourRuntime>();
        if (armour != null)
        {
            ArmourDa armourDa = GetItem<ArmourDa>(Data.armourItemName);
            armour.SetArmour(armourDa);

            if (armourDa != null)
                armour.SetCurrentArmour(Data.currentArmour);
        }

        // Weapons (NEW): rehidratar 3 slots si existe WeaponInventoryController
        WeaponInventoryController invWeapons = player.GetComponentInChildren<WeaponInventoryController>();
        if (invWeapons != null)
        {
            for (int i = 0; i < 3; i++)
            {
                string n = Data.GetWeaponSlotName(i);
                WeaponDa da = GetItem<WeaponDa>(n);
                if (da != null)
                    invWeapons.ReplaceSlot(i, da, equipAfter: false);
            }

            // Equipar arma principal (slot persistido), si existe; si no, primera válida
            int equip = Mathf.Clamp(Data.equippedSlotIndex, 0, 2);
            if (invWeapons.GetWeapon(equip) != null)
            {
                invWeapons.SetIndex(equip);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (invWeapons.GetWeapon(i) != null)
                    {
                        invWeapons.SetIndex(i);
                        break;
                    }
                }
            }

            // Compat legacy
            var equipped = invWeapons.GetWeapon(invWeapons.CurrentIndex);
            Data.weaponItemName = equipped != null ? equipped.ItemName : string.Empty;
        }
        else
        {
            // Fallback legacy: 1 arma
            WeaponRuntime weapon = player.GetComponentInChildren<WeaponRuntime>();
            if (weapon != null)
            {
                WeaponDa weaponDa = GetItem<WeaponDa>(Data.weaponItemName);
                if (weaponDa != null)
                {
                    weapon.Equip(weaponDa);
                    weapon.SetAmmo(Data.currentMagazine, Data.currentReserve);
                }
            }
        }

        // Inventory
        InventoryRuntime inv = player.GetComponentInChildren<InventoryRuntime>();
        if (inv != null)
        {
            inv.ClearAll();

            for (int i = 0; i < Data.inventory.Count; i++)
            {
                var stack = Data.inventory[i];
                if (string.IsNullOrWhiteSpace(stack.itemName) || stack.amount <= 0)
                    continue;

                InGameItem item = GetItem<InGameItem>(stack.itemName);
                if (item != null)
                    inv.AddItem(item, stack.amount);
            }
        }

        if (verboseLogs)
            Debug.Log("GameManager: Sesión APLICADA al Jugador.");
    }

    public void ClearSession()
    {
        Data.Clear();

        if (verboseLogs)
            Debug.Log("GameManager: Sesión limpiada.");
    }

    // =======================
    // SHOP HELPERS
    // =======================

    public WeaponDa[] GetCurrentLoadoutWeaponDas()
    {
        var arr = new WeaponDa[3];
        for (int i = 0; i < 3; i++)
            arr[i] = GetItem<WeaponDa>(Data.GetWeaponSlotName(i));
        return arr;
    }

    /// <summary>
    /// Reemplaza un slot del loadout persistido (sin necesitar Player en la escena).
    /// Si makeEquipped=true, la nueva arma pasa a ser la principal al volver al nivel.
    /// </summary>
    public void ReplacePersistentWeaponSlot(int slotIndex, WeaponDa newWeapon, bool makeEquipped = true)
    {
        if (slotIndex < 0 || slotIndex > 2) return;

        string itemName = newWeapon != null ? newWeapon.ItemName : string.Empty;
        Data.SetWeaponSlotName(slotIndex, itemName);

        if (makeEquipped)
            Data.equippedSlotIndex = slotIndex;

        // Compat legacy
        Data.weaponItemName = itemName;

        if (verboseLogs)
            Debug.Log($"GameManager: Loadout persistente actualizado. Slot {slotIndex} -> {itemName}");
    }

    // =======================
    // LOOKUP
    // =======================

    private T GetItem<T>(string itemName) where T : InGameItem
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        if (itemByName == null)
            BuildItemLookup();

        if (itemByName.TryGetValue(itemName, out InGameItem found))
            return found as T;

        return null;
    }
}

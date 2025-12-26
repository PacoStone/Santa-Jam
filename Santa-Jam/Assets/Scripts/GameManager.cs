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

        [Header("Weapon")]
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

            weaponItemName = string.Empty;
            currentMagazine = 0;
            currentReserve = 0;

            inventory.Clear();
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
            Debug.Log($"GameSessionManager: Item lookup listo ({itemByName.Count} items).");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoApplyOnSceneLoad)
            return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            if (verboseLogs)
                Debug.Log("GameSessionManager: No se encontró Player por tag al cargar escena (no se aplica sesión).");
            return;
        }

        ApplyToPlayer(player);
    }

    public void CaptureFromPlayer(GameObject player)
    {
        if (player == null)
            return;

        // Health
        HealthRuntime health = player.GetComponentInChildren<HealthRuntime>();
        Data.currentHealth = health.CurrentHealth;

        // Armour
        ArmourRuntime armour = player.GetComponentInChildren<ArmourRuntime>();
        Data.currentArmour = armour.CurrentArmour;
        Data.armourItemName = armour.CurrentArmourData != null ? armour.CurrentArmourData.ItemName : string.Empty;

        // Weapon
        WeaponRuntime weapon = player.GetComponentInChildren<WeaponRuntime>();
        Data.currentMagazine = weapon.currentMagazine;
        Data.currentReserve = weapon.currentReserve;
        Data.weaponItemName = weapon.weaponData != null ? weapon.weaponData.ItemName : string.Empty;

        // Inventory
        InventoryRuntime inv = player.GetComponentInChildren<InventoryRuntime>();
        Data.inventory.Clear();
        foreach (var kvp in inv.GetSnapshot())
        {
            Data.inventory.Add(new ItemStack
            {
                itemName = kvp.Key,
                amount = kvp.Value
            });
        }

        if (verboseLogs)
            Debug.Log("GameSessionManager: Sesión CAPTURADA desde Player.");
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

        // Weapon
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

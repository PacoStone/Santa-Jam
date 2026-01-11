using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TiendaBotones : MonoBehaviour
{
    [Header("Exit")]
    [SerializeField] private Button exitButton;
    [Tooltip("Nombre exacto de la escena a cargar (debe estar en Build Settings).")]
    [SerializeField] private string exitSceneName;

    [Header("Pulse (UI)")]
    [SerializeField] private RectTransform[] pulseTargets = new RectTransform[2];
    [Range(0f, 0.2f)]
    [SerializeField] private float pulseAmount = 0.02f;
    [SerializeField] private float pulseSpeed = 1.5f;

    [Header("Weapon Cards (Left)")]
    [SerializeField] private List<WeaponCardEntry> weaponCards = new List<WeaponCardEntry>(4);

    [Header("Right Panel (Selection)")]
    [SerializeField] private Image selectedCardImage;
    [SerializeField] private TMP_Text selectedTitleText;
    [SerializeField] private TMP_Text selectedAttackText;
    [SerializeField] private TMP_Text selectedEffectText;
    [SerializeField] private TMP_Text selectedPriceText;

    [Header("Buy")]
    [SerializeField] private Button buyButton;

    [Header("Replace HUD (CambiaArmas)")]
    [Tooltip("Arrastra aquí el objeto CambiaArmas (con CambiaArmasHUDController).")]
    [SerializeField] private CambiaArmasHUDController cambiaArmasHUD;

    private readonly List<Vector3> _pulseBaseScales = new();
    private int _currentIndex = -1;

    private void Awake()
    {
        CachePulseScales();

        if (exitButton != null)
            exitButton.onClick.AddListener(HandleExitClicked);

        if (buyButton != null)
            buyButton.onClick.AddListener(HandleBuyClicked);

        // Registro de WeaponCard + asignación de índice (IMPORTANTE)
        for (int i = 0; i < weaponCards.Count; i++)
        {
            var entry = weaponCards[i];
            if (entry == null || entry.cardImage == null) continue;

            var card = entry.cardImage.GetComponent<WeaponCard>();
            if (card == null)
                card = entry.cardImage.gameObject.AddComponent<WeaponCard>();

            // Esto permite que al hover/click la carta llame a OnCardHovered(i)
            card.Initialize(this, i);
        }
    }

    private void Start()
    {
        // FIX: en Tienda el cursor debe ser visible y libre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Selección inicial: primera carta válida
        for (int i = 0; i < weaponCards.Count; i++)
        {
            if (weaponCards[i] != null && weaponCards[i].weaponData != null)
            {
                SelectWeapon(i);
                break;
            }
        }
    }

    private void Update() => HandlePulse();

    private void CachePulseScales()
    {
        _pulseBaseScales.Clear();
        if (pulseTargets == null) return;

        for (int i = 0; i < pulseTargets.Length; i++)
            _pulseBaseScales.Add(pulseTargets[i] != null ? pulseTargets[i].localScale : Vector3.one);
    }

    private void HandlePulse()
    {
        if (pulseTargets == null || pulseTargets.Length == 0) return;

        float s = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        for (int i = 0; i < pulseTargets.Length; i++)
        {
            RectTransform rt = pulseTargets[i];
            if (rt == null) continue;

            Vector3 baseScale = (i < _pulseBaseScales.Count) ? _pulseBaseScales[i] : Vector3.one;
            rt.localScale = baseScale * s;
        }
    }

    private void HandleExitClicked()
    {
        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            Debug.LogWarning("[TiendaBotones] exitSceneName está vacío.");
            return;
        }

        SceneManager.LoadScene(exitSceneName);
    }

    private void HandleBuyClicked()
    {
        if (_currentIndex < 0 || _currentIndex >= weaponCards.Count)
        {
            Debug.LogWarning("[TiendaBotones] No hay carta seleccionada.");
            return;
        }

        WeaponCardEntry entry = weaponCards[_currentIndex];
        if (entry == null || entry.weaponData == null)
        {
            Debug.LogWarning("[TiendaBotones] WeaponData inválido.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[TiendaBotones] No existe GameManager activo.");
            return;
        }

        if (cambiaArmasHUD == null)
        {
#if UNITY_2023_1_OR_NEWER
            cambiaArmasHUD = FindFirstObjectByType<CambiaArmasHUDController>();
#else
            cambiaArmasHUD = FindObjectOfType<CambiaArmasHUDController>();
#endif
        }

        if (cambiaArmasHUD == null)
        {
            Debug.LogError("[TiendaBotones] No se encontró CambiaArmasHUDController en la escena.");
            return;
        }

        WeaponDa[] currentLoadout = GameManager.Instance.GetCurrentLoadoutWeaponDas();

        cambiaArmasHUD.OpenForShop(currentLoadout, entry.weaponData, (slotIndex) =>
        {
            GameManager.Instance.ReplacePersistentWeaponSlot(
                slotIndex,
                entry.weaponData,
                makeEquipped: true
            );
        });
    }

    public void OnCardHovered(int index)
    {
        SelectWeapon(index);
    }

    private void SelectWeapon(int index)
    {
        if (index < 0 || index >= weaponCards.Count) return;

        WeaponCardEntry entry = weaponCards[index];
        if (entry == null || entry.weaponData == null) return;
        if (_currentIndex == index) return;

        _currentIndex = index;
        ApplySelectionToRightPanel(entry);
    }

    private void ApplySelectionToRightPanel(WeaponCardEntry entry)
    {
        if (selectedCardImage != null)
            selectedCardImage.sprite = entry.cardImage != null ? entry.cardImage.sprite : null;

        if (selectedTitleText != null)
            selectedTitleText.text = !string.IsNullOrWhiteSpace(entry.itemNameOverride)
                ? entry.itemNameOverride
                : entry.weaponData.name;

        // Ataque:** -> Fire rate
        if (selectedAttackText != null)
            selectedAttackText.text = $"ATAQUE: {entry.weaponData.FireRate:0.##}";

        if (selectedEffectText != null)
            selectedEffectText.text = !string.IsNullOrWhiteSpace(entry.descriptionOverride)
                ? entry.descriptionOverride
                : "Sin descripción.";

        if (selectedPriceText != null)
            selectedPriceText.text = $"{entry.price:0}€";
    }

    [Serializable]
    public class WeaponCardEntry
    {
        public WeaponDa weaponData;
        public Image cardImage;
        public float price = 20f;

        [Header("Optional overrides")]
        public string itemNameOverride;
        [TextArea] public string descriptionOverride;
    }
}

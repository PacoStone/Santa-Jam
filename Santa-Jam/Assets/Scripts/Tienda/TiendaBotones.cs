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
    [SerializeField] private Image selectedCardImage;          // Image -> Carta
    [SerializeField] private TMP_Text selectedTitleText;       // Título -> Item Name
    [SerializeField] private TMP_Text selectedAttackText;      // Ataque:** -> Fire rate
    [SerializeField] private TMP_Text selectedEffectText;      // Efecto: -> Descripción
    [SerializeField] private TMP_Text selectedPriceText;       // Precio

    [Header("Buy")]
    [SerializeField] private Button buyButton;

    private readonly List<Vector3> _pulseBaseScales = new();
    private int _currentIndex = -1;

    private void Awake()
    {
        CachePulseScales();

        if (exitButton != null)
            exitButton.onClick.AddListener(HandleExitClicked);

        if (buyButton != null)
            buyButton.onClick.AddListener(HandleBuyClicked);

        // Auto-registro de los "hover" en las imágenes de carta
        // (si olvidas asignar índice manual, también lo asigna).
        for (int i = 0; i < weaponCards.Count; i++)
        {
            var entry = weaponCards[i];
            if (entry == null) continue;

            if (entry.cardImage != null)
            {
                var hover = entry.cardImage.GetComponent<WeaponCard>();
                if (hover == null) hover = entry.cardImage.gameObject.AddComponent<WeaponCard>();

                hover.Initialize(this, i);
            }
        }
    }

    private void Start()
    {
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
            Debug.LogWarning("[ShopUIController] exitSceneName está vacío. No se puede cambiar de escena.");
            return;
        }

        SceneManager.LoadScene(exitSceneName);
    }

    private void HandleBuyClicked()
    {
        Debug.Log("lo he comprado");
    }

    /// <summary>
    /// Llamado por WeaponCardHover cuando el ratón entra en una carta.
    /// </summary>
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
        {
            if (entry.cardImage != null && entry.cardImage.sprite != null)
                selectedCardImage.sprite = entry.cardImage.sprite;
            else
                selectedCardImage.sprite = null;
        }

        if (selectedTitleText != null)
            selectedTitleText.text = !string.IsNullOrWhiteSpace(entry.itemNameOverride)
                ? entry.itemNameOverride
                : entry.weaponData.name;

        // Ataque:** -> Fire rate (según lo pedido)
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

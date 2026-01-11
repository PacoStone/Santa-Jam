using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CambiaArmasHUDController : MonoBehaviour
{
    [Header("Root (CambiaArmas)")]
    [SerializeField] private RectTransform root; // El propio CambiaArmas (RectTransform)

    [Header("Cards (Arma 0/1/2)")]
    [SerializeField] private CambiaArmasCardView card0;
    [SerializeField] private CambiaArmasCardView card1;
    [SerializeField] private CambiaArmasCardView card2;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.18f;
    [SerializeField] private float hiddenOffsetY = 420f; // cuánto “arriba” está oculto

    [Header("Refs (opcionales)")]
    [Tooltip("Si existe en la escena, se usará para bloquear pausa y cambiar action maps. En Tienda puede NO existir.")]
    [SerializeField] private UIManager uiManager;

    [Header("HUD a ocultar mientras eliges (opcional)")]
    [Tooltip("Arrastra aquí tu EmptyObject 'CardsHUD'. En escenas como Tienda puede NO existir.")]
    [SerializeField] private GameObject cardsHUD;

    private bool cardsHUDWasActive;

    private Action<int> onShopChosen;
    private WeaponDa[] shopLoadout;

    private WeaponInventoryController inv;
    private WeaponDa pendingWeapon;
    private bool open;

    private Vector2 shownPos;
    private Vector2 hiddenPos;

    // Estado de cursor para fallback (sin UIManager)
    private bool prevCursorVisible;
    private CursorLockMode prevCursorLock;

    private void Awake()
    {
        if (root == null) root = GetComponent<RectTransform>();

        // Buscar UIManager si no está asignado (en Tienda puede ser null y no pasa nada)
        if (uiManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            uiManager = FindFirstObjectByType<UIManager>();
#else
            uiManager = FindObjectOfType<UIManager>();
#endif
        }

        // Buscar CardsHUD automáticamente si no está asignado (opcional)
        if (cardsHUD == null)
        {
            var found = GameObject.Find("CardsHUD");
            if (found != null) cardsHUD = found;
        }

        shownPos = root.anchoredPosition;
        hiddenPos = shownPos + Vector2.up * hiddenOffsetY;

        // Por defecto: desactivado en escena y/o escondido arriba
        root.anchoredPosition = hiddenPos;
        gameObject.SetActive(false);

        // Wire clicks
        card0.SetClickCallback(() => ChooseSlot(0));
        card1.SetClickCallback(() => ChooseSlot(1));
        card2.SetClickCallback(() => ChooseSlot(2));
    }

    public bool IsOpen => open;

    public void Open(WeaponInventoryController inventory, WeaponDa newWeapon)
    {
        if (open) return;
        if (inventory == null || newWeapon == null) return;

        inv = inventory;
        pendingWeapon = newWeapon;

        // Rellena las 3 cartas con las 3 armas equipadas
        card0.SetWeapon(inv.GetWeapon(0));
        card1.SetWeapon(inv.GetWeapon(1));
        card2.SetWeapon(inv.GetWeapon(2));

        // Oculta CardsHUD (si existe)
        if (cardsHUD != null)
        {
            cardsHUDWasActive = cardsHUD.activeSelf;
            cardsHUD.SetActive(false);
        }

        gameObject.SetActive(true);
        open = true;

        // Modo modal:
        // - Si hay UIManager: úsalo (bloqueo pausa, action map UI, cursor, freeze)
        // - Si NO hay UIManager (ej: escena Tienda): fallback simple
        if (uiManager != null)
        {
            uiManager.EnterModalUI(freezeTime: true);
        }
        else
        {
            Time.timeScale = 0f;

            prevCursorVisible = Cursor.visible;
            prevCursorLock = Cursor.lockState;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        StopAllCoroutines();
        StartCoroutine(UITween.SlideAnchored(root, hiddenPos, shownPos, slideDuration));
    }
    public void OpenForShop(WeaponDa[] currentLoadout, WeaponDa newWeapon, Action<int> onChosen)
    {
        if (open) return;
        if (currentLoadout == null || currentLoadout.Length != 3) return;
        if (newWeapon == null) return;

        inv = null; // modo tienda: no usamos inventario real
        pendingWeapon = newWeapon;
        onShopChosen = onChosen;
        shopLoadout = currentLoadout;

        card0.SetWeapon(shopLoadout[0]);
        card1.SetWeapon(shopLoadout[1]);
        card2.SetWeapon(shopLoadout[2]);

        gameObject.SetActive(true);
        open = true;

        // fallback simple si no hay UIManager en tienda
        if (uiManager != null) uiManager.EnterModalUI(freezeTime: true);
        else
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        StopAllCoroutines();
        StartCoroutine(UITween.SlideAnchored(root, hiddenPos, shownPos, slideDuration));
    }

    private void ChooseSlot(int slotIndex)
    {
        if (!open) return;

        // Gameplay
        if (inv != null && pendingWeapon != null)
        {
            inv.ReplaceSlot(slotIndex, pendingWeapon, equipAfter: true);
            Close();
            return;
        }

        // Tienda
        if (inv == null && pendingWeapon != null && onShopChosen != null)
        {
            onShopChosen.Invoke(slotIndex);
            Close();
            return;
        }

        Close();
    }


    private void Close()
    {
        if (!open) return;
        open = false;

        StopAllCoroutines();
        StartCoroutine(CloseRoutine());
        onShopChosen = null;
        shopLoadout = null;
    }

    private System.Collections.IEnumerator CloseRoutine()
    {
        yield return UITween.SlideAnchored(root, shownPos, hiddenPos, slideDuration);

        // Salir del modal
        if (uiManager != null)
        {
            uiManager.ExitModalUI(resumeTime: true);
        }
        else
        {
            Time.timeScale = 1f;

            Cursor.visible = prevCursorVisible;
            Cursor.lockState = prevCursorLock;
        }

        // Restaurar CardsHUD
        if (cardsHUD != null)
            cardsHUD.SetActive(cardsHUDWasActive);

        inv = null;
        pendingWeapon = null;

        gameObject.SetActive(false);
    }
}

/// <summary>
/// Vista de cada carta (Arma 0/1/2). Usa Image.
/// </summary>
[Serializable]
public class CambiaArmasCardView
{
    [SerializeField] private Image icon;
    [SerializeField] private CambiaArmasTiltAndClick tiltAndClick;

    public void SetWeapon(WeaponDa weapon)
    {
        if (icon == null) return;

        Sprite s = WeaponIconResolver.TryGetSprite(weapon);
        if (s != null) icon.sprite = s;
        // Si no hay sprite, conserva el que haya en el inspector
    }

    public void SetClickCallback(Action cb)
    {
        if (tiltAndClick != null)
            tiltAndClick.SetClick(cb);
    }
}

/// <summary>
/// Resuelve un Sprite de WeaponDa/InGameItem sin asumir nombre exacto del campo.
/// Busca propiedades/campos típicos: Icon, icon, ItemIcon, ItemSprite, Sprite, sprite...
/// </summary>
public static class WeaponIconResolver
{
    private static readonly string[] Candidates =
    {
        "Icon", "icon", "ItemIcon", "ItemSprite", "Sprite", "sprite", "UISprite", "uiSprite"
    };

    public static Sprite TryGetSprite(object obj)
    {
        if (obj == null) return null;

        Type t = obj.GetType();

        // Propiedades
        foreach (string name in Candidates)
        {
            PropertyInfo p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType))
                return p.GetValue(obj) as Sprite;
        }

        // Campos
        foreach (string name in Candidates)
        {
            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
                return f.GetValue(obj) as Sprite;
        }

        return null;
    }
}

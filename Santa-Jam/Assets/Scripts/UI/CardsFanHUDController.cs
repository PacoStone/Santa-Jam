using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardsFanHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager input;
    [SerializeField] private WeaponInventoryController inventory;

    [Header("Cards (RectTransforms) - order = slot index")]
    [Tooltip("Un elemento por slot. El índice 0..N-1 debe corresponder con el slot del inventario.")]
    [SerializeField] private RectTransform[] cards;

    [Header("Hints (GameObjects)")]
    [SerializeField] private GameObject hintQ;
    [SerializeField] private GameObject hintE;

    [Header("Optional overlays (same length/order as cards)")]
    [SerializeField] private Image[] icons;
    [SerializeField] private Image[] reloadFills;

    [Header("Fan Layout")]
    [SerializeField] private float sideX = 80f;
    [SerializeField] private float sideY = -15f;
    [SerializeField] private float sideRotZ = 20f;
    [SerializeField] private float centerY = 0f;

    [Header("Scale Multipliers (respect editor scale)")]
    [SerializeField] private float centerScaleMultiplier = 1.00f;
    [SerializeField] private float sideScaleMultiplier = 0.92f;

    [Header("Animation")]
    [SerializeField] private float moveDur = 0.22f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Safety / Debug")]
    [SerializeField] private bool autoWireRefs = true;

    // Base pose
    private Vector2[] _baseAnchoredPos;
    private Vector3[] _baseScales;
    private float[] _baseRotZ;
    private bool _basePoseCached;

    private int _lastIndex = int.MinValue;
    private Tween _layoutTween;

    private void Awake()
    {
        if (autoWireRefs)
            AutoWire();

        CacheBasePose();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnWeaponChanged += HandleWeaponChanged;
    }

    private void Start()
    {
        ForceRefreshLayout();
        FullRefresh(forceInstant: true);
    }

    private void Update()
    {
        if (inventory == null || cards == null || cards.Length == 0)
        {
            SetHints(false);
            SetAllCardsActive(false);
            return;
        }

        // Cambio con Q/E (si tu InputManager lo expone)
        if (input != null && inventory.WeaponCount() >= 2)
        {
            if (input.changeGunLeftPressed) inventory.ChangeLeft();
            if (input.changeGunRightPressed) inventory.ChangeRight();
        }

        FullRefresh(forceInstant: false);
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnWeaponChanged -= HandleWeaponChanged;

        RestoreBasePose();
    }

    private void OnDestroy()
    {
        RestoreBasePose();
    }

    private void HandleWeaponChanged(int _)
    {
        ForceRefreshLayout();
    }

    [ContextMenu("Force Refresh Layout")]
    public void ForceRefreshLayout()
    {
        _lastIndex = int.MinValue;
    }

    private void FullRefresh(bool forceInstant)
    {
        int slotCountUI = cards.Length;

        // Nota: WeaponCount() solo cuenta slots ocupados; slotCountUI es cuántas “cartas” existen en UI
        int countWeapons = inventory.WeaponCount();
        bool canCycle = countWeapons >= 2;

        SetHints(canCycle);

        if (countWeapons == 0)
        {
            SetAllCardsActive(false);
            return;
        }

        // Activa/desactiva cartas por ocupación del slot
        for (int i = 0; i < slotCountUI; i++)
        {
            bool occupied = inventory.GetWeapon(i) != null; // si tu inventario aún es de 3 slots, esto necesitará ampliarse
            if (cards[i] != null) cards[i].gameObject.SetActive(occupied);
        }

        RefreshWeaponIconsOverlay();
        RefreshReloadFills();

        // Layout
        if (countWeapons == 1)
        {
            int only = FindOnlyOccupiedSlot(slotCountUI);
            if (only == -1)
            {
                SetAllCardsActive(false);
                return;
            }

            if (forceInstant) ApplySingleInstant(only);
            else ApplySingleAnimated(only);

            return;
        }

        int idx = inventory.CurrentIndex;
        if (idx < 0) idx = FindFirstOccupiedSlot(slotCountUI);

        if (idx != _lastIndex)
        {
            _lastIndex = idx;

            if (forceInstant) ApplyFanInstant(idx);
            else ApplyFanAnimated(idx);
        }
    }

    private void ApplySingleInstant(int onlySlot)
    {
        _layoutTween?.Kill();

        var c = GetCard(onlySlot);
        if (c == null) return;

        c.anchoredPosition = new Vector2(0f, centerY);
        c.localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[onlySlot]);
        c.localScale = _baseScales[onlySlot] * centerScaleMultiplier;

        c.SetAsLastSibling();
    }

    private void ApplySingleAnimated(int onlySlot)
    {
        _layoutTween?.Kill();

        var c = GetCard(onlySlot);
        if (c == null) return;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        Vector2 pos = new Vector2(0f, centerY);
        float z = _baseRotZ[onlySlot];
        Vector3 sc = _baseScales[onlySlot] * centerScaleMultiplier;

        seq.Join(c.DOAnchorPos(pos, moveDur).SetEase(moveEase));
        seq.Join(c.DOLocalRotate(new Vector3(0f, 0f, z), moveDur).SetEase(moveEase));
        seq.Join(c.DOScale(sc, moveDur).SetEase(moveEase));

        seq.OnComplete(() => c.SetAsLastSibling());
        _layoutTween = seq;
    }

    private void ApplyFanInstant(int selectedIndex)
    {
        _layoutTween?.Kill();

        // Solo mostramos un “fan” de 3: izquierda-centro-derecha alrededor del seleccionado (estilo Balatro).
        // El resto de cartas, si existen, se quedan donde estén (o puedes ocultarlas si quieres).
        int left = FindPrevOccupied(selectedIndex);
        int right = FindNextOccupied(selectedIndex);

        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            if (c == null || !c.gameObject.activeSelf) continue;

            GetFanTargets3(i, left, selectedIndex, right, out Vector2 pos, out float rotZOffset, out float scaleMul);

            c.anchoredPosition = pos;
            c.localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[i] + rotZOffset);
            c.localScale = _baseScales[i] * scaleMul;
        }

        var sel = GetCard(selectedIndex);
        if (sel != null) sel.SetAsLastSibling();
    }

    private void ApplyFanAnimated(int selectedIndex)
    {
        _layoutTween?.Kill();

        int left = FindPrevOccupied(selectedIndex);
        int right = FindNextOccupied(selectedIndex);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            if (c == null || !c.gameObject.activeSelf) continue;

            GetFanTargets3(i, left, selectedIndex, right, out Vector2 pos, out float rotZOffset, out float scaleMul);

            Vector3 targetScale = _baseScales[i] * scaleMul;
            float targetRotZ = _baseRotZ[i] + rotZOffset;

            seq.Join(c.DOAnchorPos(pos, moveDur).SetEase(moveEase));
            seq.Join(c.DOLocalRotate(new Vector3(0f, 0f, targetRotZ), moveDur).SetEase(moveEase));
            seq.Join(c.DOScale(targetScale, moveDur).SetEase(moveEase));
        }

        seq.OnComplete(() =>
        {
            var sel = GetCard(selectedIndex);
            if (sel != null) sel.SetAsLastSibling();
        });

        _layoutTween = seq;

        var selected = GetCard(selectedIndex);
        if (selected != null)
            selected.DOPunchScale(_baseScales[selectedIndex] * 0.06f, 0.16f, 10, 0.85f).SetUpdate(true);
    }

    private void GetFanTargets3(int cardIndex, int leftIndex, int selectedIndex, int rightIndex,
        out Vector2 pos, out float rotZOffset, out float scaleMul)
    {
        if (cardIndex == selectedIndex)
        {
            pos = new Vector2(0f, centerY);
            rotZOffset = 0f;
            scaleMul = centerScaleMultiplier;
            return;
        }

        if (cardIndex == leftIndex)
        {
            pos = new Vector2(-sideX, sideY);
            rotZOffset = +sideRotZ;
            scaleMul = sideScaleMultiplier;
            return;
        }

        if (cardIndex == rightIndex)
        {
            pos = new Vector2(+sideX, sideY);
            rotZOffset = -sideRotZ;
            scaleMul = sideScaleMultiplier;
            return;
        }

        // Si hay más de 3 cartas activas, a las “no foco” las dejamos atenuadas y atrás.
        // Puedes cambiar esto por ocultarlas o apilarlas.
        pos = new Vector2(0f, sideY - 30f);
        rotZOffset = 0f;
        scaleMul = sideScaleMultiplier * 0.90f;
    }

    private void RefreshWeaponIconsOverlay()
    {
        if (icons == null || icons.Length == 0) return;

        int n = Mathf.Min(icons.Length, cards.Length);

        for (int i = 0; i < n; i++)
        {
            var icon = icons[i];
            if (icon == null) continue;

            var w = inventory.GetWeapon(i);
            if (w != null && w.Icon != null)
            {
                icon.sprite = w.Icon;
                SetAlpha(icon, 1f);
            }
            else
            {
                SetAlpha(icon, 0f);
            }
        }
    }

    private void RefreshReloadFills()
    {
        if (reloadFills == null || reloadFills.Length == 0) return;

        int n = Mathf.Min(reloadFills.Length, cards.Length);

        for (int i = 0; i < n; i++)
        {
            var fill = reloadFills[i];
            if (fill == null) continue;

            if (cards[i] == null || !cards[i].gameObject.activeSelf)
            {
                SetAlpha(fill, 0f);
                continue;
            }

            float t = inventory.GetReloadProgress01(i);
            fill.fillAmount = Mathf.Clamp01(t);
            SetAlpha(fill, t > 0f ? 1f : 0f);
        }
    }

    private void SetHints(bool active)
    {
        if (hintQ != null) hintQ.SetActive(active);
        if (hintE != null) hintE.SetActive(active);
    }

    private void SetAllCardsActive(bool active)
    {
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
            if (cards[i] != null) cards[i].gameObject.SetActive(active);
    }

    private int FindOnlyOccupiedSlot(int slotCountUI)
    {
        int found = -1;
        for (int i = 0; i < slotCountUI; i++)
        {
            if (inventory.GetWeapon(i) != null)
            {
                if (found != -1) return -1;
                found = i;
            }
        }
        return found;
    }

    private int FindFirstOccupiedSlot(int slotCountUI)
    {
        for (int i = 0; i < slotCountUI; i++)
            if (inventory.GetWeapon(i) != null) return i;
        return -1;
    }

    private int FindPrevOccupied(int from)
    {
        if (cards == null || cards.Length == 0) return from;

        for (int step = 1; step <= cards.Length; step++)
        {
            int i = Mod(from - step, cards.Length);
            if (inventory.GetWeapon(i) != null) return i;
        }
        return from;
    }

    private int FindNextOccupied(int from)
    {
        if (cards == null || cards.Length == 0) return from;

        for (int step = 1; step <= cards.Length; step++)
        {
            int i = Mod(from + step, cards.Length);
            if (inventory.GetWeapon(i) != null) return i;
        }
        return from;
    }

    private RectTransform GetCard(int i)
    {
        if (cards == null) return null;
        if (i < 0 || i >= cards.Length) return null;
        return cards[i];
    }

    private void CacheBasePose()
    {
        if (cards == null) return;

        int n = cards.Length;
        _baseAnchoredPos = new Vector2[n];
        _baseScales = new Vector3[n];
        _baseRotZ = new float[n];

        for (int i = 0; i < n; i++)
        {
            if (cards[i] == null) continue;
            _baseAnchoredPos[i] = cards[i].anchoredPosition;
            _baseScales[i] = cards[i].localScale;
            _baseRotZ[i] = cards[i].localEulerAngles.z;
        }

        _basePoseCached = true;
    }

    private void RestoreBasePose()
    {
        if (!_basePoseCached || cards == null) return;

        _layoutTween?.Kill();

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            DOTween.Kill(cards[i], complete: false);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;

            cards[i].anchoredPosition = _baseAnchoredPos[i];
            cards[i].localScale = _baseScales[i];
            cards[i].localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[i]);
        }
    }

    private void AutoWire()
    {
        if (input != null && inventory != null) return;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            if (input == null) input = player.GetComponent<InputManager>();
            if (inventory == null) inventory = player.GetComponent<WeaponInventoryController>();
        }

        if (input == null) input = FindFirstObjectByType<InputManager>();
        if (inventory == null) inventory = FindFirstObjectByType<WeaponInventoryController>();
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }

    private int Mod(int a, int m) => (a % m + m) % m;
}

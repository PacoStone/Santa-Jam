using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardsFanHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager input;
    [SerializeField] private WeaponInventoryController inventory;

    [Header("Cards (RectTransforms)")]
    [SerializeField] private RectTransform card0;
    [SerializeField] private RectTransform card1;
    [SerializeField] private RectTransform card2;

    [Header("Hints (GameObjects)")]
    [SerializeField] private GameObject hintQ;
    [SerializeField] private GameObject hintE;

    [Header("Optional overlays (NOT the card background)")]
    [Tooltip("Asigna solo si tienes un Image 'WeaponIcon' por carta. Si lo dejas vacío, no toco sprites/alpha.")]
    [SerializeField] private Image icon0;
    [SerializeField] private Image icon1;
    [SerializeField] private Image icon2;

    [Tooltip("Asigna solo si tienes un Image filled para recarga por carta.")]
    [SerializeField] private Image reloadFill0;
    [SerializeField] private Image reloadFill1;
    [SerializeField] private Image reloadFill2;

    [Header("Fan Layout (offsets)")]
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
    [Tooltip("Auto-encontrar Input/Inventory si están en None (no toca transforms).")]
    [SerializeField] private bool autoWireRefs = true;

    // Internals
    private RectTransform[] _cards;
    private Image[] _icons;
    private Image[] _reloadFills;

    // Pose base (la del editor) para restaurar al salir
    private Vector2[] _baseAnchoredPos;
    private Vector3[] _baseScales;
    private float[] _baseRotZ;

    private bool _basePoseCached;

    private int _lastIndex = int.MinValue;
    private Tween _layoutTween;

    private void Awake()
    {
        _cards = new[] { card0, card1, card2 };
        _icons = new[] { icon0, icon1, icon2 };
        _reloadFills = new[] { reloadFill0, reloadFill1, reloadFill2 };

        if (autoWireRefs)
            AutoWire();

        CacheBasePose(); // IMPORTANT: cachea la pose del editor al entrar en play
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
        if (inventory == null)
        {
            SetHints(false);
            SetAllCardsActive(false);
            return;
        }

        if (input != null && inventory.HasAtLeastTwoWeapons())
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

        // Al desactivar (y al salir de Play), restauramos la pose base.
        RestoreBasePose();
    }

    private void OnDestroy()
    {
        // Extra safety
        RestoreBasePose();
    }

    private void HandleWeaponChanged(int _)
    {
        ForceRefreshLayout();
    }

    [ContextMenu("Recache Base Pose From Current Transforms")]
    public void RecacheBasePoseFromCurrent()
    {
        CacheBasePose();
    }

    [ContextMenu("Restore Base Pose Now")]
    public void RestoreBasePoseNow()
    {
        RestoreBasePose();
    }

    [ContextMenu("Force Refresh Layout")]
    public void ForceRefreshLayout()
    {
        _lastIndex = int.MinValue;
    }

    private void FullRefresh(bool forceInstant)
    {
        int count = inventory.WeaponCount();
        bool canCycle = count >= 2;

        SetHints(canCycle);

        if (count == 0)
        {
            SetAllCardsActive(false);
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            bool occupied = inventory.GetWeapon(i) != null;
            if (_cards[i] != null) _cards[i].gameObject.SetActive(occupied);
        }

        RefreshWeaponIconsOverlay();

        if (count == 1)
        {
            int only = FindOnlyOccupiedSlot();
            if (only == -1)
            {
                SetAllCardsActive(false);
                return;
            }

            if (forceInstant) ApplySingleInstant(only);
            else ApplySingleAnimated(only);

            RefreshReloadFills();
            return;
        }

        int idx = inventory.CurrentIndex;
        if (idx < 0) idx = FindFirstOccupiedSlot();

        if (idx != _lastIndex)
        {
            _lastIndex = idx;

            if (forceInstant) ApplyFanInstant(idx);
            else ApplyFanAnimated(idx);
        }

        RefreshReloadFills();
    }

    private void ApplySingleInstant(int onlySlot)
    {
        _layoutTween?.Kill();

        if (_cards[onlySlot] == null) return;

        _cards[onlySlot].anchoredPosition = new Vector2(0f, centerY);
        _cards[onlySlot].localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[onlySlot]);
        _cards[onlySlot].localScale = _baseScales[onlySlot] * centerScaleMultiplier;

        _cards[onlySlot].SetAsLastSibling();
    }

    private void ApplySingleAnimated(int onlySlot)
    {
        _layoutTween?.Kill();

        if (_cards[onlySlot] == null) return;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        Vector2 pos = new Vector2(0f, centerY);
        float z = _baseRotZ[onlySlot];
        Vector3 sc = _baseScales[onlySlot] * centerScaleMultiplier;

        seq.Join(_cards[onlySlot].DOAnchorPos(pos, moveDur).SetEase(moveEase));
        seq.Join(_cards[onlySlot].DOLocalRotate(new Vector3(0f, 0f, z), moveDur).SetEase(moveEase));
        seq.Join(_cards[onlySlot].DOScale(sc, moveDur).SetEase(moveEase));

        seq.OnComplete(() => _cards[onlySlot].SetAsLastSibling());
        _layoutTween = seq;
    }

    private void ApplyFanInstant(int selectedIndex)
    {
        _layoutTween?.Kill();

        for (int i = 0; i < 3; i++)
        {
            if (_cards[i] == null) continue;
            if (!_cards[i].gameObject.activeSelf) continue;

            GetFanTargets(i, selectedIndex, out Vector2 pos, out float rotZOffset, out float scaleMul);

            _cards[i].anchoredPosition = pos;
            _cards[i].localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[i] + rotZOffset);
            _cards[i].localScale = _baseScales[i] * scaleMul;
        }

        if (_cards[selectedIndex] != null)
            _cards[selectedIndex].SetAsLastSibling();
    }

    private void ApplyFanAnimated(int selectedIndex)
    {
        _layoutTween?.Kill();

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < 3; i++)
        {
            if (_cards[i] == null) continue;
            if (!_cards[i].gameObject.activeSelf) continue;

            GetFanTargets(i, selectedIndex, out Vector2 pos, out float rotZOffset, out float scaleMul);

            Vector3 targetScale = _baseScales[i] * scaleMul;
            float targetRotZ = _baseRotZ[i] + rotZOffset;

            seq.Join(_cards[i].DOAnchorPos(pos, moveDur).SetEase(moveEase));
            seq.Join(_cards[i].DOLocalRotate(new Vector3(0f, 0f, targetRotZ), moveDur).SetEase(moveEase));
            seq.Join(_cards[i].DOScale(targetScale, moveDur).SetEase(moveEase));
        }

        seq.OnComplete(() =>
        {
            if (_cards[selectedIndex] != null)
                _cards[selectedIndex].SetAsLastSibling();
        });

        _layoutTween = seq;

        if (_cards[selectedIndex] != null)
            _cards[selectedIndex].DOPunchScale(_baseScales[selectedIndex] * 0.06f, 0.16f, 10, 0.85f).SetUpdate(true);
    }

    private void GetFanTargets(int cardIndex, int selectedIndex, out Vector2 pos, out float rotZOffset, out float scaleMul)
    {
        int leftIndex = Mod(selectedIndex - 1, 3);
        int rightIndex = Mod(selectedIndex + 1, 3);

        if (cardIndex == selectedIndex)
        {
            pos = new Vector2(0f, centerY);
            rotZOffset = 0f;
            scaleMul = centerScaleMultiplier;
        }
        else if (cardIndex == leftIndex)
        {
            pos = new Vector2(-sideX, sideY);
            rotZOffset = +sideRotZ;
            scaleMul = sideScaleMultiplier;
        }
        else
        {
            pos = new Vector2(+sideX, sideY);
            rotZOffset = -sideRotZ;
            scaleMul = sideScaleMultiplier;
        }
    }

    private void RefreshWeaponIconsOverlay()
    {
        bool anyIcons = false;
        for (int i = 0; i < 3; i++)
            if (_icons[i] != null) { anyIcons = true; break; }
        if (!anyIcons) return;

        for (int i = 0; i < 3; i++)
        {
            if (_icons[i] == null) continue;

            var w = inventory.GetWeapon(i);
            if (w != null && w.Icon != null)
            {
                _icons[i].sprite = w.Icon;
                SetAlpha(_icons[i], 1f);
            }
            else
            {
                SetAlpha(_icons[i], 0f);
            }
        }
    }

    private void RefreshReloadFills()
    {
        bool anyFills = false;
        for (int i = 0; i < 3; i++)
            if (_reloadFills[i] != null) { anyFills = true; break; }
        if (!anyFills) return;

        for (int i = 0; i < 3; i++)
        {
            if (_reloadFills[i] == null) continue;

            if (_cards[i] == null || !_cards[i].gameObject.activeSelf)
            {
                SetAlpha(_reloadFills[i], 0f);
                continue;
            }

            float t = inventory.GetReloadProgress01(i);
            _reloadFills[i].fillAmount = Mathf.Clamp01(t);
            SetAlpha(_reloadFills[i], t > 0f ? 1f : 0f);
        }
    }

    private void SetHints(bool active)
    {
        if (hintQ != null) hintQ.SetActive(active);
        if (hintE != null) hintE.SetActive(active);
    }

    private void SetAllCardsActive(bool active)
    {
        for (int i = 0; i < 3; i++)
            if (_cards[i] != null) _cards[i].gameObject.SetActive(active);
    }

    private int FindOnlyOccupiedSlot()
    {
        int found = -1;
        for (int i = 0; i < 3; i++)
        {
            if (inventory.GetWeapon(i) != null)
            {
                if (found != -1) return -1;
                found = i;
            }
        }
        return found;
    }

    private int FindFirstOccupiedSlot()
    {
        for (int i = 0; i < 3; i++)
            if (inventory.GetWeapon(i) != null) return i;
        return -1;
    }

    private void CacheBasePose()
    {
        _baseAnchoredPos = new Vector2[3];
        _baseScales = new Vector3[3];
        _baseRotZ = new float[3];

        for (int i = 0; i < 3; i++)
        {
            if (_cards[i] == null) continue;

            _baseAnchoredPos[i] = _cards[i].anchoredPosition;
            _baseScales[i] = _cards[i].localScale;
            _baseRotZ[i] = _cards[i].localEulerAngles.z;
        }

        _basePoseCached = true;
    }

    private void RestoreBasePose()
    {
        if (!_basePoseCached) return;

        _layoutTween?.Kill();

        // Mata tweens que puedan seguir vivos en estos rects
        for (int i = 0; i < 3; i++)
        {
            if (_cards[i] == null) continue;
            DOTween.Kill(_cards[i], complete: false);
        }

        // Restaura valores del editor
        for (int i = 0; i < 3; i++)
        {
            if (_cards[i] == null) continue;

            _cards[i].anchoredPosition = _baseAnchoredPos[i];
            _cards[i].localScale = _baseScales[i];
            _cards[i].localRotation = Quaternion.Euler(0f, 0f, _baseRotZ[i]);
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

using System.Collections;
using UnityEngine;

public class PauseMenuUIManager : MonoBehaviour
{
    private enum MenuState
    {
        Main,
        ConfirmExit
        // luego podrás añadir: Options
    }

    [Header("UI Groups")]
    [SerializeField] private GameObject mainButtonsGroup;   // "3 botones"
    [SerializeField] private GameObject confirmExitGroup;   // "confirmar salir"

    [Header("Reveal Objects (Ghost + Text)")]
    [SerializeField] private RectTransform ghost;
    [SerializeField] private RectTransform ghostText;

    [Header("How to Move (Anchors vs Local Position)")]
    [Tooltip("AnchoredPosition funciona bien si los anchors NO están en Stretch. " +
             "LocalPosition es más fiable si tus elementos están en Stretch o con offsets raros.")]
    [SerializeField] private bool useAnchoredPosition = true;

    [Header("Hidden Offsets (from the SHOWN position)")]
    [Tooltip("Fantasma: offset diagonal para ocultarlo detrás.")]
    [SerializeField] private Vector2 ghostHiddenOffset = new Vector2(100f, -40f);

    [Tooltip("Texto: offset solo horizontal para ocultarlo (normalmente hacia la derecha).")]
    [SerializeField] private Vector2 textHiddenOffset = new Vector2(600f, 0f);

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private MenuState _current = MenuState.Main;
    private MenuState _previous = MenuState.Main;

    // Positions captured as "SHOWN" (visible) in inspector layout:
    private Vector3 _ghostShown;
    private Vector3 _textShown;

    // Computed "HIDDEN" positions:
    private Vector3 _ghostHidden;
    private Vector3 _textHidden;

    private Coroutine _ghostCo;
    private Coroutine _textCo;

    private void Awake()
    {
        // Seguridad: si falta algo, lo verás claro.
        if (mainButtonsGroup == null) Debug.LogError("[PauseMenuUIManager] Falta Main Buttons Group (3 botones).", this);
        if (confirmExitGroup == null) Debug.LogError("[PauseMenuUIManager] Falta Confirm Exit Group (confirmar salir).", this);
        if (ghost == null) Debug.LogError("[PauseMenuUIManager] Falta referencia al fantasma (RectTransform).", this);
        if (ghostText == null) Debug.LogError("[PauseMenuUIManager] Falta referencia al texto del fantasma (RectTransform).", this);

        CaptureShownPositions();
        ComputeHiddenPositions();

        // Arranque: menú principal visible, confirmación oculta, y fantasma/texto ocultos.
        SetState(MenuState.Main, instant: true);
        HideRevealInstant();
    }

    // --------------------
    // Buttons
    // --------------------

    // Enlaza esto al botón "Salir"
    public void OnExitPressed()
    {
        _previous = _current;
        SetState(MenuState.ConfirmExit, instant: false);
        ShowRevealAnimated();
    }

    // Enlaza esto al botón "No"
    public void OnCancelExit()
    {
        SetState(_previous, instant: false);
        HideRevealAnimated();
    }

    // Enlaza esto al botón "Sí" si quieres (opcional).
    // El cambio de escena NO está aquí; lo haces tú con LevelLoader.
    public void OnConfirmExit()
    {
        // Intencionalmente vacío.
        // Si quieres, aquí podrías desactivar el menú o reproducir un sonido.
    }

    // --------------------
    // State / UI
    // --------------------

    private void SetState(MenuState state, bool instant)
    {
        _current = state;

        if (logStateChanges)
            Debug.Log($"[PauseMenuUIManager] State -> {_current}", this);

        switch (state)
        {
            case MenuState.Main:
                if (mainButtonsGroup != null) mainButtonsGroup.SetActive(true);
                if (confirmExitGroup != null) confirmExitGroup.SetActive(false);
                break;

            case MenuState.ConfirmExit:
                if (mainButtonsGroup != null) mainButtonsGroup.SetActive(false);
                if (confirmExitGroup != null) confirmExitGroup.SetActive(true);
                break;
        }
    }

    // --------------------
    // Reveal (Ghost + Text)
    // --------------------

    private void CaptureShownPositions()
    {
        // Lo que tengas colocado “bonito” en el editor cuenta como SHOWN.
        _ghostShown = GetPos(ghost);
        _textShown = GetPos(ghostText);
    }

    private void ComputeHiddenPositions()
    {
        _ghostHidden = _ghostShown + (Vector3)ghostHiddenOffset;
        _textHidden = _textShown + (Vector3)textHiddenOffset;
    }

    private void ShowRevealAnimated()
    {
        StartTween(ghost, _ghostShown, ref _ghostCo);
        StartTween(ghostText, _textShown, ref _textCo);
    }

    private void HideRevealAnimated()
    {
        StartTween(ghost, _ghostHidden, ref _ghostCo);
        StartTween(ghostText, _textHidden, ref _textCo);
    }

    private void HideRevealInstant()
    {
        StopTween(ref _ghostCo);
        StopTween(ref _textCo);

        SetPos(ghost, _ghostHidden);
        SetPos(ghostText, _textHidden);
    }

    private void StartTween(RectTransform rt, Vector3 to, ref Coroutine routine)
    {
        if (rt == null) return;

        StopTween(ref routine);
        routine = StartCoroutine(CoTween(rt, to));
    }

    private void StopTween(ref Coroutine routine)
    {
        if (routine == null) return;
        StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator CoTween(RectTransform rt, Vector3 to)
    {
        Vector3 from = GetPos(rt);

        float d = Mathf.Max(0.0001f, duration);
        float t = 0f;

        while (t < d)
        {
            t += Time.unscaledDeltaTime; // funciona en pausa
            float a = Mathf.Clamp01(t / d);
            float e = (ease != null) ? ease.Evaluate(a) : a;

            SetPos(rt, Vector3.LerpUnclamped(from, to, e));
            yield return null;
        }

        SetPos(rt, to);
    }

    private Vector3 GetPos(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;
        return useAnchoredPosition ? (Vector3)rt.anchoredPosition : rt.localPosition;
    }

    private void SetPos(RectTransform rt, Vector3 value)
    {
        if (rt == null) return;

        if (useAnchoredPosition)
            rt.anchoredPosition = (Vector2)value;
        else
            rt.localPosition = value;
    }

#if UNITY_EDITOR
    [ContextMenu("Recapture SHOWN Positions (from current layout)")]
    private void Editor_RecaptureShown()
    {
        CaptureShownPositions();
        ComputeHiddenPositions();
        Debug.Log("[PauseMenuUIManager] SHOWN positions recaptured from current layout.", this);
    }

    [ContextMenu("Test: Show Reveal")]
    private void Editor_TestShow()
    {
        ShowRevealAnimated();
    }

    [ContextMenu("Test: Hide Reveal")]
    private void Editor_TestHide()
    {
        HideRevealAnimated();
    }
#endif
}

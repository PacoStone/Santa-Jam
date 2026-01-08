using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class WeaponCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Hover Select")]
    private TiendaBotones _shop;
    private int _index;

    [Header("Balatro-like Tilt")]
    [Tooltip("Camera usada por el Canvas. Déjala vacía si tu Canvas es Screen Space - Overlay.")]
    [SerializeField] private Camera uiCamera;

    [Tooltip("Máxima rotación en grados (X e Y) cuando el ratón está en el borde de la carta.")]
    [SerializeField] private float maxTilt = 12f;

    [Tooltip("Rotación Z extra (estética) en función de la X del ratón. 0 para desactivar.")]
    [SerializeField] private float maxRollZ = 4f;

    [Tooltip("Cuánto se 'acerca' la rotación a su objetivo (más alto = más rápido).")]
    [SerializeField] private float tiltResponsiveness = 14f;

    [Tooltip("Velocidad de retorno al estado neutro al salir del hover.")]
    [SerializeField] private float returnSpeed = 10f;

    [Tooltip("Escala ligera al hacer hover (1.02 recomendado). 1 para desactivar.")]
    [SerializeField] private float hoverScale = 1.02f;

    [Tooltip("Suavizado de escala.")]
    [SerializeField] private float scaleSpeed = 12f;

    private RectTransform _rt;
    private bool _hovering;
    private Vector2 _localCursor;
    private Quaternion _baseRotation;
    private Vector3 _baseScale;

    private Quaternion _currentRotation;

    public void Initialize(TiendaBotones shop, int index)
    {
        _shop = shop;
        _index = index;
    }

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _baseRotation = _rt.localRotation;
        _baseScale = _rt.localScale;
        _currentRotation = _baseRotation;
    }

    private void Update()
    {
        // Objetivo de rotación
        Quaternion targetRot = _baseRotation;

        if (_hovering)
        {
            // Normaliza el cursor dentro del rect: -1..1 (centro = 0)
            Rect r = _rt.rect;
            float nx = (r.width <= 0.0001f) ? 0f : Mathf.Clamp((_localCursor.x / (r.width * 0.5f)), -1f, 1f);
            float ny = (r.height <= 0.0001f) ? 0f : Mathf.Clamp((_localCursor.y / (r.height * 0.5f)), -1f, 1f);

            // Tilt estilo Balatro:
            // - Mover ratón a la derecha inclina hacia Y (yaw)
            // - Mover ratón arriba inclina hacia X (pitch) en sentido inverso para "mirar" al cursor
            float tiltX = -ny * maxTilt;
            float tiltY = nx * maxTilt;
            float rollZ = -nx * maxRollZ;

            targetRot = _baseRotation * Quaternion.Euler(tiltX, tiltY, rollZ);
            _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, 1f - Mathf.Exp(-tiltResponsiveness * Time.unscaledDeltaTime));
        }
        else
        {
            _currentRotation = Quaternion.Slerp(_currentRotation, _baseRotation, 1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime));
        }

        _rt.localRotation = _currentRotation;

        // Escala ligera al hover (opcional)
        Vector3 targetScale = _hovering ? _baseScale * hoverScale : _baseScale;
        _rt.localScale = Vector3.Lerp(_rt.localScale, targetScale, 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;

        // Tu comportamiento actual: actualizar panel derecho por índice
        if (_shop != null)
            _shop.OnCardHovered(_index);

        UpdateLocalCursor(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!_hovering) return;
        UpdateLocalCursor(eventData);
    }

    private void UpdateLocalCursor(PointerEventData eventData)
    {
        // Convierte posición pantalla -> punto local dentro del RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, eventData.position, uiCamera, out _localCursor);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [Header("Rotation")]
    [SerializeField] private float maxRotation = 8f;
    [SerializeField] private float smooth = 12f;

    private RectTransform rect;
    private Quaternion targetRot;

    // Shop binding
    private TiendaBotones shop;
    private int index = -1;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetRot = rect.localRotation;
    }

    /// <summary>
    /// Método para tienda: asigna a qué índice corresponde esta carta.
    /// Al hover/click refresca el panel derecho.
    /// </summary>
    public void Initialize(TiendaBotones tienda, int idx)
    {
        shop = tienda;
        index = idx;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // En tienda queremos que con hover también se actualice (opcional pero útil)
        if (shop != null && index >= 0)
            shop.OnCardHovered(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click: actualiza selección/panel (esto es lo que pedías)
        if (shop != null && index >= 0)
            shop.OnCardHovered(index);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (rect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.position, eventData.pressEventCamera, out localPoint);

        // normalizado -1..1 aprox
        Vector2 half = rect.rect.size * 0.5f;
        float nx = (half.x <= 0.0001f) ? 0f : Mathf.Clamp(localPoint.x / half.x, -1f, 1f);
        float ny = (half.y <= 0.0001f) ? 0f : Mathf.Clamp(localPoint.y / half.y, -1f, 1f);

        float rotX = -ny * maxRotation;
        float rotY = nx * maxRotation;

        targetRot = Quaternion.Euler(rotX, rotY, 0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetRot = Quaternion.identity;
    }

    private void Update()
    {
        if (rect == null) return;

        rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRot, Time.unscaledDeltaTime * smooth);
    }
}

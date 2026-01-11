using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CambiaArmasTiltAndClick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [Header("Balatro-like Tilt")]
    [SerializeField] private Camera uiCamera;
    [SerializeField] private float maxTilt = 12f;
    [SerializeField] private float maxRollZ = 4f;
    [SerializeField] private float tiltResponsiveness = 14f;
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private float hoverScale = 1.02f;
    [SerializeField] private float scaleSpeed = 12f;

    private RectTransform rt;
    private bool hovering;
    private Vector2 localCursor;
    private Quaternion baseRotation;
    private Vector3 baseScale;

    private Quaternion currentRotation;
    private Action onClick;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseRotation = rt.localRotation;
        baseScale = rt.localScale;
        currentRotation = baseRotation;
    }

    public void SetClick(Action cb) => onClick = cb;

    private void Update()
    {
        Quaternion targetRot = baseRotation;

        if (hovering)
        {
            Rect r = rt.rect;

            float nx = (r.width <= 0.0001f) ? 0f : Mathf.Clamp((localCursor.x / (r.width * 0.5f)), -1f, 1f);
            float ny = (r.height <= 0.0001f) ? 0f : Mathf.Clamp((localCursor.y / (r.height * 0.5f)), -1f, 1f);

            float tiltX = -ny * maxTilt;
            float tiltY = nx * maxTilt;
            float rollZ = -nx * maxRollZ;

            targetRot = baseRotation * Quaternion.Euler(tiltX, tiltY, rollZ);

            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRot,
                1f - Mathf.Exp(-tiltResponsiveness * Time.unscaledDeltaTime)
            );
        }
        else
        {
            currentRotation = Quaternion.Slerp(
                currentRotation,
                baseRotation,
                1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime)
            );
        }

        rt.localRotation = currentRotation;

        Vector3 targetScale = hovering ? baseScale * hoverScale : baseScale;
        rt.localScale = Vector3.Lerp(
            rt.localScale,
            targetScale,
            1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime)
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        UpdateLocalCursor(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!hovering) return;
        UpdateLocalCursor(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    private void UpdateLocalCursor(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, uiCamera, out localCursor);
    }
}

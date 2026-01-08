using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIFixVisibilityDebug : MonoBehaviour
{
    [SerializeField] private bool forceVisible = true;
    [SerializeField] private Image img;

    private void OnEnable()
    {
        if (img == null) img = GetComponent<Image>();
        Apply();
    }

    private void Update()
    {
        if (!Application.isPlaying) Apply();
    }

    private void Apply()
    {
        if (!forceVisible || img == null) return;

        img.enabled = true;

        // Fuerza alpha 1
        var c = img.color;
        c.a = 1f;
        img.color = c;

        // Si hay CanvasGroup en este GO o padres, fuerza alpha 1
        var groups = GetComponentsInParent<CanvasGroup>(true);
        foreach (var g in groups)
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }
}

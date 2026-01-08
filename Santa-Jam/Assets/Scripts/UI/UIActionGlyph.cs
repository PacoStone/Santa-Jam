using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIActionGlyph : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager input;
    [SerializeField] private Image targetImage;

    [Header("Action Name (must match Input Actions)")]
    [SerializeField] private string actionName = "ChangeGunLeft";

    [Header("Glyph Databases")]
    [SerializeField] private InputGlyphDatabase pcGlyphs;
    [SerializeField] private InputGlyphDatabase psGlyphs;
    [SerializeField] private InputGlyphDatabase xboxGlyphs;

    [Header("Behaviour")]
    [Tooltip("Si no encuentra glyph en DB, mantiene el sprite que tengas ya asignado en el editor (no lo oculta).")]
    [SerializeField] private bool keepEditorSpriteIfMissing = true;

    [Tooltip("Activa warnings en consola cuando falten glyphs.")]
    [SerializeField] private bool logMissingGlyph = true;

    private InputAction action;
    private string lastPath;
    private InputManager.GlyphSet lastSet;

    private void OnEnable()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Asegura que Image no quede deshabilitado.
        if (targetImage != null)
            targetImage.enabled = true;

        ResolveAction();

        if (input != null)
            input.OnGlyphSetChanged += HandleGlyphSetChanged;

        Refresh(force: true);
    }

    private void OnDisable()
    {
        if (input != null)
            input.OnGlyphSetChanged -= HandleGlyphSetChanged;
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void HandleGlyphSetChanged(InputManager.GlyphSet _)
    {
        Refresh(force: true);
    }

    private void ResolveAction()
    {
        if (input == null || input.PlayerInput == null) return;

        action = input.PlayerInput.actions.FindAction(actionName, throwIfNotFound: false);

        // Si no se encuentra, no rompemos; el Refresh lo gestionará.
    }

    private void Refresh(bool force)
    {
        if (input == null || input.PlayerInput == null || targetImage == null)
            return;

        if (action == null)
            ResolveAction();

        if (action == null)
        {
            // No hay acción -> no ocultes si tienes sprite de editor
            if (keepEditorSpriteIfMissing && targetImage.sprite != null)
                SetAlpha(targetImage, 1f);
            else
                SetAlpha(targetImage, 0f);

            return;
        }

        string scheme = input.PlayerInput.currentControlScheme;
        string path = FindBestBindingPath(action, scheme);

        var set = input.CurrentGlyphSet;

        if (!force && path == lastPath && set == lastSet)
            return;

        lastPath = path;
        lastSet = set;

        Sprite sprite =
            GetSprite(set, path) ??
            GetSprite(set, SimplifyToGenericGamepadPath(path));

        if (sprite != null)
        {
            targetImage.sprite = sprite;
            SetAlpha(targetImage, 1f);
            return;
        }

        if (logMissingGlyph)
        {
            Debug.LogWarning(
                $"[UIActionGlyph] Missing glyph. obj='{name}', action='{actionName}', scheme='{scheme}', path='{path}', set='{set}'",
                this);
        }

        // Fallback: no ocultar si ya hay sprite asignado en editor
        if (keepEditorSpriteIfMissing && targetImage.sprite != null)
            SetAlpha(targetImage, 1f);
        else
            SetAlpha(targetImage, 0f);
    }

    private Sprite GetSprite(InputManager.GlyphSet set, string controlPath)
    {
        if (string.IsNullOrEmpty(controlPath))
            return null;

        return set switch
        {
            InputManager.GlyphSet.PlayStation => psGlyphs ? psGlyphs.Get(controlPath) : null,
            InputManager.GlyphSet.Xbox => xboxGlyphs ? xboxGlyphs.Get(controlPath) : null,
            _ => pcGlyphs ? pcGlyphs.Get(controlPath) : null
        };
    }

    private static string FindBestBindingPath(InputAction a, string scheme)
    {
        // Preferimos bindings del control scheme actual
        for (int i = 0; i < a.bindings.Count; i++)
        {
            var b = a.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;

            if (!string.IsNullOrEmpty(b.groups) &&
                !string.IsNullOrEmpty(scheme) &&
                !b.groups.Contains(scheme))
                continue;

            if (!string.IsNullOrEmpty(b.effectivePath))
                return b.effectivePath;
        }

        // Fallback: cualquiera con effectivePath
        for (int i = 0; i < a.bindings.Count; i++)
        {
            var b = a.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (!string.IsNullOrEmpty(b.effectivePath))
                return b.effectivePath;
        }

        return null;
    }

    private static string SimplifyToGenericGamepadPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        // "<DualSenseGamepadHID>/rightShoulder" -> "<Gamepad>/rightShoulder"
        int idx = path.IndexOf(">/", System.StringComparison.Ordinal);
        if (idx <= 0) return path;

        string tail = path.Substring(idx + 1); // incluye "/..."
        return "<Gamepad>" + tail;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }
}

using System.Collections;
using UnityEngine;

public static class UITween
{
    public static IEnumerator SlideAnchored(RectTransform rt, Vector2 from, Vector2 to, float duration)
    {
        if (rt == null) yield break;

        float t = 0f;
        rt.anchoredPosition = from;

        if (duration <= 0.0001f)
        {
            rt.anchoredPosition = to;
            yield break;
        }

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float eased = EaseOutCubic(Mathf.Clamp01(t));
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        rt.anchoredPosition = to;
    }

    private static float EaseOutCubic(float x)
    {
        float a = 1f - x;
        return 1f - a * a * a;
    }
}

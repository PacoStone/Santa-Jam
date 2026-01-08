using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Input/Glyph Database")]
public class InputGlyphDatabase : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string controlPath; // ej: <Keyboard>/q  |  <Gamepad>/leftShoulder
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<string, Sprite> map;

    private void OnEnable()
    {
        map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        if (entries == null) return;

        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.controlPath) || e.sprite == null) continue;
            map[e.controlPath.Trim()] = e.sprite;
        }
    }

    public Sprite Get(string controlPath)
    {
        if (map == null) OnEnable();
        if (string.IsNullOrWhiteSpace(controlPath)) return null;
        map.TryGetValue(controlPath.Trim(), out var s);
        return s;
    }
}

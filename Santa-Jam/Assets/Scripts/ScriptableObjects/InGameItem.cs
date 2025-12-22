using UnityEngine;

public abstract class InGameItem : ScriptableObject
{
    public string ItemName;
    public Sprite Icon;
    [TextArea] public string Description;
    public ItemCategory Category;
    public Vector2 PreviewAnchorPosition;
    [Range(0,2)] public int HandsToHold;
}

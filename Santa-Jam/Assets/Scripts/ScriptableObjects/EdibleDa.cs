using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Items/Edible")]
public class EdibleDa : InGameItem
{
    [Min(0)]
    public int HealAmount = 25;

    private void OnValidate()
    {
        Category = ItemCategory.Edible;
    }
}

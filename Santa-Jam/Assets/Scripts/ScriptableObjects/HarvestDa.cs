using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Items/Harvest")]
public class HarvestDa : InGameItem
{
    [Min(1)]
    public int Amount = 1;

    private void OnValidate()
    {
        Category = ItemCategory.Harvest;
    }
}

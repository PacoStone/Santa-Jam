using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Items/Armour")]
public class ArmourDa : InGameItem
{
    public int TotalArmourPoints = 100;
    public int ArmourLossPerHit = 1;

    [Range(0f, 1f)]
    public float DamageReductionPercent = 0.3f;

    private void OnValidate()
    {
        Category = ItemCategory.Armour;
    }
}

using UnityEngine;

public class ArmourRuntime : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ArmourDa armourData;

    [Header("Runtime (solo para ver)")]
    [SerializeField] private int currentArmour;

    public void SetArmour(ArmourDa newArmour)
    {
        armourData = newArmour;

        if (armourData == null)
        {
            currentArmour = 0;
            return;
        }

        currentArmour = armourData.TotalArmourPoints;
        Debug.Log($"Armadura equipada: {armourData.ItemName} ({currentArmour})");
    }

    private void Awake()
    {
        if (armourData != null)
        {
            currentArmour = armourData.TotalArmourPoints;
        }
    }

    public int ApplyArmourToDamage(int damage)
    {
        if (armourData == null || currentArmour <= 0)
            return damage;

        int absorbed = Mathf.RoundToInt(damage * armourData.DamageReductionPercent);
        int armourCost = absorbed + armourData.ArmourLossPerHit;

        if (armourCost > currentArmour)
            armourCost = currentArmour;

        currentArmour -= armourCost;

        int finalDamage = Mathf.Max(0, damage - absorbed);
        return finalDamage;
    }
}

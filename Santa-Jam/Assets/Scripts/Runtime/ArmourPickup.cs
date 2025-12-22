using UnityEngine;

public class ArmourPickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private ArmourDa armourToGive;

    protected override bool TryApply(GameObject other)
    {
        ArmourRuntime armour = other.GetComponentInChildren<ArmourRuntime>();

        if (armour == null)
            return false;

        if (armourToGive == null)
            return false;

        armour.SetArmour(armourToGive);
        Debug.Log($"Pickup: armadura equipada -> {armourToGive.ItemName}");
        return true;
    }
}

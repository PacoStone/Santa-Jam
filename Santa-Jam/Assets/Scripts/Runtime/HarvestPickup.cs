using UnityEngine;

public class HarvestPickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private HarvestDa harvestItem;

    protected override bool TryApply(GameObject other)
    {
        InventoryRuntime inventory = other.GetComponentInChildren<InventoryRuntime>();

        if (inventory == null || harvestItem == null)
            return false;

        inventory.AddItem(harvestItem, harvestItem.Amount);
        return true;
    }
}

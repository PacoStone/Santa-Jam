using UnityEngine;

public class EdiblePickup : PickupBase
{
    [Header("Data")]
    [SerializeField] private EdibleDa edibleToConsume;

    protected override bool TryApply(GameObject other)
    {
        HealthRuntime health = other.GetComponentInChildren<HealthRuntime>();

        if (health == null || edibleToConsume == null)
            return false;

        return health.Heal(edibleToConsume.HealAmount);
    }
}

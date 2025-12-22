using UnityEngine;

public abstract class PickupBase : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private bool destroyOnPickup = true;

    protected abstract bool TryApply(GameObject other);

    private void OnTriggerEnter(Collider other)
    {
        if (!TryApply(other.gameObject))
            return;

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

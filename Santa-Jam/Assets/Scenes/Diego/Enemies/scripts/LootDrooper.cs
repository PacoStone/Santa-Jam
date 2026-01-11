using UnityEngine;

public class LootDropper : MonoBehaviour
{
    [Header("Loot")]
    public GameObject lootPrefab;
    public int minLoot = 10;
    public int maxLoot = 25;

    [Header("Fuerza")]
    public float spawnForce = 6f;
    public float upwardForce = 2f;

    void OnDestroy()
    {
        DropLoot();
    }

    void DropLoot()
    {
        if (lootPrefab == null) return;

        int amount = Random.Range(minLoot, maxLoot + 1);

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;

            GameObject loot = Instantiate(lootPrefab, spawnPos, Quaternion.identity);

            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDir = new Vector3(
                    Random.Range(-1f, 1f),
                    0.5f,
                    Random.Range(-1f, 1f)
                ).normalized;

                Vector3 force = randomDir * spawnForce + Vector3.up * upwardForce;
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}

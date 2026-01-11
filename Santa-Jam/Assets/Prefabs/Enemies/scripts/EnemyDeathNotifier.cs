using UnityEngine;

public class EnemyDeathNotifier : MonoBehaviour
{
    public EnemyWaveSpawner spawner;

    void OnDestroy()
    {
        if (spawner != null)
            spawner.OnEnemyDied();
    }
}

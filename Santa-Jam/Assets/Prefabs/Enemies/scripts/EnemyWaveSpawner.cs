using UnityEngine;
using UnityEngine.AI;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("Enemigos")]
    public GameObject[] enemyPrefabs;
    public int enemiesToSpawn = 10;

    [Header("Jugador")]
    public Transform player;

    [Header("Spawn")]
    public float spawnRadius = 30f;
    public float minDistanceFromPlayer = 10f;

    private int enemiesAlive = 0;
    private bool hasSpawned = false;

    void Start()
    {
        CharacterController pc = FindAnyObjectByType<CharacterController>();
        if (pc != null)
            player = pc.transform;
        SpawnWave();
    }

    void SpawnWave()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 pos;
            if (!TryGetPointOnNavMesh(out pos)) continue;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);

            enemiesAlive++;

            EnemyDeathNotifier notifier = enemy.AddComponent<EnemyDeathNotifier>();
            notifier.spawner = this;
        }
    }

    public void OnEnemyDied()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            Debug.Log("Oleada completada");
        }
    }

    public bool AreEnemiesAlive()
    {
        return enemiesAlive > 0;
    }


    bool TryGetPointOnNavMesh(out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * spawnRadius;
            randomDir += player.position;
            randomDir.y = player.position.y;

            if (Vector3.Distance(randomDir, player.position) < minDistanceFromPlayer)
                continue;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, 3f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}

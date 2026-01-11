using UnityEngine;
using UnityEngine.SceneManagement;

public class EndWaveTrigger : MonoBehaviour
{
    public EnemyWaveSpawner spawner;

    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<CharacterController>()) return;

        if (spawner == null)
        {
            Debug.LogWarning("No spawner assigned to EndWaveTrigger");
            return;
        }

        if (!spawner.AreEnemiesAlive())
        {
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.Log("Aún quedan enemigos vivos");
        }
    }
}

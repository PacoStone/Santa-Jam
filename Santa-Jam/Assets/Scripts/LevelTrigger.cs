using UnityEngine;

public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        hasTriggered = true;

        if (levelLoader == null)
        {
            Debug.LogError("[LevelTrigger] LevelLoader no asignado.");
            return;
        }

        // Llama exactamente a lo que tengas configurado en el inspector del LevelLoader
        levelLoader.LoadConfiguredTarget();
    }
}

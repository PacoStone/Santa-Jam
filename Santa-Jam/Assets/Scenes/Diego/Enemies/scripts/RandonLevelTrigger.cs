using UnityEngine;

public class RandomLevelTrigger : MonoBehaviour
{
    [Header("Level Loader")]
    public LevelLoader levelLoader;

    [Header("Random Target")]
    public string[] sceneNames;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (levelLoader == null)
        {
            Debug.LogWarning("[RandomLevelTrigger] No LevelLoader assigned");
            return;
        }

        if (sceneNames == null || sceneNames.Length == 0)
        {
            Debug.LogWarning("[RandomLevelTrigger] No scenes configured");
            return;
        }

        string randomScene = sceneNames[Random.Range(0, sceneNames.Length)];

        levelLoader.SetTargetSceneName(randomScene);
        levelLoader.LoadConfiguredTarget();
    }
}

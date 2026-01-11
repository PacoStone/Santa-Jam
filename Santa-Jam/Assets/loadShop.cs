using UnityEngine;
using UnityEngine.SceneManagement;

public class loadShop : MonoBehaviour
{
    [Header("Level Loader")]
    public LevelLoader levelLoader;

    [Header("Random Target")]
    public string[] sceneNames;
    public string subSceneName = "Tienda";
    public GameObject player;
    public GameObject canvas;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (levelLoader == null)
        {
            Debug.LogWarning("[RandomLevelTrigger] No LevelLoader assigned");
            return;
        }


        LoadSubScene();
    }


    public void LoadSubScene()
    {
        player.SetActive(false);
        canvas.SetActive(false);
        if (!SceneManager.GetSceneByName(subSceneName).isLoaded)
            SceneManager.LoadScene(subSceneName, LoadSceneMode.Additive);
    }

    public void UnloadSubScene()
    {
        player.SetActive(true);
        canvas.SetActive(true);
        if (SceneManager.GetSceneByName(subSceneName).isLoaded)
            SceneManager.UnloadSceneAsync(subSceneName);
    }
}

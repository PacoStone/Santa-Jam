using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public enum TargetMode
    {
        BuildIndex,
        SceneName
    }

    [Header("Transition")]
    [SerializeField] private Animator transition;
    [SerializeField] private float transitionTime = 1f;

    [Header("Default Target (Inspector)")]
    [SerializeField] private TargetMode targetMode = TargetMode.BuildIndex;

    [Tooltip("Se usa si Target Mode = BuildIndex")]
    [SerializeField] private int targetBuildIndex = 0;

    [Tooltip("Se usa si Target Mode = SceneName (nombre exacto en Build Settings)")]
    [SerializeField] private string targetSceneName = "";

    [Header("Debug / Optional")]
    [SerializeField] private KeyCode debugLoadKey = KeyCode.N;

    private void Update()
    {
        if (Input.GetKeyDown(debugLoadKey))
            LoadConfiguredTarget();
    }

    // Public API (Botones / otros scripts)

    /// Carga el destino configurado en el Inspector (targetMode + targetBuildIndex/targetSceneName).
    /// Útil para asignarlo directo a un Button OnClick().
    public void LoadConfiguredTarget()
    {
        if (targetMode == TargetMode.BuildIndex)
            LoadLevelByBuildIndex(targetBuildIndex);
        else
            LoadLevelByName(targetSceneName);
    }

    /// Para Button OnClick() pasando un int (Build Index).
    public void LoadLevelByBuildIndex(int buildIndex)
    {
        StartCoroutine(LoadLevelRoutine(buildIndex));
    }

    /// Para Button OnClick() pasando un string (Scene Name).
    public void LoadLevelByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[LevelLoader] SceneName vacío. No se puede cargar.");
            return;
        }

        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    /// Mantengo tu funcionalidad anterior por si la sigues usando.
    public void LoadNextLevel()
    {
        LoadLevelByBuildIndex(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// Permite cambiar el destino por código (por ejemplo, al hacer hover en la tienda).
    public void SetTargetBuildIndex(int buildIndex)
    {
        targetMode = TargetMode.BuildIndex;
        targetBuildIndex = buildIndex;
    }

    /// Permite cambiar el destino por código (por ejemplo, al hacer hover en la tienda).
    public void SetTargetSceneName(string sceneName)
    {
        targetMode = TargetMode.SceneName;
        targetSceneName = sceneName;
    }

    // Internals
    private IEnumerator LoadLevelRoutine(int levelIndex)
    {
        // (Opcional) trigger de animación
        if (transition != null)
            transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(levelIndex);
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        if (transition != null)
            transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiSceneLoader : MonoBehaviour
{
    [Tooltip("Загружать все сцены из Build Settings кроме текущей")]
    public bool loadAllScenes = true;

    [Tooltip("Дополнительные сцены по именам (если не хочешь грузить все)")]
    public string[] extraScenes;

    private void Start()
    {
        StartCoroutine(LoadScenesAsync());
    }

    private IEnumerator LoadScenesAsync()
    {
        // Имя активной сцены
        string activeScene = SceneManager.GetActiveScene().name;

        if (loadAllScenes)
        {
            // Загружаем все сцены из Build Settings кроме активной
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (sceneName != activeScene && !SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield return StartCoroutine(LoadSceneAdditive(sceneName));
                }
            }
        }
        else
        {
            // Загружаем только те, что перечислены вручную
            foreach (var sceneName in extraScenes)
            {
                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield return StartCoroutine(LoadSceneAdditive(sceneName));
                }
            }
        }
    }

    private IEnumerator LoadSceneAdditive(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = true; // Разрешаем активацию сразу
        while (!op.isDone)
        {
            // Можно сюда повесить прогрессбар: op.progress
            yield return null;
        }
    }

    // Кнопка выхода
    public void QuitGame()
    {
#if UNITY_EDITOR
        // В редакторе просто остановим Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // В билде закрываем приложение
        Application.Quit();
#endif
    }

    // Переключение между полноэкранным и оконным режимом
    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
}

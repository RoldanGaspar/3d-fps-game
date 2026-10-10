using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles scene loading and transitions.
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    /// <summary>0..1 progress of the scene currently loading.</summary>
    public float Progress { get; private set; }
    public bool IsLoading { get; private set; }

    public void LoadScene(string sceneName)
    {
        if (IsLoading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene '" + sceneName + "' is not in the build settings.");
            return;
        }
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        IsLoading = true;
        Progress = 0f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            Progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
        Progress = 1f;
        IsLoading = false;
    }

    public void RestartCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }
}

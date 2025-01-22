using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // For loading screen UI

public class SceneManager : MonoBehaviour
{
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private Text loadingText;
    [SerializeField] private float minimumLoadingTime = 1f; // Minimum time to show loading screen

    private static SceneManager instance;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        float timeElapsed = 0f;

        // While scene is loading
        while (!asyncLoad.isDone)
        {
            timeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Update loading bar
            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            // Update loading text
            if (loadingText != null)
            {
                loadingText.text = $"Loading... {(progress * 100):0}%";
            }

            // Wait for minimum loading time and scene to be ready
            if (progress >= 0.9f && timeElapsed >= minimumLoadingTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    // Public method to load scene with a fade effect
    public void LoadSceneWithFade(string sceneName, float fadeTime = 1f)
    {
        StartCoroutine(FadeAndLoadScene(sceneName, fadeTime));
    }

    private IEnumerator FadeAndLoadScene(string sceneName, float fadeTime)
    {
        // Fade out
        CanvasGroup canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            loadingScreen.SetActive(true);
            canvasGroup.alpha = 0;

            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.deltaTime / fadeTime;
                yield return null;
            }
        }

        // Load scene
        yield return StartCoroutine(LoadSceneAsync(sceneName));

        // Fade in
        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime / fadeTime;
                yield return null;
            }
            loadingScreen.SetActive(false);
        }
    }

    // Error handling
    public bool IsSceneValid(string sceneName)
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Scene validation failed: {e.Message}");
            return false;
        }
    }

    // Quick scene reload
    public void ReloadCurrentScene()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    // Load scene additively
    public void LoadSceneAdditive(string sceneName)
    {
        StartCoroutine(LoadSceneAdditiveAsync(sceneName));
    }

    private IEnumerator LoadSceneAdditiveAsync(string sceneName)
    {
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // Unload scene
    public void UnloadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneAsync(sceneName));
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        AsyncOperation asyncUnload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }
}
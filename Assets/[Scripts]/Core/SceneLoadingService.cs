using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Collections;

namespace Planetarium
{
    public class SceneLoadingService : SceneService
    {
        public event Action<float> OnLoadProgressChanged;
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string, string> OnLoadError;

        
        
        private bool isLoading;
        private AsyncOperation currentLoadOperation;
        private string targetSceneName;

        public bool IsLoading => isLoading;
        public float LoadProgress => currentLoadOperation?.progress ?? 0f;
        public string CurrentTargetScene => targetSceneName;

        protected override void OnInitialize()
        {
            Debug.Log("SceneLoadingService: Initialized");
        }

        public async Task<bool> LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (isLoading)
            {
                OnLoadError?.Invoke(sceneName, "Another scene is currently loading");
                return false;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                OnLoadError?.Invoke(sceneName, "Scene name cannot be empty");
                return false;
            }

            try
            {
                isLoading = true;
                targetSceneName = sceneName;
                OnSceneLoadStarted?.Invoke(sceneName);

                // Start loading the scene asynchronously
               // currentLoadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
                currentLoadOperation.allowSceneActivation = false;

                // Monitor the loading progress
                while (currentLoadOperation.progress < 0.9f)
                {
                    OnLoadProgressChanged?.Invoke(currentLoadOperation.progress);
                    await Task.Yield();
                }

                // Scene is loaded but not activated, wait for any additional setup
                OnLoadProgressChanged?.Invoke(0.9f);

                // Allow the scene to activate
                currentLoadOperation.allowSceneActivation = true;

                // Wait for the scene to fully activate
                while (!currentLoadOperation.isDone)
                {
                    await Task.Yield();
                }

                OnLoadProgressChanged?.Invoke(1f);
                OnSceneLoadCompleted?.Invoke(sceneName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading scene {sceneName}: {e.Message}");
                OnLoadError?.Invoke(sceneName, e.Message);
                return false;
            }
            finally
            {
                isLoading = false;
                currentLoadOperation = null;
                targetSceneName = null;
            }
        }

        
        
        public void CancelLoading()
        {
            if (!isLoading) return;

            // Note: Unity doesn't provide a way to cancel scene loading
            // This just cleans up our state
            isLoading = false;
            OnLoadError?.Invoke(targetSceneName, "Scene loading cancelled");
            currentLoadOperation = null;
            targetSceneName = null;
        }

        protected override void OnDeinitialize()
        {
            if (isLoading)
            {
                CancelLoading();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Threading.Tasks;

namespace Planetarium.UI.Views
{
    public class LoadingScreen : UIView
    {
        [Header("Loading Screen Elements")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private CanvasGroup contentGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;
        [SerializeField] private float progressBarSpeed = 0.5f;
        
        
        
        [Header("Text Settings")]
        [SerializeField] private string[] loadingMessages = {
            "Preparing defenses...",
            "Calibrating turrets...",
            "Scanning planetary systems...",
            "Initializing resource generators..."
        };

        private SceneLoadingService sceneLoader;
        private Sequence progressSequence;
        private bool isShowing;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            sceneLoader = Context.scene.GetService<SceneLoadingService>();
            if (sceneLoader != null)
            {
                sceneLoader.OnLoadProgressChanged += UpdateProgress;
                sceneLoader.OnSceneLoadStarted += OnSceneLoadStarted;
                sceneLoader.OnSceneLoadCompleted += OnSceneLoadCompleted;
                sceneLoader.OnLoadError += OnLoadError;
            }

            // Ensure the screen starts hidden
            if (contentGroup != null)
            {
                contentGroup.alpha = 0f;
                contentGroup.gameObject.SetActive(false);
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            if (sceneLoader != null)
            {
                sceneLoader.OnLoadProgressChanged -= UpdateProgress;
                sceneLoader.OnSceneLoadStarted -= OnSceneLoadStarted;
                sceneLoader.OnSceneLoadCompleted -= OnSceneLoadCompleted;
                sceneLoader.OnLoadError -= OnLoadError;
            }
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
            isShowing = true;

            // Stop any existing animations
            progressSequence?.Kill();
            DOTween.Kill(contentGroup);

            // Show and animate the content
            if (contentGroup != null)
            {
                contentGroup.gameObject.SetActive(true);
                if (instant)
                {
                    contentGroup.alpha = 1f;
                }
                else
                {
                    contentGroup.DOFade(1f, fadeInDuration)
                        .SetEase(fadeEase);
                }
            }

            // Start loading animation
            StartLoadingAnimation();
        }

        public override void Close(bool instant = false)
        {
            if (!isShowing) return;
            isShowing = false;

            // Stop animations
            progressSequence?.Kill();
            DOTween.Kill(contentGroup);

            if (contentGroup != null)
            {
                if (instant)
                {
                    contentGroup.alpha = 0f;
                    contentGroup.gameObject.SetActive(false);
                    base.Close(true);
                }
                else
                {
                    contentGroup.DOFade(0f, fadeOutDuration)
                        .SetEase(fadeEase)
                        .OnComplete(() => {
                            contentGroup.gameObject.SetActive(false);
                            base.Close(false);
                        });
                }
            }
            else
            {
                base.Close(instant);
            }
        }

        private void StartLoadingAnimation()
        {
            if (progressBar == null) return;

            progressSequence?.Kill();
            progressSequence = DOTween.Sequence();

            // Create a subtle loading animation
            progressSequence.Append(progressBar.DOFillAmount(0.2f, progressBarSpeed).SetEase(Ease.InOutSine))
                .Append(progressBar.DOFillAmount(0.3f, progressBarSpeed).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);

            // Cycle through loading messages
            if (loadingText != null)
            {
                InvokeRepeating(nameof(CycleLoadingMessage), 2f, 3f);
            }
        }

        private void CycleLoadingMessage()
        {
            if (!isShowing || loadingText == null) return;
            
            loadingText.text = loadingMessages[Random.Range(0, loadingMessages.Length)];
        }

        private void UpdateProgress(float progress)
        {
            if (!isShowing) return;

            // Update progress bar
            if (progressBar != null)
            {
                progressSequence?.Kill();
                progressBar.DOFillAmount(progress, 0.25f).SetEase(Ease.OutCubic);
            }

            // Update progress text
            if (progressText != null)
            {
                progressText.text = $"{Mathf.Round(progress * 100)}%";
            }
        }

        private void OnSceneLoadStarted(string sceneName)
        {
            if (loadingText != null)
            {
                loadingText.text = $"Loading {sceneName}...";
            }
        }

        private void OnSceneLoadCompleted(string sceneName)
        {
            Close(false);
        }

        private void OnLoadError(string sceneName, string error)
        {
            if (loadingText != null)
            {
                loadingText.text = $"Error loading {sceneName}: {error}";
            }
            
            // Close the loading screen after a delay
            DOVirtual.DelayedCall(3f, () => Close(false));
        }
    }
}

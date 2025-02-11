using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Planetarium.Stats;
using DG.Tweening;
using Planetarium.UI.Views;
using UnityEngine.EventSystems;

namespace Planetarium.UI
{
    public class PlanetTagGroup : UITagGroup, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Planet Tag UI")]
        [SerializeField] private Image planetIcon;
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI planetName;
        [SerializeField] private TextMeshProUGUI resourceCount;
        [SerializeField] private Button interactButton;
        
        [Header("Animation Settings")]
        [SerializeField] private float expandedScale = 1.2f;
        [SerializeField] private float expandDuration = 0.3f;
        [SerializeField] private Ease expandEase = Ease.OutBack;

        [Header("Level Loading")]
        [SerializeField] private string levelSceneName;
        private SceneLoadingService _sceneLoader;
        private UIManager _uiManager;

        private Vector3 _originalScale;
        private Tween _scaleTween;

        private void Start()
        {
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(OnInteractButtonClicked);
            }

            _originalScale = transform.localScale;
            
            // Get required services
            _sceneLoader = Context.scene.GetService<SceneLoadingService>();
            _uiManager = Context.scene.GetService<UIManager>();
        }

        public void Initialize(TaggedComponent target)
        {
            base.Initialize(target);
            _target = target;
            
            if (_target != null)
            {
                UpdatePlanetInfo();
                _target.OnTagAdded += OnTagChanged;
                _target.OnTagRemoved += OnTagChanged;
            }
        }

        private void OnTagChanged(GameplayTag tag)
        {
            UpdatePlanetInfo();
        }

        private void UpdatePlanetInfo()
        {
            if (_target == null) return;

            if (planetName != null)
            {
                planetName.text = _target.gameObject.name;
            }

            // Update status based on tags
            if (planetName != null)
            {
                string status = "";
                foreach (var tag in _target.Tags)
                {
                    status += tag.ToString() + " ";
                }
                planetName.text = status.Trim();
            }
        }

        public void SetResourceInfo(int count, Sprite resourceSprite = null)
        {
            if (resourceCount != null) resourceCount.text = count.ToString();
            if (resourceIcon != null && resourceSprite != null) resourceIcon.sprite = resourceSprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale * expandedScale, expandDuration)
                .SetEase(expandEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale, expandDuration)
                .SetEase(expandEase);
        }

        private async void OnInteractButtonClicked()
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogWarning($"No level scene name specified for planet tag {gameObject.name}");
                return;
            }

            if (_sceneLoader == null || _uiManager == null)
            {
                Debug.LogError($"Required services not found in {gameObject.name}");
                return;
            }

            // Show loading screen
            var loadingScreen = _uiManager.GetView<LoadingScreen>();
            if (loadingScreen != null)
            {
                loadingScreen.Open();
            }

            // Load the level
            bool success = await _sceneLoader.LoadSceneAsync(levelSceneName);
            
            if (!success && loadingScreen != null)
            {
                loadingScreen.Close();
            }
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();

            if (_target != null)
            {
                _target.OnTagAdded -= OnTagChanged;
                _target.OnTagRemoved -= OnTagChanged;
            }

            if (interactButton != null)
            {
                interactButton.onClick.RemoveListener(OnInteractButtonClicked);
            }
        }
    }
}

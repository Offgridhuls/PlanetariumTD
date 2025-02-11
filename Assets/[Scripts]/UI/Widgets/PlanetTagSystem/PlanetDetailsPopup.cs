using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using Planetarium.Stats;

namespace Planetarium.UI
{
    public class PlanetDetailsPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI planetNameText;
        [SerializeField] private TextMeshProUGUI resourceText;
        [SerializeField] private Image planetImage;
        [SerializeField] private Transform contentContainer;
        
        [Header("Animation Settings")]
        [SerializeField] private float openDuration = 0.4f;
        [SerializeField] private float closeDuration = 0.3f;
        [SerializeField] private Ease openEase = Ease.OutBack;
        [SerializeField] private Ease closeEase = Ease.InQuint;
        [SerializeField] private float openScale = 1.1f;
        
        private Action _onClose;
        private Vector2 _originalSize;
        private TaggedComponent _planet;

        private void Awake()
        {
            if (popupPanel == null) popupPanel = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            _originalSize = popupPanel.sizeDelta;
            
            // Setup initial state
            canvasGroup.alpha = 0f;
            popupPanel.localScale = Vector3.zero;
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Initialize(TaggedComponent planet, Action onClose)
        {
            _planet = planet;
            _onClose = onClose;
            
            if (_planet != null)
            {
                UpdateUI();
                _planet.OnTagAdded += OnPlanetUpdated;
                _planet.OnTagRemoved += OnPlanetUpdated;
            }
            
            // Animate open
            DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, openDuration))
                .Join(popupPanel.DOScale(openScale, openDuration).SetEase(openEase))
                .Append(popupPanel.DOScale(1f, openDuration * 0.5f).SetEase(Ease.OutBack))
                .SetLink(gameObject);
        }

        private void UpdateUI()
        {
            if (_planet == null) return;

            if (planetNameText != null)
            {
                planetNameText.text = _planet.gameObject.name;
            }

            // Update other UI elements based on planet data
            // Add your custom UI updates here
        }

        private void OnPlanetUpdated(GameplayTag tag)
        {
            UpdateUI();
        }

        public void Close()
        {
            // Animate close
            DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, closeDuration))
                .Join(popupPanel.DOScale(0f, closeDuration).SetEase(closeEase))
                .OnComplete(() => 
                {
                    _onClose?.Invoke();
                    Destroy(gameObject);
                })
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            if (_planet != null)
            {
                _planet.OnTagAdded -= OnPlanetUpdated;
                _planet.OnTagRemoved -= OnPlanetUpdated;
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }
    }
}

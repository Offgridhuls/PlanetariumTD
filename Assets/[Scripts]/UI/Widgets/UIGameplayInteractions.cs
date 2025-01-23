using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Planetarium.UI
{
    public class UIGameplayInteractions : UIBehaviour
    {
        [Header("UI Groups")]
        [SerializeField] private UIBehaviour _playerInfoGroup;
        [SerializeField] private CanvasGroup _resourcesGroup;

        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI _baseHealthText;
        [SerializeField] private TextMeshProUGUI _waveText;

        [Header("Animation")]
        [SerializeField] private float _updateAnimationDuration = 0.5f;
        [SerializeField] private Ease _updateEaseType = Ease.OutBack;
        [SerializeField] private float _punchScale = 1.2f;
        
        private GameStateManager _gameState;
        private int _displayedBaseHealth;
        private int _displayedWave;
        
        // Store tweens to kill them when needed
        private Tween _baseHealthTween;
        private Tween _waveTween;

        private void Start()
        {
            _gameState = FindFirstObjectByType<GameStateManager>();

            if (_gameState != null)
            {
                // Subscribe to events
                _gameState.OnWaveChanged += UpdateWaveDisplay;

                // Initial update
                UpdateAllDisplays();
            }
            
            // Initialize resource group if present
            if (_resourcesGroup != null)
            {
                _resourcesGroup.alpha = 0f;
                _resourcesGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                // Unsubscribe from events
                _gameState.OnWaveChanged -= UpdateWaveDisplay;
            }
            
            // Kill all active tweens
            _baseHealthTween?.Kill();
            _waveTween?.Kill();
        }

        private void UpdateAllDisplays()
        {
            if (_gameState == null) return;

            UpdateWaveDisplay(_gameState.CurrentWave);
            
            // Initial base health display without animation
            if (_baseHealthText != null)
            {
                /*_displayedBaseHealth = _gameState.BaseHealth;
                _baseHealthText.text = $" {displayedBaseHealth}";*/
            }
        }

        public void UpdateBaseHealthDisplay(int newValue)
        {
            if (_baseHealthText == null) return;
            
            // Kill any existing tween
            _baseHealthTween?.Kill();
            
            // Animate the number
            _baseHealthTween = DOTween.To(() => _displayedBaseHealth, x => 
            {
                _displayedBaseHealth = x;
                _baseHealthText.text = $" {x}";
            }, newValue, _updateAnimationDuration)
                .SetEase(_updateEaseType);
            
            // Add shake effect if health decreased
            if (newValue < _displayedBaseHealth)
            {
                _baseHealthText.transform
                    .DOShakePosition(_updateAnimationDuration, 10f, 20, 90f)
                    .SetEase(_updateEaseType);
                
                // Flash red
                Color originalColor = _baseHealthText.color;
                _baseHealthText.DOColor(Color.red, _updateAnimationDuration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad);
            }
        }

        private void UpdateWaveDisplay(int newValue)
        {
            if (_waveText == null) return;
            
            // Kill any existing tween
            _waveTween?.Kill();
            
            _displayedWave = newValue;
            _waveText.text = $"Wave {newValue}";
            
            // Animate wave number change
            _waveText.transform.DOScale(Vector3.one * _punchScale, _updateAnimationDuration * 0.5f)
                .SetEase(Ease.OutBack)
                .SetLoops(2, LoopType.Yoyo);
        }

        private void UpdatePlayerIndicator()
        {
            if (_playerInfoGroup != null && !_playerInfoGroup.gameObject.activeSelf)
            {
                _playerInfoGroup.gameObject.SetActive(true);
                _playerInfoGroup.transform.DOScale(Vector3.one, 0.5f)
                    .From(Vector3.zero)
                    .SetEase(Ease.OutBack);
            }
        }
    }
}
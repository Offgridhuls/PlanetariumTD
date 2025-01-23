using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Planetarium.UI
{
    public class CurrencyView : UIView
    {
        [SerializeField] private TextMeshProUGUI currencyText;

        [Header("Animation")]
        [SerializeField] private float updateAnimationDuration = 0.5f;
        [SerializeField] private Ease updateEaseType = Ease.OutBack;
        [SerializeField] private float punchScale = 1.2f;

        private GameStateManager gameState;
        private int displayedCurrency;
        private Tween currencyTween;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            Debug.Log($"CurrencyView: OnInitialize called on {gameObject.name}");
            gameState = Context.GameState;

            if (gameState != null)
            {
                Debug.Log($"CurrencyView: Found GameState, subscribing to events on {gameObject.name}");
                gameState.OnCurrencyChanged += UpdateCurrencyDisplay;
                UpdateCurrencyDisplay(gameState.GetCurrency());
            }
            else
            {
                Debug.LogError($"CurrencyView: GameState is null on {gameObject.name}!");
            }

            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (currencyText == null)
            {
                Debug.LogError($"CurrencyView: currencyText is not assigned on {gameObject.name}!");
            }
        }

        protected override void OnDeinitialize()
        {
            Debug.Log($"CurrencyView: OnDeinitialize called on {gameObject.name}");
            base.OnDeinitialize();
            
            if (gameState != null)
            {
                Debug.Log($"CurrencyView: Unsubscribing from events on {gameObject.name}");
                gameState.OnCurrencyChanged -= UpdateCurrencyDisplay;
            }

            // Kill active tween
            if (currencyTween != null && currencyTween.IsActive())
            {
                currencyTween.Kill();
                currencyTween = null;
            }
        }

        private void UpdateCurrencyDisplay(int newCurrency)
        {
            Debug.Log($"CurrencyView: UpdateCurrencyDisplay called with currency: {newCurrency} on {gameObject.name}");
            if (currencyText == null)
            {
                Debug.LogError($"CurrencyView: currencyText is null during update on {gameObject.name}!");
                return;
            }

            // Kill any active animation
            if (currencyTween != null && currencyTween.IsActive())
            {
                currencyTween.Kill();
            }

            // Animate the currency text
            if (gameObject.activeInHierarchy)
            {
                currencyText.transform.localScale = Vector3.one;
                currencyTween = DOTween.Sequence()
                    .Append(currencyText.transform.DOScale(punchScale, updateAnimationDuration * 0.5f))
                    .Append(currencyText.transform.DOScale(1f, updateAnimationDuration * 0.5f))
                    .SetEase(updateEaseType);
            }

            displayedCurrency = newCurrency;
            currencyText.text = newCurrency.ToString();
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            
            // Update the display when becoming visible
            if (gameState != null)
            {
                UpdateCurrencyDisplay(gameState.GetCurrency());
            }
        }

        public override void Open(bool instant = false)
        {
            Debug.Log($"CurrencyView: Open called on {gameObject.name} (instant: {instant})");
            base.Open(instant);

            if (!instant && gameObject.activeInHierarchy)
            {
                // Fade in using the base class's CanvasGroup
                CanvasGroup.alpha = 0f;
                DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 1f, 0.5f)
                    .SetEase(Ease.OutQuad);
            }
        }

        public override void Close(bool instant = false)
        {
            Debug.Log($"CurrencyView: Close called on {gameObject.name} (instant: {instant})");
            if (instant)
            {
                base.Close(true);
            }
            else
            {
                // Fade out using the base class's CanvasGroup
                DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, 0f, 0.5f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => base.Close(true));
            }
        }
    }
}

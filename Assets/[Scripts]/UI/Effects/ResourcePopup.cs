using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace Planetarium
{
    public class ResourcePopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image resourceIcon;

        [Header("Animation Settings")]
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private float screenPadding = 20f; // Padding from screen edges
        
        private RectTransform rectTransform;
        private Canvas parentCanvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        private Vector2 GetSafeTargetPosition(Vector2 targetPos, Vector2 elementSize, Vector2 screenSize)
        {
            // Add padding to keep elements away from the edges
            float minX = screenPadding;
            float minY = screenPadding;
            float maxX = screenSize.x - elementSize.x - screenPadding;
            float maxY = screenSize.y - elementSize.y - screenPadding;

            // Clamp the position within safe bounds
            return new Vector2(
                Mathf.Clamp(targetPos.x, minX, maxX),
                Mathf.Clamp(targetPos.y, minY, maxY)
            );
        }

        public void Initialize(ResourceType resource, int amount, Vector2 startPosition, float targetX, float targetY)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            // Set initial position
            rectTransform.anchoredPosition = startPosition;
            
            // Set resource info
            if (amountText != null)
            {
                amountText.text = $"+{amount}";
            }
            if (resourceIcon != null && resource != null)
            {
                resourceIcon.sprite = resource.icon;
                resourceIcon.color = resource.resourceColor;
            }

            // Calculate target position based on screen size
            Vector2 targetPos = Vector2.zero;
            if (parentCanvas != null)
            {
                RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                Vector2 screenSize = new Vector2(canvasRect.rect.width, canvasRect.rect.height);
                Vector2 elementSize = rectTransform.rect.size;
                
                // Calculate raw target position
                targetPos = new Vector2(
                    screenSize.x * targetX,
                    screenSize.y * targetY
                );

                // Adjust position to stay within safe bounds
                targetPos = GetSafeTargetPosition(targetPos, elementSize, screenSize);
            }

            // Move to target position with InQuint easing (starts very slow, ends fast)
            rectTransform.DOAnchorPos(targetPos, moveDuration)
                .SetEase(Ease.InQuint)
                    .OnComplete(() => Destroy(gameObject));
        }

        public void DestroyPopup()
        {
            Destroy(gameObject);
        }
    }
}

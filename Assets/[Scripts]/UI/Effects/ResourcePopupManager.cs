using UnityEngine;
using System;
using Planetarium.Resources.Types;

namespace Planetarium
{
    [Serializable]
    public struct ResourcePopupTarget
    {
        public ResourceType resourceType;
        [Range(0f, 1f)] public float targetPositionX;
        [Range(0f, 1f)] public float targetPositionY;
    }

    public class ResourcePopupManager : SceneService
    {
        [Header("Popup Settings")]
        [SerializeField] private ResourcePopup popupPrefab;
        [SerializeField] private Canvas targetCanvas;
        
        [Header("Resource Targets")]
        [SerializeField] private ResourcePopupTarget[] resourceTargets;
        [SerializeField] private float defaultTargetX = 0.9f;
        [SerializeField] private float defaultTargetY = 0.9f;

        public void ShowResourcePopup(ResourceType resource, int amount, Vector2 screenPosition)
        {
            if (resource == null || amount <= 0) return;

            // Create popup
            var popup = Instantiate(popupPrefab, targetCanvas.transform);
            
            // Convert screen position to canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetCanvas.GetComponent<RectTransform>(),
                screenPosition,
                targetCanvas.worldCamera,
                out Vector2 localPoint
            );

            // Find target position for this resource type
            float targetX = defaultTargetX;
            float targetY = defaultTargetY;
            
            if (resourceTargets != null)
            {
                foreach (var target in resourceTargets)
                {
                    if (target.resourceType == resource)
                    {
                        targetX = target.targetPositionX;
                        targetY = target.targetPositionY;
                        break;
                    }
                }
            }

            // Initialize and start animation
            popup.Initialize(resource, amount, localPoint, targetX, targetY);
            // The popup will destroy itself after animation completes
        }
    }
}

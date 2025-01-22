using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium.UI
{
    public class InventoryView : UIView
    {
        [Header("Inventory References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private TextMeshProUGUI resourceCountText;
        
        private ResourceManager resourceManager;
        
        public override void Initialize(UIManager manager)
        {
            base.Initialize(manager);
            resourceManager = FindFirstObjectByType<ResourceManager>();
            UpdateInventoryDisplay();
        }
        
      
        
        private void UpdateInventoryDisplay()
        {
            if (resourceManager == null) return;
            
            // Example of updating resource display
            // You'll want to expand this based on your resource types
            string resourceText = "";
            foreach (var resourceType in FindObjectsByType<ResourceType>(FindObjectsSortMode.None))
            {
                int count = resourceManager.GetResourceCount(resourceType);
                resourceText += $"{resourceType.resourceName}: {count}\n";
            }
            
            if (resourceCountText != null)
            {
                resourceCountText.text = resourceText;
            }
        }
    }
}

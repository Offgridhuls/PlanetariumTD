using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium.UI
{
    public class ExpansionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI ownedNodeCountText;
        [SerializeField] private TextMeshProUGUI availableNodeCountText;
        [SerializeField] private TextMeshProUGUI expansionCostText;
        [SerializeField] private GameObject nodeInfoPanel;
        [SerializeField] private TextMeshProUGUI selectedNodeStateText;
        
        [Header("Expansion Button")]
        [SerializeField] private Button expandButton;
        [SerializeField] private TextMeshProUGUI expandButtonText;
        
        private PlanetaryExpansionService expansionService;
        private GameStateManager gameStateManager;
        
        private void Start()
        {
            // Find required services
            expansionService = FindFirstObjectByType<PlanetaryExpansionService>();
            gameStateManager = FindFirstObjectByType<GameStateManager>();
            
            if (expansionService == null)
            {
                Debug.LogError("ExpansionUI: Could not find PlanetaryExpansionService!");
                gameObject.SetActive(false);
                return;
            }
            
            // Subscribe to events
            expansionService.OnNodeClaimed += HandleNodeClaimed;
            expansionService.OnExpansionCostChanged += HandleExpansionCostChanged;
            
            if (expandButton != null)
            {
                expandButton.onClick.AddListener(ExpandTerritory);
            }
            
            // Hide node info panel initially
            if (nodeInfoPanel != null)
            {
                nodeInfoPanel.SetActive(false);
            }
            
            // Initialize UI
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (expansionService != null)
            {
                expansionService.OnNodeClaimed -= HandleNodeClaimed;
                expansionService.OnExpansionCostChanged -= HandleExpansionCostChanged;
            }
            
            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ExpandTerritory);
            }
        }
        
        private void Update()
        {
            // Continuously update UI to reflect current state
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (expansionService == null)
                return;
                
            // Update node counts
            if (ownedNodeCountText != null)
            {
                ownedNodeCountText.text = $"Owned Territories: {expansionService.OwnedNodeCount}";
            }
            
            if (availableNodeCountText != null)
            {
                availableNodeCountText.text = $"Available Territories: {expansionService.AvailableNodeCount}";
            }
            
            // Update expansion cost
            if (expansionCostText != null)
            {
                expansionCostText.text = $"Expansion Cost: {expansionService.CurrentExpansionCost}";
            }
            
            // Update expand button interactability
            if (expandButton != null)
            {
                bool canAfford = gameStateManager != null && 
                    gameStateManager.Currency >= expansionService.CurrentExpansionCost;
                bool hasAvailableNodes = expansionService.AvailableNodeCount > 0;
                
                expandButton.interactable = canAfford && hasAvailableNodes;
                
                if (expandButtonText != null)
                {
                    if (!hasAvailableNodes)
                    {
                        expandButtonText.text = "No Available Territories";
                    }
                    else if (!canAfford)
                    {
                        expandButtonText.text = $"Need {expansionService.CurrentExpansionCost} Credits";
                    }
                    else
                    {
                        expandButtonText.text = $"Expand Territory ({expansionService.CurrentExpansionCost})";
                    }
                }
            }
        }
        
        public void ShowNodeInfo(PlanetaryNode node)
        {
            if (node == null || nodeInfoPanel == null)
                return;
                
            nodeInfoPanel.SetActive(true);
            
            if (selectedNodeStateText != null)
            {
                selectedNodeStateText.text = $"Node State: {node.State}";
            }
        }
        
        public void HideNodeInfo()
        {
            if (nodeInfoPanel != null)
            {
                nodeInfoPanel.SetActive(false);
            }
        }
        
        private void ExpandTerritory()
        {
            if (expansionService == null)
                return;
                
            // Find an available node to expand to
            var availableNodes = expansionService.GetAvailableNodes();
            if (availableNodes.Count > 0)
            {
                // For now, just expand to the first available node
                expansionService.TryClaimNode(availableNodes[0]);
            }
        }
        
        private void HandleNodeClaimed(PlanetaryNode node)
        {
            // Update UI after claiming a node
            UpdateUI();
        }
        
        private void HandleExpansionCostChanged(int newCost)
        {
            // Update UI after cost changes
            UpdateUI();
        }
    }
} 
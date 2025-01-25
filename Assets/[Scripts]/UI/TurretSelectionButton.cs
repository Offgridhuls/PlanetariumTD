using Planetarium.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium
{
    [RequireComponent(typeof(Button))]
    public class TurretSelectionButton : UIBehaviour
    {
        [Header("Turret Configuration")]
        [SerializeField] private DeployableBase turretPrefab;
        [SerializeField] private bool isCancelButton;
        
        [Header("UI Components")]
        [SerializeField] private Button button;
        [SerializeField] private Image turretIcon;
        [SerializeField] private Image backgroundImage;
        
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color defaultBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color unaffordableBackgroundColor = new Color(0.3f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color lockedBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color cancelButtonColor = new Color(0.7f, 0.3f, 0.3f, 1f);
        
        private TurretPlacementService placementService;
        private GameStateManager gameState;
        private TurretStats turretStats;
        private bool isSelected;
        private bool isInitialized;
        private bool isUnlocked;

        public void Initialize()
        {
            if (isInitialized) return;

            // Get required components
            if (button == null) button = GetComponent<Button>();
            placementService = FindFirstObjectByType<TurretPlacementService>();
            gameState = FindFirstObjectByType<GameStateManager>();

            // Get turret stats
            if (turretPrefab != null)
            {
                turretStats = turretPrefab.M_TurretStats;
                if (turretStats == null)
                {
                    Debug.LogError($"TurretSelectionButton: No TurretStats found on {turretPrefab.name}");
                    return;
                }
            }

            // Setup UI
            SetupUIComponents();

            // Setup service connections
            if (placementService != null)
            {
                placementService.OnTurretSelectionChanged += HandleTurretSelectionChanged;
            }
            
            if (!isCancelButton && gameState != null)
            {
                gameState.OnCurrencyChanged += UpdateButtonState;
                UpdateButtonState(gameState.Currency);
            }

            isInitialized = true;
        }

        public void Cleanup()
        {
            if (!isInitialized) return;

            // Cleanup service connections
            if (gameState != null && !isCancelButton)
            {
                gameState.OnCurrencyChanged -= UpdateButtonState;
            }
            
            if (placementService != null)
            {
                placementService.OnTurretSelectionChanged -= HandleTurretSelectionChanged;
            }
            
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }

            isInitialized = false;
        }

        private void SetupUIComponents()
        {
            button.onClick.AddListener(OnButtonClicked);
            
            if (isCancelButton)
            {
                if (backgroundImage != null) backgroundImage.color = cancelButtonColor;
                if (nameText != null) nameText.text = "Cancel";
                if (descriptionText != null) descriptionText.text = "Cancel turret placement";
                if (costText != null) costText.gameObject.SetActive(false);
                if (statsText != null) statsText.gameObject.SetActive(false);
                return;
            }
            
            if (turretStats != null)
            {
                // Set text components from TurretStats
                if (nameText != null) nameText.text = turretStats.GetName();
                if (descriptionText != null) descriptionText.text = turretStats.GetDescription();
                if (statsText != null) statsText.text = turretStats.GetStatsDescription();
                
                // Set icon from TurretStats
                if (turretIcon != null)
                {
                    turretIcon.sprite = turretStats.GetIcon();
                    turretIcon.gameObject.SetActive(turretIcon.sprite != null);
                }

                // Set initial unlock state
                isUnlocked = turretStats.IsUnlockedByDefault();
            }
            
            // Set initial background color
            UpdateVisuals(false, true);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void HandleTurretSelectionChanged(DeployableBase selectedTurret)
        {
            if (!isCancelButton)
            {
                isSelected = (turretPrefab == selectedTurret);
                UpdateVisuals(isSelected, button.interactable);
            }
        }

        private void OnButtonClicked()
        {
            if (!isUnlocked) return;

            if (placementService != null)
            {
                if (isCancelButton)
                {
                    placementService.CancelTurretPlacement();
                }
                else
                {
                    if (isSelected)
                    {
                        placementService.CancelTurretPlacement();
                    }
                    else
                    {
                        placementService.SelectTurret(turretPrefab);
                    }
                }
            }
        }

        private void UpdateCostText()
        {
            if (costText != null && turretStats != null)
            {
                int cost = turretStats.GetCoinCost();
                costText.text = $"{cost} <sprite=0>"; // Assuming coin sprite is index 0 in TMP sprite asset
            }
        }

        private void UpdateButtonState(int currentCurrency)
        {
            if (turretStats != null)
            {
                bool canAfford = currentCurrency >= turretStats.GetCoinCost();
                button.interactable = canAfford && isUnlocked;
                UpdateVisuals(isSelected, canAfford);
            }
        }

        public void RefreshDisplay()
        {
            if (!isInitialized) return;
            
            UpdateCostText();
            if (!isCancelButton && gameState != null)
            {
                UpdateButtonState(gameState.Currency);
            }
        }

        private void UpdateVisuals(bool selected, bool affordable)
        {
            if (backgroundImage != null)
            {
                if (isCancelButton)
                {
                    backgroundImage.color = cancelButtonColor;
                }
                else if (!isUnlocked)
                {
                    backgroundImage.color = lockedBackgroundColor;
                }
                else
                {
                    Color targetColor = affordable 
                        ? (selected ? selectedBackgroundColor : defaultBackgroundColor)
                        : unaffordableBackgroundColor;
                    
                    backgroundImage.color = targetColor;
                }
            }

            // Update text colors based on state
            Color textColor = (affordable && isUnlocked) ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            if (nameText != null) nameText.color = textColor;
            if (costText != null) costText.color = textColor;
            if (descriptionText != null) descriptionText.color = textColor;
            if (statsText != null) statsText.color = textColor;
        }
    }
}

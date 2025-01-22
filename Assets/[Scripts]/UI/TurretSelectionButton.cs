using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium
{
    [RequireComponent(typeof(Button))]
    public class TurretSelectionButton : MonoBehaviour
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
        
        [Header("Button Text")]
        [SerializeField] private string turretName = "Basic Turret";
        [SerializeField, TextArea(2, 4)] private string turretDescription = "A basic defensive turret";
        
        [Header("Visual Settings")]
        [SerializeField] private Color defaultBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color unaffordableBackgroundColor = new Color(0.3f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color cancelButtonColor = new Color(0.7f, 0.3f, 0.3f, 1f);
        
        private TurretPlacementService placementService;
        private GameStateManager gameState;
        private bool isSelected;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            SetupUIComponents();
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
                return;
            }
            
            // Set text components if available
            if (nameText != null) nameText.text = turretName;
            if (descriptionText != null) descriptionText.text = turretDescription;
            
            // Set icon if available
            if (turretIcon != null && turretPrefab != null)
            {
                var turretIconProvider = turretPrefab.GetComponent<SpriteRenderer>();
                if (turretIconProvider != null && turretIconProvider.sprite != null)
                {
                    turretIcon.sprite = turretIconProvider.sprite;
                }
            }
            
            // Set initial background color
            UpdateVisuals(false, true);
        }

        private void Start()
        {
            placementService = FindFirstObjectByType<TurretPlacementService>();
            gameState = GameStateManager.Instance;
            
            if (placementService != null)
            {
                placementService.OnTurretSelectionChanged += HandleTurretSelectionChanged;
            }
            
            if (!isCancelButton)
            {
                if (costText != null && turretPrefab != null)
                {
                    UpdateCostText();
                }

                if (gameState != null)
                {
                    gameState.OnCurrencyChanged += UpdateButtonState;
                    UpdateButtonState(gameState.Currency);
                }
            }
        }

        private void OnDestroy()
        {
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
            if (costText != null && turretPrefab != null)
            {
                int cost = turretPrefab.M_TurretStats.GetCoinCost();
                costText.text = $"{cost} <sprite=0>"; // Assuming coin sprite is index 0 in TMP sprite asset
            }
        }

        private void UpdateButtonState(int currentCurrency)
        {
            if (turretPrefab != null)
            {
                bool canAfford = currentCurrency >= turretPrefab.M_TurretStats.GetCoinCost();
                button.interactable = canAfford;
                UpdateVisuals(isSelected, canAfford);
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
                else
                {
                    Color targetColor = affordable 
                        ? (selected ? selectedBackgroundColor : defaultBackgroundColor)
                        : unaffordableBackgroundColor;
                    
                    backgroundImage.color = targetColor;
                }
            }

            // Update text colors based on state
            Color textColor = affordable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            if (nameText != null) nameText.color = textColor;
            if (costText != null) costText.color = textColor;
            if (descriptionText != null) descriptionText.color = textColor;
        }
    }
}

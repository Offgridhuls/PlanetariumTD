using Planetarium.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using LootLocker.Requests;

public class ProfileView : UIView
{
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Turret Cards")]
    [SerializeField] private Transform turretCardContainer;
    [SerializeField] private GameObject turretCardPrefab;

    [Header("UI Controls")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button refreshButton;
    
    private List<GameObject> _instantiatedCards = new List<GameObject>();

    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Set up button listeners
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                UIManager.TransitionToView<MainMenuView>(this);
            });
        }
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() => {
                RefreshProfileData();
            });
        }
    }

    public override void Open(bool instant = false)
    {
        base.Open(instant);
        
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Load profile data when opening the view
        RefreshProfileData();
    }
    
    public override void Close(bool instant = false)
    {
        base.Close(instant);
        
        // Clear the turret cards
        ClearTurretCards();
    }
    
    private void RefreshProfileData()
    {
        // Get player name
        LootLockerSDKManager.GetPlayerName((response) => {
            if (response.success)
            {
                UpdatePlayerName(response.name);
            }
            else
            {
                Debug.LogWarning("Failed to get player name: " + response.errorData);
                UpdatePlayerName("Player");
            }
        });
        
        // Get currency (Oil)
        LootLockerSDKManager.GetBalance((response) => {
            if (response.success)
            {
                int oilAmount = response.balance ?? 0;
                UpdateCurrency(oilAmount);
            }
            else
            {
                Debug.LogWarning("Failed to get currency: " + response.errorData);
                UpdateCurrency(0);
            }
        });
        
        // Get turret cards
        GetTurretCards();
    }
    
    private void UpdatePlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }
    
    private void UpdateCurrency(int amount)
    {
        if (currencyText != null)
        {
            currencyText.text = $"Oil: {amount}";
        }
    }
    
    private void GetTurretCards()
    {
        // For simplicity, we'll create some sample turret data
        // In a real application, you would get this from LootLocker inventory
        List<TurretCardData> sampleTurrets = new List<TurretCardData>()
        {
            new TurretCardData() {
                Name = "Basic Turret",
                Description = "Standard defensive turret with balanced stats.",
                Damage = 15,
                FireRate = 1.5f
            },
            new TurretCardData() {
                Name = "Rapid Fire",
                Description = "Low damage but extremely fast firing rate.",
                Damage = 5,
                FireRate = 5.0f
            },
            new TurretCardData() {
                Name = "Heavy Cannon",
                Description = "High damage with slow firing rate.",
                Damage = 50,
                FireRate = 0.5f
            }
        };
        
        DisplayTurretCards(sampleTurrets);
        
        // Note: In a real application, you would use LootLockerSDKManager.GetInventory to get the player's inventory
        // and filter for turret items, then convert them to TurretCardData objects
    }
    
    private void DisplayTurretCards(List<TurretCardData> turretCards)
    {
        ClearTurretCards();
        
        if (turretCardContainer == null || turretCardPrefab == null)
        {
            Debug.LogWarning("Turret card container or prefab not assigned!");
            return;
        }
        
        foreach (var turretData in turretCards)
        {
            GameObject cardObject = Instantiate(turretCardPrefab, turretCardContainer);
            TurretCardDisplay cardDisplay = cardObject.GetComponent<TurretCardDisplay>();
            
            if (cardDisplay != null)
            {
                cardDisplay.SetupCard(turretData);
            }
            
            _instantiatedCards.Add(cardObject);
        }
    }
    
    private void ClearTurretCards()
    {
        foreach (var card in _instantiatedCards)
        {
            Destroy(card);
        }
        _instantiatedCards.Clear();
    }
}
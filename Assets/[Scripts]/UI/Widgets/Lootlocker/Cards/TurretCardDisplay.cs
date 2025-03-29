using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium.UI
{
    public class TurretCardDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI fireRateText;
        
        public void SetupCard(TurretCardData cardData)
        {
            if (nameText != null)
                nameText.text = cardData.Name;
                
            if (descriptionText != null)
                descriptionText.text = cardData.Description;
                
            if (damageText != null)
                damageText.text = $"Damage: {cardData.Damage}";
                
            if (fireRateText != null)
                fireRateText.text = $"Fire Rate: {cardData.FireRate}/sec";
        }
    }
}
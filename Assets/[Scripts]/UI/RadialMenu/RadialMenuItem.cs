using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Planetarium.Deployables
{
    public class RadialMenuItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private float highlightScale = 1.2f;
        
        private DeployableBase deployablePrefab;
        private System.Action onSelected;
        private bool isHighlighted;
        private Vector3 originalScale;
        
        private void Awake()
        {
            originalScale = transform.localScale;
        }
        
        public void Initialize(DeployableBase prefab, Sprite icon, string label)
        {
            deployablePrefab = prefab;
            
            if (iconImage != null && icon != null)
                iconImage.sprite = icon;
                
            if (labelText != null)
                labelText.text = label;
                
            SetHighlighted(false);
        }
        
        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            
            isHighlighted = highlighted;
            if (backgroundImage != null)
                backgroundImage.color = highlighted ? highlightedColor : normalColor;
                
            transform.localScale = highlighted ? originalScale * highlightScale : originalScale;
        }
        
        public void Select()
        {
            onSelected?.Invoke();
        }
        
        public void SetSelectCallback(System.Action callback)
        {
            onSelected = callback;
        }
        
        public DeployableBase GetDeployablePrefab()
        {
            return deployablePrefab;
        }
    }
}

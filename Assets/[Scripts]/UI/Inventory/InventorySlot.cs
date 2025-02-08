using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Planetarium
{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image selectedOverlay;

        public event System.Action<int> OnSlotClicked;
        public bool HasItem => Resource != null;
        public ResourceType Resource { get; private set; }
        private int slotIndex;

        private void Awake()
        {
            Clear();
        }

        public void Initialize(int index)
        {
            slotIndex = index;
            Clear();
        }

        public void SetResource(ResourceType resource, int count)
        {
            if (resource == null)
            {
                Clear();
                return;
            }

            Resource = resource;
            if (iconImage != null)
            {
                iconImage.sprite = resource.icon;
                iconImage.enabled = true;
            }

            if (countText != null)
            {
                countText.text = count > 0 ? count.ToString() : string.Empty;
                countText.enabled = count > 0;
            }
            if(nameText != null)
            {
                nameText.text = Resource.resourceName;
                nameText.enabled = true;
            }
        }

        public void Clear()
        {
            Resource = null;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
            {
                countText.text = string.Empty;
                countText.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = string.Empty;
                nameText.enabled = false;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedOverlay != null)
            {
                selectedOverlay.enabled = selected;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(slotIndex);
        }

        private void OnDestroy()
        {
            OnSlotClicked = null;
        }
    }
}

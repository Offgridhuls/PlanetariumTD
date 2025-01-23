using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Planetarium.UI
{
    public class InventoryView : UIView
    {
        [Header("Inventory References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private InventorySlot slotPrefab;
        [SerializeField] private int maxSlots = 8;

        private ResourceInventory inventory;
        private List<InventorySlot> slots = new List<InventorySlot>();
        private Dictionary<ResourceType, InventorySlot> itemSlots = new Dictionary<ResourceType, InventorySlot>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            inventory = Context.ResourceInventory;

            InitializeSlots();

            if (inventory != null)
            {
                inventory.OnInventoryChanged += UpdateInventory;
                inventory.OnItemSelected += OnItemSelected;
                UpdateInventory(inventory.GetInventory());
            }
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= UpdateInventory;
                inventory.OnItemSelected -= OnItemSelected;
            }

            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            slots.Clear();
            itemSlots.Clear();
        }

        private void InitializeSlots()
        {
            if (itemContainer == null || slotPrefab == null) return;

            // Clear existing slots
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            slots.Clear();
            itemSlots.Clear();

            // Create new slots
            for (int i = 0; i < maxSlots; i++)
            {
                var slotGO = Instantiate(slotPrefab.gameObject, itemContainer);
                var slot = slotGO.GetComponent<InventorySlot>();
                slot.Initialize(i);
                slot.OnSlotClicked += OnSlotClicked;
                slots.Add(slot);
            }
        }

        private void UpdateInventory(Dictionary<ResourceType, int> items)
        {
            // Reset all slots
            foreach (var slot in slots)
            {
                slot.Clear();
            }
            itemSlots.Clear();

            // Populate slots with items
            int index = 0;
            foreach (var item in items)
            {
                if (index >= slots.Count) break;

                slots[index].SetResource(item.Key, item.Value);
                itemSlots[item.Key] = slots[index];
                index++;
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < slots.Count)
            {
                var slot = slots[slotIndex];
                if (slot.HasItem)
                {
                    PlayClickSound();
                    inventory.SelectItem(slot.Resource);
                }
            }
        }

        private void OnItemSelected(ResourceType resource)
        {
            foreach (var slot in slots)
            {
                slot.SetSelected(slot.Resource == resource);
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium
{
    public class ResourceInventory : SceneService
    {
        public event Action<Dictionary<ResourceType, int>> OnInventoryChanged;
        public event Action<ResourceType> OnItemSelected;

        [Header("Initial Resources")]
        [SerializeField] private ResourceType[] initialResources;
        [SerializeField] private int[] initialAmounts;

        private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
        private ResourceType selectedResource;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // Initialize with empty inventory
            resources.Clear();
            selectedResource = null;

            // Add initial resources if configured
            if (initialResources != null && initialAmounts != null)
            {
                int count = Mathf.Min(initialResources.Length, initialAmounts.Length);
                for (int i = 0; i < count; i++)
                {
                    if (initialResources[i] != null && initialAmounts[i] > 0)
                    {
                        AddResource(initialResources[i], initialAmounts[i]);
                    }
                }
            }
        }

        protected override void OnDeinitialize()
        {
            OnInventoryChanged = null;
            OnItemSelected = null;
        }

        public void AddResource(ResourceType resource, int amount)
        {
            if (resource == null || amount <= 0) return;

            if (!resources.ContainsKey(resource))
            {
                resources[resource] = 0;
            }
            resources[resource] += amount;
            OnInventoryChanged?.Invoke(GetInventory());
        }

        public bool TrySpendResource(ResourceType resource, int amount)
        {
            if (resource == null || amount <= 0) return false;
            if (!resources.ContainsKey(resource) || resources[resource] < amount) return false;

            resources[resource] -= amount;
            if (resources[resource] <= 0)
            {
                resources.Remove(resource);
            }
            OnInventoryChanged?.Invoke(GetInventory());
            return true;
        }

        public void SelectItem(ResourceType resource)
        {
            selectedResource = resource;
            OnItemSelected?.Invoke(resource);
        }

        public Dictionary<ResourceType, int> GetInventory()
        {
            return new Dictionary<ResourceType, int>(resources);
        }

        public int GetResourceCount(ResourceType resource)
        {
            return resources.TryGetValue(resource, out int count) ? count : 0;
        }

        public ResourceType GetSelectedResource()
        {
            return selectedResource;
        }
    }
}

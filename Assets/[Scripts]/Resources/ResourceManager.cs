using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Planetarium
{
    public class ResourceManager : SceneService
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceType[] availableResources;
        [SerializeField] private float globalGravitationMultiplier = 1f;
        [SerializeField] private int maxResourcesPerType = 1000;
        
        // Events
        public UnityEvent<ResourceType, int> onGlobalResourceCollected = new UnityEvent<ResourceType, int>();
        
        // Resource tracking
        private Dictionary<ResourceType, int> globalResourceCounts = new Dictionary<ResourceType, int>();
        private Dictionary<ResourceType, ObjectPool<ResourcePickup>> resourcePools = new Dictionary<ResourceType, ObjectPool<ResourcePickup>>();
        private Dictionary<ResourceType, Transform> resourceParents = new Dictionary<ResourceType, Transform>();
        private Transform poolRoot;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Create pool root
            GameObject rootObj = new GameObject("Resource_Pools");
            rootObj.transform.SetParent(transform);
            poolRoot = rootObj.transform;
            
            // Initialize resource counts and pools
            if (availableResources != null)
            {
                foreach (var resource in availableResources)
                {
                    globalResourceCounts[resource] = 0;
                    CreateResourceParent(resource);
                    InitializePoolForType(resource);
                }
            }
        }

        private void CreateResourceParent(ResourceType type)
        {
            if (type == null) return;

            GameObject parentObj = new GameObject($"Pool_{type.resourceName}");
            parentObj.transform.SetParent(poolRoot);
            resourceParents[type] = parentObj.transform;
        }

        private void InitializePoolForType(ResourceType type)
        {
            if (type?.pickupPrefab == null) return;

            Transform parentTransform = resourceParents[type];
            
            resourcePools[type] = new ObjectPool<ResourcePickup>(
                createFunc: () => 
                {
                    GameObject obj = Instantiate(type.pickupPrefab, Vector3.zero, Quaternion.identity, parentTransform);
                    ResourcePickup pickup = obj.GetComponent<ResourcePickup>();
                    pickup.resourceType = type;
                    return pickup;
                },
                actionOnGet: (pickup) => 
                {
                    pickup.gameObject.SetActive(true);
                    pickup.Initialize(Context, this);
                },
                actionOnRelease: (pickup) => 
                {
                    pickup.transform.SetParent(parentTransform); // Ensure it's under the correct parent
                    pickup.gameObject.SetActive(false);
                },
                actionOnDestroy: (pickup) => 
                {
                    if (pickup != null)
                        Destroy(pickup.gameObject);
                },
                defaultCapacity: 20,
                maxSize: maxResourcesPerType
            );
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            // Clear all resources and dispose pools
            globalResourceCounts.Clear();
            foreach (var pool in resourcePools.Values)
            {
                pool.Clear();
            }
            resourcePools.Clear();
            resourceParents.Clear();

            // Destroy pool root
            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
                poolRoot = null;
            }
        }

        public ResourcePickup SpawnResource(ResourceType type, Vector3 position, int amount = 1)
        {
            if (type == null) return null;

            ObjectPool<ResourcePickup> pool;
            if (!resourcePools.TryGetValue(type, out pool))
            {
                CreateResourceParent(type);
                InitializePoolForType(type);
                pool = resourcePools[type];
            }

            ResourcePickup pickup = pool.Get();
            if (pickup != null)
            {
                pickup.transform.position = position;
                pickup.amount = amount;
            }

            return pickup;
        }

        public void ReleaseResource(ResourcePickup pickup)
        {
            if (pickup == null || pickup.resourceType == null) return;

            ObjectPool<ResourcePickup> pool;
            if (resourcePools.TryGetValue(pickup.resourceType, out pool))
            {
                pool.Release(pickup);
            }
        }

        public void CollectResource(ResourceType type, int amount)
        {
            if (!globalResourceCounts.ContainsKey(type))
            {
                globalResourceCounts[type] = 0;
            }

            globalResourceCounts[type] += amount;
            onGlobalResourceCollected.Invoke(type, globalResourceCounts[type]);
        }

        public int GetResourceCount(ResourceType type)
        {
            return globalResourceCounts.ContainsKey(type) ? globalResourceCounts[type] : 0;
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (globalResourceCounts.ContainsKey(type) && globalResourceCounts[type] >= amount)
            {
                globalResourceCounts[type] -= amount;
                onGlobalResourceCollected.Invoke(type, globalResourceCounts[type]);
                return true;
            }
            return false;
        }

        public float GetGravitationMultiplier()
        {
            return globalGravitationMultiplier;
        }
    }
}

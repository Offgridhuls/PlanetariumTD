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
            
            // Pre-instantiate the pool with inactive objects
            List<ResourcePickup> preWarmList = new List<ResourcePickup>();
            for (int i = 0; i < 20; i++) // Pre-warm with default capacity
            {
                GameObject obj = Instantiate(type.pickupPrefab, Vector3.zero, Quaternion.identity, parentTransform);
                ResourcePickup pickup = obj.GetComponent<ResourcePickup>();
                pickup.resourceType = type;
                pickup.gameObject.SetActive(false);
                preWarmList.Add(pickup);
            }
            
            resourcePools[type] = new ObjectPool<ResourcePickup>(
                createFunc: () => 
                {
                    GameObject obj = Instantiate(type.pickupPrefab, Vector3.zero, Quaternion.identity, parentTransform);
                    ResourcePickup pickup = obj.GetComponent<ResourcePickup>();
                    pickup.resourceType = type;
                    obj.SetActive(false); // Ensure object starts inactive
                    return pickup;
                },
                actionOnGet: (pickup) => 
                {
                    if (pickup != null)
                    {
                        pickup.gameObject.SetActive(true);
                        pickup.Initialize(Context, this);
                        pickup.ResetState();
                    }
                },
                actionOnRelease: (pickup) => 
                {
                    if (pickup != null && pickup.gameObject != null)
                    {
                        pickup.gameObject.SetActive(false); // Ensure object is deactivated
                        pickup.transform.SetParent(parentTransform);
                        pickup.StopAllCoroutines();
                    }
                },
                actionOnDestroy: (pickup) => 
                {
                    if (pickup != null && pickup.gameObject != null)
                    {
                        Destroy(pickup.gameObject);
                    }
                },
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: maxResourcesPerType
            );

            // Add pre-warmed objects to the pool
            foreach (var pickup in preWarmList)
            {
                resourcePools[type].Release(pickup);
            }
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

            // Get or create pool
            if (!resourcePools.TryGetValue(type, out ObjectPool<ResourcePickup> pool))
            {
                CreateResourceParent(type);
                InitializePoolForType(type);
                pool = resourcePools[type];
            }

            // Get resource from pool
            ResourcePickup pickup = null;
            try
            {
                pickup = pool.Get();
                if (pickup != null)
                {
                    pickup.transform.position = position;
                    pickup.amount = amount;
                }
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning($"Pool for resource type {type.resourceName} is at capacity. Consider increasing maxResourcesPerType.");
            }

            return pickup;
        }

        public void ReleaseResource(ResourcePickup pickup)
        {
            if (pickup == null || pickup.resourceType == null) return;

            if (resourcePools.TryGetValue(pickup.resourceType, out ObjectPool<ResourcePickup> pool))
            {
                try
                {
                    pool.Release(pickup);
                }
                catch (System.InvalidOperationException e)
                {
                    Debug.LogError($"Failed to release resource to pool: {e.Message}");
                    Destroy(pickup.gameObject); // Fallback: destroy the object if we can't pool it
                }
            }
            else
            {
                Destroy(pickup.gameObject); // Fallback: destroy the object if no pool exists
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

        /// <summary>
        /// Clears all active resources from the game world and resets resource state
        /// </summary>
        public void ClearAllResources()
        {
            try
            {
                // Clear active resource pickups
                if (resourcePools != null)
                {
                    foreach (var pool in resourcePools.Values)
                    {
                        if (pool != null)
                        {
                            pool.Clear();
                        }
                    }
                }

                // Reset resource counts
                globalResourceCounts.Clear();
                foreach (var resourceType in System.Enum.GetValues(typeof(ResourceType)))
                {
                    globalResourceCounts[(ResourceType)resourceType] = 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error clearing resources: {e.Message}");
            }
        }
    }
}

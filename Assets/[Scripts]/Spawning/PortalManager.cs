using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium.Spawning
{
    public class PortalManager : SceneService
    {
        [Header("Distribution Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private float usageDecayRate = 0.2f;
        
        // Dictionary of portal ID to list of portals (for multiple portals with same ID)
        private Dictionary<string, List<SpawnPortal>> portalGroups = new Dictionary<string, List<SpawnPortal>>();
        [SerializeField]
        private List<SpawnPortal> activePortals = new List<SpawnPortal>();

        // Track spawn counts for each portal to ensure even distribution
        private Dictionary<SpawnPortal, int> portalSpawnCounts = new Dictionary<SpawnPortal, int>();
        private Dictionary<SpawnPortal, float> portalUsageRatios = new Dictionary<SpawnPortal, float>();
        private float lastUpdateTime;

        protected override void OnTick()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdatePortalUsage();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdatePortalUsage()
        {
            if (activePortals.Count == 0) return;

            // Find max spawn count
            int maxSpawns = portalSpawnCounts.Count > 0 ? portalSpawnCounts.Values.Max() : 0;
            if (maxSpawns == 0) return;

            // Update usage ratios and apply decay
            foreach (var portal in activePortals)
            {
                if (!portalUsageRatios.ContainsKey(portal))
                {
                    portalUsageRatios[portal] = 0f;
                }

                float currentRatio = portalSpawnCounts[portal] / (float)maxSpawns;
                portalUsageRatios[portal] = Mathf.Lerp(
                    portalUsageRatios[portal],
                    currentRatio,
                    1f - Mathf.Pow(usageDecayRate, Time.deltaTime)
                );

               // portal.UpdateUsageVisualization(portalUsageRatios[portal]);
            }
        }

        public SpawnPortal GetPortalForEnemy(EnemySpawnData enemyType)
        {
            // Get all portal groups that can spawn this enemy type
            var availableGroups = portalGroups
                .Where(group => group.Value.Any(p => p.IsActive && p.CanSpawnType(enemyType)))
                .ToList();

            if (availableGroups.Count == 0) return null;

            // Randomly select a portal group
            var selectedGroup = availableGroups[Random.Range(0, availableGroups.Count)];
            
            // Get active portals from this group that can spawn this enemy
            var availablePortals = selectedGroup.Value
                .Where(p => p.IsActive && p.CanSpawnType(enemyType))
                .ToList();

            if (availablePortals.Count == 0) return null;

            // Find portal with lowest spawn count in this group
            var minSpawnCount = availablePortals.Min(p => portalSpawnCounts[p]);
            var leastUsedPortals = availablePortals
                .Where(p => portalSpawnCounts[p] == minSpawnCount)
                .ToList();

            // Select portal based on group distribution pattern
            var selectedPortal = SelectPortalFromGroup(leastUsedPortals);
            portalSpawnCounts[selectedPortal]++;
            selectedPortal.OnSpawn();
            
            return selectedPortal;
        }

        private SpawnPortal SelectPortalFromGroup(List<SpawnPortal> portals)
        {
            if (portals.Count <= 1) return portals[0];

            // Calculate spawn position based on portal arrangement
            float normalizedIndex = (float)portals[0].SpawnCount / (portals.Sum(p => p.SpawnCount) + 1);
            
            // Find portal that creates best distribution
            return portals.OrderBy(p => {
                var spawnPos = p.GetSpawnPosition(normalizedIndex);
                var otherSpawns = portals.Where(op => op != p)
                    .Select(op => op.GetSpawnPosition())
                    .ToList();
                
                // Calculate average distance to other spawn points
                float avgDistance = otherSpawns.Count > 0 
                    ? otherSpawns.Average(pos => Vector3.Distance(spawnPos, pos))
                    : 0f;
                    
                return -avgDistance; // Negative to get maximum distance
            }).First();
        }

        public void ActivatePortals(List<string> portalIds = null)
        {
            if (portalIds == null || portalIds.Count == 0)
            {
                foreach (var group in portalGroups.Values)
                {
                    foreach (var portal in group)
                    {
                        ActivatePortal(portal);
                    }
                }
            }
            else
            {
                foreach (var id in portalIds)
                {
                    if (portalGroups.TryGetValue(id, out List<SpawnPortal> portals))
                    {
                        foreach (var portal in portals)
                        {
                            ActivatePortal(portal);
                        }
                    }
                }
            }
        }

        private void ActivatePortal(SpawnPortal portal)
        {
            portal.Activate();
            if (!activePortals.Contains(portal))
            {
                activePortals.Add(portal);
                portalSpawnCounts[portal] = 0;
                portalUsageRatios[portal] = 0f;
            }
        }

        public void DeactivatePortals(List<string> portalIds = null)
        {
            if (portalIds == null || portalIds.Count == 0)
            {
                foreach (var portal in activePortals.ToList())
                {
                    DeactivatePortal(portal);
                }
                activePortals.Clear();
            }
            else
            {
                foreach (var id in portalIds)
                {
                    if (portalGroups.TryGetValue(id, out List<SpawnPortal> portals))
                    {
                        foreach (var portal in portals)
                        {
                            DeactivatePortal(portal);
                        }
                    }
                }
            }
        }

        private void DeactivatePortal(SpawnPortal portal)
        {
            portal.Deactivate();
            activePortals.Remove(portal);
            portalUsageRatios.Remove(portal);
        }

        protected override void OnInitialize()
        {
            // Find all portals in the scene
            var scenePortals = FindObjectsOfType<SpawnPortal>();
            foreach (var portal in scenePortals)
            {
                RegisterPortal(portal);
            }
        }

        public void RegisterPortal(SpawnPortal portal)
        {
            if (!portalGroups.ContainsKey(portal.PortalId))
            {
                portalGroups[portal.PortalId] = new List<SpawnPortal>();
            }
            
            if (!portalGroups[portal.PortalId].Contains(portal))
            {
                portalGroups[portal.PortalId].Add(portal);
                portalSpawnCounts[portal] = 0;
            }
        }

        public void UnregisterPortal(SpawnPortal portal)
        {
            if (portalGroups.ContainsKey(portal.PortalId))
            {
                portalGroups[portal.PortalId].Remove(portal);
                if (portalGroups[portal.PortalId].Count == 0)
                {
                    portalGroups.Remove(portal.PortalId);
                }
            }
            
            portalSpawnCounts.Remove(portal);
            activePortals.Remove(portal);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.Stats
{
    public enum ResourceKind
    {
        Coins,
        Gems
    }

    public enum DamageType
    {
        Normal
    }

    [CreateAssetMenu(fileName = "New Wave Stat", menuName = "Planetarium/Stats/Game/Wave Stat")]
    public class WaveStat : StatBase
    {
        [System.Serializable]
        public struct WaveData
        {
            public int currentWave;
            public int enemiesRemaining;
            public float timeUntilNextWave;
            public bool isFinalWave;
        }

        public WaveData defaultValue;
        public override object GetDefaultValue() => defaultValue;
        public override string FormatValue(object value)
        {
            var data = (WaveData)value;
            if (data.isFinalWave)
                return "Final Wave!";
            return string.Format(format, data.currentWave, data.enemiesRemaining, data.timeUntilNextWave);
        }
    }

    [CreateAssetMenu(fileName = "New Enemy Stat", menuName = "Planetarium/Stats/Game/Enemy Stat")]
    public class EnemyStat : StatBase
    {
        [System.Serializable]
        public struct EnemyData
        {
            public int totalKilled;
            public int totalSpawned;
            public float totalDamageDealt;    // Damage dealt TO enemies
            public float totalDamageTaken;    // Damage taken FROM enemies
            public Dictionary<string, int> enemyTypeKills; // Kills per enemy type
        }

        public EnemyData defaultValue;
        public override object GetDefaultValue() => defaultValue;
        public override string FormatValue(object value)
        {
            var data = (EnemyData)value;
            return string.Format(format, data.totalKilled, data.totalSpawned, 
                data.totalDamageDealt, data.totalDamageTaken);
        }
    }

    [CreateAssetMenu(fileName = "New Tower Stat", menuName = "Planetarium/Stats/Game/Tower Stat")]
    public class TowerStat : StatBase
    {
        [System.Serializable]
        public struct TowerData
        {
            public int towersBuilt;
            public int towersUpgraded;
            public int towersSold;
            public float totalDamageDealt;
            public Dictionary<string, int> towerTypeCount; // Count per tower type
        }

        public TowerData defaultValue;
        public override object GetDefaultValue() => defaultValue;
        public override string FormatValue(object value)
        {
            var data = (TowerData)value;
            return string.Format(format, data.towersBuilt, data.towersUpgraded, 
                data.towersSold, data.totalDamageDealt);
        }
    }

    [CreateAssetMenu(fileName = "New Resource Stat", menuName = "Planetarium/Stats/Game/Resource Stat")]
    public class ResourceStat : StatBase
    {
        [System.Serializable]
        public struct ResourceData
        {
            public int currentResources;
            public int totalResourcesCollected;
            public int totalResourcesSpent;
            public float resourceMultiplier;
        }

        public ResourceData defaultValue;
        public override object GetDefaultValue() => defaultValue;
        public override string FormatValue(object value)
        {
            var data = (ResourceData)value;
            return string.Format(format, data.currentResources, data.totalResourcesCollected, 
                data.totalResourcesSpent, data.resourceMultiplier);
        }
    }

    [CreateAssetMenu(fileName = "New Turret Type Stat", menuName = "Planetarium/Stats/Game/Turret Type Stat")]
    public class TurretTypeStats : StatBase
    {
        [System.Serializable]
        public struct TurretStats
        {
            public int totalBuilt;
            public int totalDestroyed;
            public float totalDamageDealt;
            public int totalKills;
            public float resourcesSpent;
            public float accuracy;
            public int shotsFired;
            public int shotsHit;
        }

        [System.Serializable]
        public class TurretTypeData
        {
            public Dictionary<string, TurretStats> turretStats = new Dictionary<string, TurretStats>();

            public TurretTypeData()
            {
                turretStats = new Dictionary<string, TurretStats>();
            }
        }

        public TurretTypeData defaultValue = new TurretTypeData();
        
        public override object GetDefaultValue() => defaultValue;
        
        public override string FormatValue(object value)
        {
            var data = (TurretTypeData)value;
            if (data == null || data.turretStats == null || data.turretStats.Count == 0)
                return "No turret data";
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var kvp in data.turretStats)
            {
                sb.AppendFormat("Type: {0}\n", kvp.Key);
                sb.AppendFormat("Built: {0}, Kills: {1}\n", kvp.Value.totalBuilt, kvp.Value.totalKills);
                sb.AppendFormat("Damage: {0:F0}, Accuracy: {1:P0}\n\n", kvp.Value.totalDamageDealt, kvp.Value.accuracy);
            }
            return sb.ToString();
        }
    }

    [CreateAssetMenu(fileName = "New Enemy Type Stat", menuName = "Planetarium/Stats/Game/Enemy Type Stat")]
    public class EnemyTypeStats : StatBase
    {
        [System.Serializable]
        public struct EnemyStats
        {
            public int totalSpawned;
            public int totalKilled;
            public float totalDamageDealt;
            public float totalDamageTaken;
            public float averageLifetime;
            public int resourcesDropped;
            public float furthestProgress; // 0-1 progress through path
        }

        [System.Serializable]
        public class EnemyTypeData
        {
            public Dictionary<string, EnemyStats> enemyStats = new Dictionary<string, EnemyStats>();
        }

        public EnemyTypeData defaultValue = new EnemyTypeData();
        
        public override object GetDefaultValue() => defaultValue;
        
        public override string FormatValue(object value)
        {
            var data = (EnemyTypeData)value;
            if (string.IsNullOrEmpty(format)) return "No enemy data";
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var kvp in data.enemyStats)
            {
                sb.AppendFormat("Type: {0}\n", kvp.Key);
                sb.AppendFormat("Spawned: {0}, Killed: {1}\n", kvp.Value.totalSpawned, kvp.Value.totalKilled);
                sb.AppendFormat("Damage Dealt: {0:F0}, Taken: {1:F0}\n\n", kvp.Value.totalDamageDealt, kvp.Value.totalDamageTaken);
            }
            return sb.ToString();
        }
    }
}

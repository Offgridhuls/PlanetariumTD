using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.Stats
{
    /// <summary>
    /// Helper class to easily access game-specific stats
    /// </summary>
    public static class GameStatsHelper
    {
        // Stat IDs - these should match the names of your stat assets
        public static class IDs
        {
            public const string WaveStats = "WaveStats";
            public const string EnemyStats = "EnemyStats";
            public const string TowerStats = "TowerStats";
            public const string ResourceStats = "ResourceStats";
            public const string GameTime = "GameTime";
            public const string PlayerHealth = "PlayerHealth";
            public const string TurretTypeStats = "TurretTypeStats";
            public const string EnemyTypeStats = "EnemyTypeStats";
        }

        #region Wave Stats
        public static void UpdateWaveStats(int currentWave, int enemiesRemaining, float timeUntilNext, bool isFinal = false)
        {
            StatManager.Instance.SetValue(IDs.WaveStats, new WaveStat.WaveData
            {
                currentWave = currentWave,
                enemiesRemaining = enemiesRemaining,
                timeUntilNextWave = timeUntilNext,
                isFinalWave = isFinal
            });
        }

        public static WaveStat.WaveData GetWaveStats()
        {
            return StatManager.Instance.GetValue<WaveStat.WaveData>(IDs.WaveStats);
        }
        #endregion

        #region Enemy Stats
        public static void OnEnemySpawned(EnemySpawnData enemyData)
        {
            if (enemyData == null) return;

            var data = StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
            if (data == null)
            {
                data = new EnemyTypeStats.EnemyTypeData();
                data.enemyStats = new Dictionary<string, EnemyTypeStats.EnemyStats>();
            }
            
            if (!data.enemyStats.ContainsKey(enemyData.name))
            {
                data.enemyStats[enemyData.name] = new EnemyTypeStats.EnemyStats();
            }

            var stats = data.enemyStats[enemyData.name];
            stats.totalSpawned++;
            data.enemyStats[enemyData.name] = stats;
            
            StatManager.Instance.SetValue(IDs.EnemyTypeStats, data);
            
            // Also update general enemy stats
            EnemySpawned();
        }

        public static void OnEnemyKilled(string enemyName, float maxHealth, float lifetime, float progress)
        {
            if (string.IsNullOrEmpty(enemyName)) return;

            var data = StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
            if (data == null)
            {
                data = new EnemyTypeStats.EnemyTypeData();
                data.enemyStats = new Dictionary<string, EnemyTypeStats.EnemyStats>();
            }

            if (!data.enemyStats.ContainsKey(enemyName))
            {
                data.enemyStats[enemyName] = new EnemyTypeStats.EnemyStats();
            }

            var stats = data.enemyStats[enemyName];
            stats.totalKilled++;
            stats.averageLifetime = ((stats.averageLifetime * (stats.totalKilled - 1)) + lifetime) / stats.totalKilled;
            stats.furthestProgress = Mathf.Max(stats.furthestProgress, progress);
            data.enemyStats[enemyName] = stats;

            StatManager.Instance.SetValue(IDs.EnemyTypeStats, data);
            
            // Update general enemy stats
            EnemyKilled(enemyName, maxHealth);
        }

        public static void OnEnemyDamageDealt(string enemyName, float damage)
        {
            if (string.IsNullOrEmpty(enemyName)) return;

            var data = StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
            if (data == null)
            {
                data = new EnemyTypeStats.EnemyTypeData();
                data.enemyStats = new Dictionary<string, EnemyTypeStats.EnemyStats>();
            }

            if (!data.enemyStats.ContainsKey(enemyName))
            {
                data.enemyStats[enemyName] = new EnemyTypeStats.EnemyStats();
            }

            var stats = data.enemyStats[enemyName];
            stats.totalDamageDealt += damage;
            data.enemyStats[enemyName] = stats;

            StatManager.Instance.SetValue(IDs.EnemyTypeStats, data);
        }

        public static void OnEnemyDamageTaken(string enemyName, float damage)
        {
            if (string.IsNullOrEmpty(enemyName)) return;

            var data = StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
            if (data == null)
            {
                data = new EnemyTypeStats.EnemyTypeData();
                data.enemyStats = new Dictionary<string, EnemyTypeStats.EnemyStats>();
            }

            if (!data.enemyStats.ContainsKey(enemyName))
            {
                data.enemyStats[enemyName] = new EnemyTypeStats.EnemyStats();
            }

            var stats = data.enemyStats[enemyName];
            stats.totalDamageTaken += damage;
            data.enemyStats[enemyName] = stats;

            StatManager.Instance.SetValue(IDs.EnemyTypeStats, data);
        }

        public static void OnEnemyDroppedResources(string enemyName, int amount)
        {
            if (string.IsNullOrEmpty(enemyName)) return;

            var data = StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
            if (data == null)
            {
                data = new EnemyTypeStats.EnemyTypeData();
                data.enemyStats = new Dictionary<string, EnemyTypeStats.EnemyStats>();
            }

            if (!data.enemyStats.ContainsKey(enemyName))
            {
                data.enemyStats[enemyName] = new EnemyTypeStats.EnemyStats();
            }

            var stats = data.enemyStats[enemyName];
            stats.resourcesDropped += amount;
            data.enemyStats[enemyName] = stats;

            StatManager.Instance.SetValue(IDs.EnemyTypeStats, data);
        }

        public static void EnemyKilled(string enemyType, float damageDealt)
        {
            var stats = GetEnemyStats();
            if (stats.enemyTypeKills == null)
            {
                stats.enemyTypeKills = new Dictionary<string, int>();
            }

            stats.totalKilled++;
            stats.totalDamageDealt += damageDealt;
            if (!stats.enemyTypeKills.ContainsKey(enemyType))
                stats.enemyTypeKills[enemyType] = 0;
            stats.enemyTypeKills[enemyType]++;
            
            StatManager.Instance.SetValue(IDs.EnemyStats, stats);
        }

        public static void EnemySpawned()
        {
            ModifyEnemyStats(stats => {
                stats.totalSpawned++;
                return stats;
            });
        }

        public static void TakeDamageFromEnemy(float damage)
        {
            ModifyEnemyStats(stats => {
                stats.totalDamageTaken += damage;
                return stats;
            });
        }

        public static void OnStructureDamaged(float damage)
        {
            ModifyEnemyStats(stats => {
                stats.totalStructureDamage += damage;
                return stats;
            });
        }

        private static void ModifyEnemyStats(System.Func<EnemyStat.EnemyData, EnemyStat.EnemyData> modifier)
        {
            StatManager.Instance.ModifyValue(IDs.EnemyStats, modifier);
        }

        public static EnemyStat.EnemyData GetEnemyStats()
        {
            return StatManager.Instance.GetValue<EnemyStat.EnemyData>(IDs.EnemyStats);
        }
        #endregion

        #region Tower Stats
        public static void TowerBuilt(string towerType)
        {
            var stats = GetTowerStats();
            stats.towersBuilt++;
            if (!stats.towerTypeCount.ContainsKey(towerType))
                stats.towerTypeCount[towerType] = 0;
            stats.towerTypeCount[towerType]++;
            
            StatManager.Instance.SetValue(IDs.TowerStats, stats);
        }

        public static void TowerUpgraded()
        {
            ModifyTowerStats(stats => {
                stats.towersUpgraded++;
                return stats;
            });
        }

        public static void TowerSold()
        {
            ModifyTowerStats(stats => {
                stats.towersSold++;
                return stats;
            });
        }

        public static void AddTowerDamage(float damage)
        {
            ModifyTowerStats(stats => {
                stats.totalDamageDealt += damage;
                return stats;
            });
        }

        private static void ModifyTowerStats(System.Func<TowerStat.TowerData, TowerStat.TowerData> modifier)
        {
            StatManager.Instance.ModifyValue(IDs.TowerStats, modifier);
        }

        public static TowerStat.TowerData GetTowerStats()
        {
            return StatManager.Instance.GetValue<TowerStat.TowerData>(IDs.TowerStats);
        }
        #endregion

        #region Resource Stats
        public static void AddResources(int amount)
        {
            ModifyResourceStats(stats => {
                stats.currentResources += amount;
                stats.totalResourcesCollected += amount;
                return stats;
            });
        }

        public static void SpendResources(int amount)
        {
            ModifyResourceStats(stats => {
                stats.currentResources -= amount;
                stats.totalResourcesSpent += amount;
                return stats;
            });
        }

        public static void SetResourceMultiplier(float multiplier)
        {
            ModifyResourceStats(stats => {
                stats.resourceMultiplier = multiplier;
                return stats;
            });
        }

        private static void ModifyResourceStats(System.Func<ResourceStat.ResourceData, ResourceStat.ResourceData> modifier)
        {
            StatManager.Instance.ModifyValue(IDs.ResourceStats, modifier);
        }

        public static ResourceStat.ResourceData GetResourceStats()
        {
            return StatManager.Instance.GetValue<ResourceStat.ResourceData>(IDs.ResourceStats);
        }
        #endregion

        #region Simple Stats
        public static void UpdateGameTime(float time)
        {
            StatManager.Instance.SetFloat(IDs.GameTime, time);
        }

        public static void SetPlayerHealth(float health)
        {
            StatManager.Instance.SetFloat(IDs.PlayerHealth, health);
        }

        public static float GetPlayerHealth()
        {
            return StatManager.Instance.GetFloat(IDs.PlayerHealth);
        }
        #endregion

        #region Turret Type Stats
        public static void OnTurretShotFired(string turretName)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.shotsFired++;
            // Recalculate accuracy
            stats.accuracy = stats.shotsHit / (float)stats.shotsFired;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static void OnTurretShotHit(string turretName)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.shotsHit++;
            // Recalculate accuracy
            stats.accuracy = stats.shotsHit / (float)stats.shotsFired;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static void OnTurretBuilt(string turretName, float cost)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.totalBuilt++;
            stats.resourcesSpent += cost;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static void OnTurretDestroyed(string turretName)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.totalDestroyed++;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static void OnTurretDamageDealt(string turretName, float damage)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.totalDamageDealt += damage;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static void OnTurretKill(string turretName)
        {
            if (string.IsNullOrEmpty(turretName)) return;

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null)
            {
                data = new TurretTypeStats.TurretTypeData();
            }

            if (!data.turretStats.ContainsKey(turretName))
            {
                data.turretStats[turretName] = new TurretTypeStats.TurretStats();
            }

            var stats = data.turretStats[turretName];
            stats.totalKills++;
            data.turretStats[turretName] = stats;

            StatManager.Instance.SetValue(IDs.TurretTypeStats, data);
        }

        public static TurretTypeStats.TurretStats GetTurretStats(string turretName)
        {
            if (string.IsNullOrEmpty(turretName)) return new TurretTypeStats.TurretStats();

            var data = StatManager.Instance.GetValue<TurretTypeStats.TurretTypeData>(IDs.TurretTypeStats);
            if (data == null || !data.turretStats.ContainsKey(turretName))
            {
                return new TurretTypeStats.TurretStats();
            }

            return data.turretStats[turretName];
        }
        #endregion

        #region Enemy Type Stats
        private static void ModifyEnemyTypeStats(System.Func<EnemyTypeStats.EnemyTypeData, EnemyTypeStats.EnemyTypeData> modifier)
        {
            StatManager.Instance.ModifyValue(IDs.EnemyTypeStats, modifier);
        }

        public static EnemyTypeStats.EnemyTypeData GetEnemyTypeStats()
        {
            return StatManager.Instance.GetValue<EnemyTypeStats.EnemyTypeData>(IDs.EnemyTypeStats);
        }
        #endregion
    }
}

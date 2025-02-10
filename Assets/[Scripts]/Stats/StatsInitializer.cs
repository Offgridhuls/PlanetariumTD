using UnityEngine;
using UnityEditor;
using System.IO;

namespace Planetarium.Stats
{
#if UNITY_EDITOR
    public static class StatsInitializer
    {
        private const string StatsPath = "Assets/[Resources]/Stats";

        [MenuItem("Planetarium/Stats/Initialize All Stats")]
        public static void InitializeAllStats()
        {
            Debug.Log("StatsInitializer: InitializeAllStats");

            // Ensure directories exist
            if (!Directory.Exists(StatsPath))
            {
                Directory.CreateDirectory(StatsPath);
            }

            // Create database if it doesn't exist
            var database = AssetDatabase.LoadAssetAtPath<StatDatabase>(StatsPath + "/GameStatDatabase.asset");
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<StatDatabase>();
                AssetDatabase.CreateAsset(database, StatsPath + "/GameStatDatabase.asset");
            }

            // Create Wave Stats
            var waveStat = CreateOrGetStat<WaveStat>("WaveStat", database);
            waveStat.defaultValue = new WaveStat.WaveData
            {
                currentWave = 0,
                enemiesRemaining = 0,
                timeUntilNextWave = 0,
                isFinalWave = false
            };
            Debug.Log("Created WaveStat");

            // Create Enemy Type Stats
            var enemyStats = CreateOrGetStat<EnemyTypeStats>("EnemyTypeStats", database);
            enemyStats.defaultValue = new EnemyTypeStats.EnemyTypeData();
            Debug.Log("Created EnemyTypeStats");

            // Create Turret Type Stats
            var turretStats = CreateOrGetStat<TurretTypeStats>("TurretTypeStats", database);
            turretStats.defaultValue = new TurretTypeStats.TurretTypeData();
            Debug.Log("Created TurretTypeStats");

            // Create Resource Stats
            var resourceStats = CreateOrGetStat<ResourceStat>("ResourceStats", database);
            resourceStats.defaultValue = new ResourceStat.ResourceData
            {
                currentResources = 0,
                totalResourcesCollected = 0,
                totalResourcesSpent = 0,
                resourceMultiplier = 1.0f
            };
            Debug.Log("Created ResourceStats");

            // Save all changes
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Stats initialization complete!");
        }

        private static T CreateOrGetStat<T>(string name, StatDatabase database) where T : StatBase
        {
            string path = $"{StatsPath}/{name}.asset";
            var stat = AssetDatabase.LoadAssetAtPath<T>(path);
            
            if (stat == null)
            {
                stat = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(stat, path);
            }

            if (!database.stats.Contains(stat))
            {
                database.stats.Add(stat);
            }

            return stat;
        }
    }
#endif
}

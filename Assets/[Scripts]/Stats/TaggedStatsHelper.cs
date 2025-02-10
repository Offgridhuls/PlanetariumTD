using UnityEngine;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    public static class TaggedStatsHelper
    {
        private static GameObject statsHolder;
        private static TaggedComponent statsComponent;

        // Cached tags for better performance
        private static class CachedTags
        {
            // Wave stats
            public static readonly GameplayTag WaveCurrent = new GameplayTag("Stats.Wave.Current");
            public static readonly GameplayTag WaveTotal = new GameplayTag("Stats.Wave.Total");
            public static readonly GameplayTag WaveTimeUntilNext = new GameplayTag("Stats.Wave.TimeUntilNext");
            public static readonly GameplayTag WaveEnemiesRemaining = new GameplayTag("Stats.Wave.EnemiesRemaining");
            public static readonly GameplayTag WaveIsFinal = new GameplayTag("Stats.Wave.IsFinal");

            // Enemy stats
            public static readonly GameplayTag EnemyTotalSpawned = new GameplayTag("Stats.Enemy.TotalSpawned");
            public static readonly GameplayTag EnemyCurrentAlive = new GameplayTag("Stats.Enemy.CurrentAlive");
            public static readonly GameplayTag EnemyTotalKilled = new GameplayTag("Stats.Enemy.TotalKilled");
            public static readonly GameplayTag EnemyTotalReachedEnd = new GameplayTag("Stats.Enemy.TotalReachedEnd");
            public static readonly GameplayTag EnemyTotalDamageTaken = new GameplayTag("Stats.Enemy.TotalDamageTaken");
            public static readonly GameplayTag EnemyTotalDamageDealt = new GameplayTag("Stats.Enemy.TotalDamageDealt");

            // Turret stats
            public static readonly GameplayTag TurretTotalPlaced = new GameplayTag("Stats.Turret.TotalPlaced");
            public static readonly GameplayTag TurretCurrentActive = new GameplayTag("Stats.Turret.CurrentActive");
            public static readonly GameplayTag TurretTotalRemoved = new GameplayTag("Stats.Turret.TotalRemoved");
            public static readonly GameplayTag TurretTotalDamageDealt = new GameplayTag("Stats.Turret.TotalDamageDealt");
            public static readonly GameplayTag TurretTotalKills = new GameplayTag("Stats.Turret.TotalKills");

            // Resource stats
            public static readonly GameplayTag ResourcesCurrent = new GameplayTag("Stats.Resources.Current");
            public static readonly GameplayTag ResourcesTotalGained = new GameplayTag("Stats.Resources.TotalGained");
            public static readonly GameplayTag ResourcesTotalSpent = new GameplayTag("Stats.Resources.TotalSpent");

            // Player stats
            public static readonly GameplayTag PlayerHealth = new GameplayTag("Stats.Player.Health");
            public static readonly GameplayTag PlayerMaxHealth = new GameplayTag("Stats.Player.MaxHealth");
        }

        private static void EnsureStatsComponent()
        {
            if (statsHolder == null)
            {
                statsHolder = new GameObject("StatsHolder");
                Object.DontDestroyOnLoad(statsHolder);
                statsComponent = statsHolder.AddComponent<TaggedComponent>();
            }
        }

        private static float GetStatValue(GameplayTag tag)
        {
            EnsureStatsComponent();
            var valueTag = new GameplayTag($"{tag.TagName}.Value");
            return float.Parse(valueTag.DevComment ?? "0");
        }

        private static void SetStatValue(GameplayTag tag, float value)
        {
            EnsureStatsComponent();
            var valueTag = new GameplayTag($"{tag.TagName}.Value", value.ToString());
            statsComponent.AddTag(valueTag);
        }

        private static void AddStatValue(GameplayTag tag, float value)
        {
            var currentValue = GetStatValue(tag);
            SetStatValue(tag, currentValue + value);
        }

        private static void SubtractStatValue(GameplayTag tag, float value)
        {
            var currentValue = GetStatValue(tag);
            SetStatValue(tag, currentValue - value);
        }

        private static void IncrementStatValue(GameplayTag tag)
        {
            AddStatValue(tag, 1);
        }

        private static void DecrementStatValue(GameplayTag tag)
        {
            SubtractStatValue(tag, 1);
        }

        public static void OnWaveStart(int waveNumber)
        {
            SetStatValue(CachedTags.WaveCurrent, waveNumber);
            IncrementStatValue(CachedTags.WaveTotal);
        }

        public static void OnEnemySpawned()
        {
            IncrementStatValue(CachedTags.EnemyTotalSpawned);
            IncrementStatValue(CachedTags.EnemyCurrentAlive);
        }

        public static void OnEnemyKilled()
        {
            IncrementStatValue(CachedTags.EnemyTotalKilled);
            DecrementStatValue(CachedTags.EnemyCurrentAlive);
        }

        public static void OnEnemyReachedEnd()
        {
            IncrementStatValue(CachedTags.EnemyTotalReachedEnd);
            DecrementStatValue(CachedTags.EnemyCurrentAlive);
        }

        public static void OnEnemyDamageTaken(float damage)
        {
            AddStatValue(CachedTags.EnemyTotalDamageTaken, damage);
        }

        public static void OnTurretPlaced()
        {
            IncrementStatValue(CachedTags.TurretTotalPlaced);
            IncrementStatValue(CachedTags.TurretCurrentActive);
        }

        public static void OnTurretRemoved()
        {
            IncrementStatValue(CachedTags.TurretTotalRemoved);
            DecrementStatValue(CachedTags.TurretCurrentActive);
        }

        public static void OnTurretDamageDealt(float damage)
        {
            AddStatValue(CachedTags.TurretTotalDamageDealt, damage);
        }

        public static void OnResourceGained(float amount)
        {
            AddStatValue(CachedTags.ResourcesTotalGained, amount);
            AddStatValue(CachedTags.ResourcesCurrent, amount);
        }

        public static void OnResourceSpent(float amount)
        {
            AddStatValue(CachedTags.ResourcesTotalSpent, amount);
            SubtractStatValue(CachedTags.ResourcesCurrent, amount);
        }

        public static Dictionary<string, float> GetWaveStats()
        {
            return new Dictionary<string, float>
            {
                { "Current", GetStatValue(CachedTags.WaveCurrent) },
                { "Total", GetStatValue(CachedTags.WaveTotal) }
            };
        }

        public static Dictionary<string, float> GetEnemyStats()
        {
            return new Dictionary<string, float>
            {
                { "TotalSpawned", GetStatValue(CachedTags.EnemyTotalSpawned) },
                { "CurrentAlive", GetStatValue(CachedTags.EnemyCurrentAlive) },
                { "TotalKilled", GetStatValue(CachedTags.EnemyTotalKilled) },
                { "TotalReachedEnd", GetStatValue(CachedTags.EnemyTotalReachedEnd) },
                { "TotalDamageTaken", GetStatValue(CachedTags.EnemyTotalDamageTaken) }
            };
        }

        public static Dictionary<string, float> GetTurretStats()
        {
            return new Dictionary<string, float>
            {
                { "TotalPlaced", GetStatValue(CachedTags.TurretTotalPlaced) },
                { "CurrentActive", GetStatValue(CachedTags.TurretCurrentActive) },
                { "TotalRemoved", GetStatValue(CachedTags.TurretTotalRemoved) },
                { "TotalDamageDealt", GetStatValue(CachedTags.TurretTotalDamageDealt) }
            };
        }

        public static Dictionary<string, float> GetResourceStats()
        {
            return new Dictionary<string, float>
            {
                { "Current", GetStatValue(CachedTags.ResourcesCurrent) },
                { "TotalGained", GetStatValue(CachedTags.ResourcesTotalGained) },
                { "TotalSpent", GetStatValue(CachedTags.ResourcesTotalSpent) }
            };
        }
    }
}

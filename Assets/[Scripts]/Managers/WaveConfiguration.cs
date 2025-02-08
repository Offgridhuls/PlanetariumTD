using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "WaveConfiguration", menuName = "PlanetariumTD/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    public enum SpawnPattern
    {
        Random,         // Completely random around the planet
        Clustered,     // Enemies spawn close to each other
        Opposite,      // Enemies spawn on opposite sides
        Sequential,    // Spawn in sequence around the planet
        Spiral,        // Spiral pattern around the planet
        Converging     // Multiple groups converging on a point
    }

    public enum WaveType
    {
        Normal,        // Standard wave
        Rush,          // Fast, weak enemies
        Elite,         // Few, strong enemies
        Swarm,         // Many weak enemies
        Mixed,         // Mix of different types
        Boss           // Boss with minions
    }

    public enum SpawnMethod
    {
        Random,     // Spawn at random points around planet
        Portal,     // Spawn from specific portals
        Mixed      // Mix of random and portal spawns
    }

    [System.Serializable]
    public class EnemyTypeConfig
    {
        [Header("Basic Settings")]
        public EnemySpawnData enemyData;
        [Range(0f, 1f)]
        public float baseSpawnWeight = 1f;
        [Range(0f, 1f)]
        public float waveProgressionWeight = 0.1f;

        [Header("Spawn Settings")]
        public SpawnMethod spawnMethod = SpawnMethod.Random;
        public List<string> allowedPortalIds = new List<string>();
        [Range(0f, 1f)]
        public float portalSpawnChance = 1f; // Only used if SpawnMethod is Mixed

        [Header("Scaling")]
        public float healthScaling = 1.2f;
        public float speedScaling = 1.1f;
        public float damageScaling = 1.15f;

        [Header("Spawn Timing")]
        public float minSpawnDelay = 0.5f;
        public float maxSpawnDelay = 2f;
        [Range(0, 100)]
        public int maxPerWave = 50;

        [Header("Special Behaviors")]
        public bool isBoss = false;
        public bool canBeElite = true;
        [Range(0f, 1f)]
        public float eliteChance = 0.1f;
        public float eliteHealthMultiplier = 2.5f;
        public float eliteSpeedMultiplier = 1.5f;
        public float eliteDamageMultiplier = 2f;
        public Color eliteColor = Color.red;

        public Material eliteMaterial;
        [Header("Formation Settings")]
        [Range(0f, 360f)]
        public float preferredSpawnArc = 360f;
        public float minDistanceFromOthers = 2f;
        public bool maintainFormation = false;
    }

    [System.Serializable]
    public class WaveModifier
    {
        public string name;
        public WaveType waveType = WaveType.Normal;
        [Range(0, 100)]
        public int minWaveNumber = 0;
        public float enemyCountMultiplier = 1f;
        public float healthMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float spawnDelayMultiplier = 1f;
        public SpawnPattern spawnPattern = SpawnPattern.Random;
        [Range(0f, 1f)]
        public float eliteChanceBonus = 0f;
    }

    [System.Serializable]
    public class BossWaveConfig
    {
        public int firstBossWave = 5;
        public int bossWaveInterval = 5;
        public float bossHealthMultiplier = 5f;
        public float bossSpeedMultiplier = 0.8f;
        public int minionSpawnCount = 10;
        public float minionSpawnInterval = 15f;
        [Range(0f, 1f)]
        public float minionHealthPercent = 0.5f;
    }

    [Header("Base Wave Settings")]
    public float baseEnemyCount = 10f;
    public float enemyCountScaling = 1.5f;
    public float waveDelayBase = 5f;
    public float waveDelayReduction = 0.95f;
    public float minWaveDelay = 2f;
    
    [Header("Difficulty Scaling")]
    public float globalHealthScaling = 1.1f;
    public float globalSpeedScaling = 1.05f;
    public float difficultyRampUpSpeed = 0.1f;
    
    [Header("Wave Patterns")]
    public List<WaveModifier> waveModifiers = new List<WaveModifier>();
    public BossWaveConfig bossWaveSettings;
    [Range(0f, 1f)]
    public float patternRandomization = 0.2f;
    
    [Header("Enemy Types")]
    public List<EnemyTypeConfig> enemyTypes = new List<EnemyTypeConfig>();

    public IEnumerable<EnemyTypeConfig> GetActiveEnemyConfigs()
    {
        return enemyTypes.Where(config => config != null && config.enemyData != null);
    }

    public EnemyTypeConfig GetEnemyConfig(EnemySpawnData enemyData)
    {
        return enemyTypes.FirstOrDefault(config => config.enemyData == enemyData);
    }

    public Vector3 GetSpawnPosition(Vector3 planetPosition, float spawnHeight)
    {
        // Random position around planet
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
        return planetPosition + randomDirection * spawnHeight;
    }

    [Header("Special Wave Features")]
    public bool enableDynamicDifficulty = true;
    [Range(0f, 1f)]
    public float playerPerformanceWeight = 0.5f;
    public float maxDifficultyAdjustment = 0.5f;

    public int CalculateWaveEnemyCount(int waveNumber, float playerPerformance = 1f)
    {
        float baseCount = baseEnemyCount * Mathf.Pow(enemyCountScaling, Mathf.Log(waveNumber + 1));
        
        // Apply wave modifier
        WaveModifier modifier = GetWaveModifier(waveNumber);
        if (modifier != null)
        {
            baseCount *= modifier.enemyCountMultiplier;
        }

        // Apply dynamic difficulty if enabled
        if (enableDynamicDifficulty)
        {
            float difficultyAdjustment = Mathf.Lerp(1f, playerPerformance, playerPerformanceWeight);
            difficultyAdjustment = Mathf.Clamp(difficultyAdjustment, 1f - maxDifficultyAdjustment, 1f + maxDifficultyAdjustment);
            baseCount *= difficultyAdjustment;
        }

        return Mathf.RoundToInt(baseCount);
    }

    public WaveModifier GetWaveModifier(int waveNumber)
    {
        var validModifiers = waveModifiers.FindAll(m => m.minWaveNumber <= waveNumber);
        if (validModifiers.Count == 0) return null;

        // Check for boss wave
        if (bossWaveSettings != null && 
            waveNumber >= bossWaveSettings.firstBossWave && 
            (waveNumber - bossWaveSettings.firstBossWave) % bossWaveSettings.bossWaveInterval == 0)
        {
            return validModifiers.Find(m => m.waveType == WaveType.Boss) ?? validModifiers[0];
        }

        return validModifiers[Random.Range(0, validModifiers.Count)];
    }

    public Vector3 GetSpawnPosition(SpawnPattern pattern, int spawnIndex, int totalSpawns, Vector3 planetPosition, float planetRadius, float heightOffset)
    {
        float angle = 0f;
        switch (pattern)
        {
            case SpawnPattern.Sequential:
                angle = (360f * spawnIndex) / totalSpawns;
                break;
                
            case SpawnPattern.Opposite:
                angle = (180f * spawnIndex);
                break;
                
            case SpawnPattern.Spiral:
                angle = (720f * spawnIndex) / totalSpawns;
                heightOffset += (planetRadius * 0.5f * spawnIndex) / totalSpawns;
                break;
                
            case SpawnPattern.Clustered:
                float clusterCenter = Random.Range(0f, 360f);
                float spread = 45f;
                angle = clusterCenter + Random.Range(-spread, spread);
                break;
                
            case SpawnPattern.Converging:
                int groupSize = totalSpawns / 3;
                int group = spawnIndex / groupSize;
                float groupAngle = 120f * group;
                float memberOffset = (40f * (spawnIndex % groupSize)) / groupSize;
                angle = groupAngle + memberOffset;
                break;
                
            case SpawnPattern.Random:
            default:
                angle = Random.Range(0f, 360f);
                break;
        }

        // Add some randomization if configured
        angle += Random.Range(-180f * patternRandomization, 180f * patternRandomization);
        
        // Convert to radians
        float rad = angle * Mathf.Deg2Rad;
        
        // Calculate position
        float x = Mathf.Cos(rad);
        float z = Mathf.Sin(rad);
        Vector3 direction = new Vector3(x, 0, z).normalized;
        
        return planetPosition + direction * (planetRadius + heightOffset);
    }

    public Dictionary<EnemySpawnData, int> GenerateWaveComposition(int waveNumber, int totalEnemies, float playerPerformance = 1f)
    {
        Dictionary<EnemySpawnData, int> composition = new Dictionary<EnemySpawnData, int>();
        WaveModifier modifier = GetWaveModifier(waveNumber);
        
        // Handle boss waves
        if (modifier?.waveType == WaveType.Boss)
        {
            var boss = enemyTypes.Find(e => e.isBoss);
            if (boss != null)
            {
                composition[boss.enemyData] = 1;
                totalEnemies = bossWaveSettings.minionSpawnCount;
            }
        }

        float totalWeight = 0f;
        float[] weights = new float[enemyTypes.Count];
        
        // Calculate weights based on wave type and progression
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (modifier?.waveType == WaveType.Boss && enemyTypes[i].isBoss)
                continue;

            float progressionFactor = Mathf.Log(waveNumber + 1) * enemyTypes[i].waveProgressionWeight;
            weights[i] = enemyTypes[i].baseSpawnWeight * (1f + progressionFactor);

            // Adjust weights based on wave type
            if (modifier != null)
            {
                switch (modifier.waveType)
                {
                    case WaveType.Rush:
                        weights[i] *= enemyTypes[i].maxSpawnDelay <= 1f ? 2f : 0.5f;
                        break;
                    case WaveType.Elite:
                        weights[i] *= enemyTypes[i].canBeElite ? 2f : 0.25f;
                        break;
                    case WaveType.Swarm:
                        weights[i] *= enemyTypes[i].maxPerWave >= 20 ? 2f : 0.5f;
                        break;
                }
            }

            totalWeight += weights[i];
        }

        // Distribute enemies
        int remainingEnemies = totalEnemies;
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (remainingEnemies <= 0) break;
            if (modifier?.waveType == WaveType.Boss && enemyTypes[i].isBoss) continue;

            float normalizedWeight = weights[i] / totalWeight;
            int enemyCount = Mathf.Min(
                enemyTypes[i].maxPerWave,
                Mathf.RoundToInt(totalEnemies * normalizedWeight)
            );
            
            if (enemyCount > 0)
            {
                composition[enemyTypes[i].enemyData] = enemyCount;
                remainingEnemies -= enemyCount;
            }
        }

        return composition;
    }

    public float GetWaveDelay(int waveNumber)
    {
        float delay = waveDelayBase * Mathf.Pow(waveDelayReduction, waveNumber - 1);
        
        WaveModifier modifier = GetWaveModifier(waveNumber);
        if (modifier != null)
        {
            delay *= modifier.spawnDelayMultiplier;
        }
        
        return Mathf.Max(minWaveDelay, delay);
    }

    public float GetHealthMultiplier(int waveNumber, bool isElite = false)
    {
        float multiplier = Mathf.Pow(globalHealthScaling, waveNumber - 1);
        
        WaveModifier modifier = GetWaveModifier(waveNumber);
        if (modifier != null)
        {
            multiplier *= modifier.healthMultiplier;
        }
        
        if (isElite)
        {
            var eliteConfig = enemyTypes.Find(e => e.canBeElite);
            if (eliteConfig != null)
            {
                multiplier *= eliteConfig.eliteHealthMultiplier;
            }
        }
        
        return multiplier;
    }

    public float GetSpeedMultiplier(int waveNumber, bool isElite = false)
    {
        float multiplier = Mathf.Pow(globalSpeedScaling, waveNumber - 1);
        
        WaveModifier modifier = GetWaveModifier(waveNumber);
        if (modifier != null)
        {
            multiplier *= modifier.speedMultiplier;
        }
        
        if (isElite)
        {
            var eliteConfig = enemyTypes.Find(e => e.canBeElite);
            if (eliteConfig != null)
            {
                multiplier *= eliteConfig.eliteSpeedMultiplier;
            }
        }
        
        return multiplier;
    }
}

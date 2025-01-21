using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Wave Configuration")]
    [SerializeField] private WaveConfiguration waveConfig;
    [SerializeField] private bool autoStartWaves = true;
    [SerializeField] private Transform spawnParent;

    private PlanetBase planet;
    private int currentWaveNumber = 0;
    private float nextSpawnTime;
    private float nextWaveTime;
    private Queue<(EnemySpawnData data, float delay)> currentWaveQueue = new Queue<(EnemySpawnData data, float delay)>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private Dictionary<string, EnemySpawnData> cachedEnemyData = new Dictionary<string, EnemySpawnData>();

    public System.Action<int> onWaveStart;
    public System.Action<int> onWaveComplete;
    public System.Action<float> onWaveProgress;

    private float playerPerformance = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cache enemy data from Resources folder
        EnemySpawnData[] enemyDataArray = Resources.LoadAll<EnemySpawnData>("Enemies");
        foreach (var data in enemyDataArray)
        {
            cachedEnemyData[data.name] = data;
        }
    }

    private void Start()
    {
        planet = Object.FindFirstObjectByType<PlanetBase>();
        if (spawnParent == null)
        {
            spawnParent = transform;
        }

        if (autoStartWaves && waveConfig != null)
        {
            StartNextWave();
        }
    }

    private void Update()
    {
        if (currentWaveQueue.Count > 0 && Time.time >= nextSpawnTime)
        {
            SpawnNextEnemy();
            
            // Update wave progress
            float progress = 1f - (float)currentWaveQueue.Count / GetTotalEnemiesInWave();
            onWaveProgress?.Invoke(progress);
        }
        else if (currentWaveQueue.Count == 0 && activeEnemies.Count == 0 && Time.time >= nextWaveTime)
        {
            StartNextWave();
        }

        // Clean up destroyed enemies
        activeEnemies.RemoveAll(e => e == null);
    }

    public void StartNextWave()
    {
        currentWaveNumber++;
        if (waveConfig == null)
        {
            Debug.LogError("No wave configuration assigned!");
            return;
        }

        // Calculate wave parameters
        int totalEnemies = waveConfig.CalculateWaveEnemyCount(currentWaveNumber, playerPerformance);
        float waveDelay = waveConfig.GetWaveDelay(currentWaveNumber);
        float healthMultiplier = waveConfig.GetHealthMultiplier(currentWaveNumber);

        // Get wave modifier for spawn pattern
        var modifier = waveConfig.GetWaveModifier(currentWaveNumber);
        var spawnPattern = modifier != null ? modifier.spawnPattern : WaveConfiguration.SpawnPattern.Random;

        // Generate wave composition
        var composition = waveConfig.GenerateWaveComposition(currentWaveNumber, totalEnemies, playerPerformance);
        
        // Clear and populate wave queue
        currentWaveQueue.Clear();
        int spawnIndex = 0;
        foreach (var kvp in composition)
        {
            var enemyConfig = waveConfig.enemyTypes.Find(x => x.enemyData == kvp.Key);
            if (enemyConfig == null) continue;

            for (int i = 0; i < kvp.Value; i++)
            {
                float spawnDelay = Random.Range(enemyConfig.minSpawnDelay, enemyConfig.maxSpawnDelay);
                currentWaveQueue.Enqueue((kvp.Key, spawnDelay));
                spawnIndex++;
            }
        }

        // Set timers
        nextWaveTime = Time.time + waveDelay;
        nextSpawnTime = Time.time;
        
        Debug.Log($"Starting Wave {currentWaveNumber} with {totalEnemies} enemies");
        onWaveStart?.Invoke(currentWaveNumber);
    }

    private void SpawnNextEnemy()
    {
        if (currentWaveQueue.Count == 0) return;

        var (enemyData, spawnDelay) = currentWaveQueue.Dequeue();
        if (enemyData == null) return;

        // Get current wave modifier for spawn pattern
        var modifier = waveConfig.GetWaveModifier(currentWaveNumber);
        var spawnPattern = modifier != null ? modifier.spawnPattern : WaveConfiguration.SpawnPattern.Random;

        // Calculate spawn position using pattern
        Vector3 spawnPosition = waveConfig.GetSpawnPosition(
            spawnPattern,
            GetTotalEnemiesInWave() - currentWaveQueue.Count,
            GetTotalEnemiesInWave(),
            planet.transform.position,
            planet.GetPlanetRadius(),
            enemyData.spawnParams.spawnHeight
        );

        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, spawnParent);
        
        // Configure the enemy
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            ConfigureEnemy(enemy, enemyData);
            activeEnemies.Add(enemy);
        }

        // Spawn effect
        if (enemyData.spawnEffect != null)
        {
            Instantiate(enemyData.spawnEffect, spawnPosition, Quaternion.identity);
        }

        nextSpawnTime = Time.time + spawnDelay;
    }

    private void ConfigureEnemy(EnemyBase enemy, EnemySpawnData spawnData)
    {
        // Get wave-specific configuration
        var enemyConfig = waveConfig.enemyTypes.Find(x => x.enemyData == spawnData);
        float healthMultiplier = waveConfig.GetHealthMultiplier(currentWaveNumber);
        float speedMultiplier = waveConfig.GetSpeedMultiplier(currentWaveNumber);

        // Check for elite status
        bool isElite = false;
        if (enemyConfig != null && enemyConfig.canBeElite)
        {
            float eliteChance = enemyConfig.eliteChance;
            var modifier = waveConfig.GetWaveModifier(currentWaveNumber);
            if (modifier != null)
            {
                eliteChance += modifier.eliteChanceBonus;
            }
            isElite = Random.value < eliteChance;
        }

        if (isElite)
        {
            healthMultiplier = waveConfig.GetHealthMultiplier(currentWaveNumber, true);
            speedMultiplier = waveConfig.GetSpeedMultiplier(currentWaveNumber, true);
        }

        // Set up health
        enemy.SetMaxHealth(spawnData.maxHealth * healthMultiplier);
        enemy.SetIntegrity(spawnData.baseIntegrity);

        // Configure flying enemy parameters if applicable
        if (spawnData.isFlying && enemy is FlyingEnemyBase flyingEnemy)
        {
            flyingEnemy.SetFlightParameters(spawnData.heightVariation, spawnData.heightChangeSpeed);
        }

        // Set effects
        enemy.SetEffects(spawnData.deathEffect, spawnData.damageEffect, spawnData.healthBarPrefab);
        
        // Set rewards
        enemy.SetRewards(spawnData.scoreValue, spawnData.resourceValue);

        // Apply elite visual changes if needed
        if (isElite && enemyConfig != null)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    material.color = enemyConfig.eliteColor;
                }
                renderer.materials = materials;
            }
        }
    }

    public void UpdatePlayerPerformance(float performance)
    {
        playerPerformance = Mathf.Clamp(performance, 0.5f, 1.5f);
    }

    public EnemySpawnData GetEnemyData(string enemyName)
    {
        if (cachedEnemyData.TryGetValue(enemyName, out EnemySpawnData data))
        {
            return data;
        }
        return null;
    }

    public void SpawnEnemyByName(string enemyName)
    {
        EnemySpawnData data = GetEnemyData(enemyName);
        if (data != null)
        {
            var enemyConfig = waveConfig.enemyTypes.Find(x => x.enemyData == data);
            float spawnDelay = enemyConfig != null ? 
                Random.Range(enemyConfig.minSpawnDelay, enemyConfig.maxSpawnDelay) : 
                1f;
            currentWaveQueue.Enqueue((data, spawnDelay));
        }
    }

    public int GetCurrentWave() => currentWaveNumber;
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public int GetRemainingEnemies() => currentWaveQueue.Count;
    public int GetTotalEnemiesInWave() => GetActiveEnemyCount() + GetRemainingEnemies();
    public List<EnemyBase> GetActiveEnemies() => activeEnemies.ToList();
}

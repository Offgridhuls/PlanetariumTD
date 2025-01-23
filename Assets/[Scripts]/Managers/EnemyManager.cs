using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Planetarium;

public class EnemyManager : SceneService
{
    [Header("Wave Configuration")]
    public WaveConfiguration waveConfig;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnHeight = 10f;

    private Queue<(EnemySpawnData data, float delay)> currentWaveQueue = new Queue<(EnemySpawnData data, float delay)>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private float nextSpawnTime;
    private float playerPerformance = 1f;
    private GameStateManager gameState;

    // Events with proper parameters
    public event System.Action<EnemyBase> OnEnemySpawned;
    public event System.Action<EnemyBase> OnEnemyDied;

    protected override void OnInitialize()
    {
        gameState = Context.GameState;
        
        if (spawnParent == null)
        {
            spawnParent = transform;
        }
    }

    protected override void OnDeinitialize()
    {
        ClearWave();
        OnEnemySpawned = null;
        OnEnemyDied = null;
    }

    protected override void OnTick()
    {
        if (currentWaveQueue.Count > 0 && Time.time >= nextSpawnTime)
        {
            SpawnNextEnemy();
        }

        UpdateActiveEnemies();
    }

    private void UpdateActiveEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }

    public void StartWave(int waveNumber)
    {
        if (waveConfig == null)
        {
            Debug.LogError("No wave configuration assigned!");
            return;
        }

        // Calculate wave parameters
        int totalEnemies = waveConfig.CalculateWaveEnemyCount(waveNumber, playerPerformance);
        float healthMultiplier = waveConfig.GetHealthMultiplier(waveNumber);

        // Get wave modifier for spawn pattern
        var modifier = waveConfig.GetWaveModifier(waveNumber);
        var spawnPattern = modifier != null ? modifier.spawnPattern : WaveConfiguration.SpawnPattern.Random;

        // Generate wave composition
        var composition = waveConfig.GenerateWaveComposition(waveNumber, totalEnemies, playerPerformance);
        
        // Clear and populate wave queue
        currentWaveQueue.Clear();
        foreach (var kvp in composition)
        {
            var enemyConfig = waveConfig.enemyTypes.Find(x => x.enemyData == kvp.Key);
            if (enemyConfig == null) continue;

            for (int i = 0; i < kvp.Value; i++)
            {
                float spawnDelay = Random.Range(enemyConfig.minSpawnDelay, enemyConfig.maxSpawnDelay);
                currentWaveQueue.Enqueue((kvp.Key, spawnDelay));
            }
        }

        nextSpawnTime = Time.time;
    }

    private void SpawnNextEnemy()
    {
        if (currentWaveQueue.Count == 0) return;

        var (enemyData, spawnDelay) = currentWaveQueue.Dequeue();
        if (enemyData == null) return;

        // Get current wave modifier for spawn pattern
        var modifier = waveConfig.GetWaveModifier(gameState.GetCurrentWave());
        var spawnPattern = modifier != null ? modifier.spawnPattern : WaveConfiguration.SpawnPattern.Random;

        // Calculate spawn position using pattern
        Vector3 spawnPosition = waveConfig.GetSpawnPosition(
            spawnPattern,
            GetTotalEnemiesInWave() - currentWaveQueue.Count,
            GetTotalEnemiesInWave(),
            Context.CurrentPlanet.transform.position,
            spawnHeight,
            Context.CurrentPlanet.GetPlanetRadius()
        );

        GameObject enemyObject = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, spawnParent);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            ConfigureEnemy(enemy, enemyData);
            activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
        }

        nextSpawnTime = Time.time + spawnDelay;
    }

    private void ConfigureEnemy(EnemyBase enemy, EnemySpawnData spawnData)
    {
        int currentWave = gameState.GetCurrentWave();
        
        // Get wave-specific configuration
        var enemyConfig = waveConfig.enemyTypes.Find(x => x.enemyData == spawnData);
        float healthMultiplier = waveConfig.GetHealthMultiplier(currentWave);
        float speedMultiplier = waveConfig.GetSpeedMultiplier(currentWave);

        // Check for elite status
        bool isElite = false;
        if (enemyConfig != null)
        {
            float eliteChance = enemyConfig.eliteChance;
            var modifier = waveConfig.GetWaveModifier(currentWave);
            if (modifier != null)
            {
                eliteChance += modifier.eliteChanceBonus;
            }
            isElite = Random.value < eliteChance;
        }

        if (isElite)
        {
            healthMultiplier = waveConfig.GetHealthMultiplier(currentWave, true);
            speedMultiplier = waveConfig.GetSpeedMultiplier(currentWave, true);
        }

        // Configure enemy with modified stats
        enemy.ProcessSpawnData(spawnData, speedMultiplier, isElite);
        enemy.onDeath.AddListener(() => {
            activeEnemies.Remove(enemy);
            OnEnemyDied?.Invoke(enemy);
        });

        // Set materials for elite enemies
        if (isElite && enemyConfig != null && enemyConfig.eliteMaterial != null)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = enemyConfig.eliteMaterial;
                }
                renderer.materials = materials;
            }
        }
    }

    public void UpdatePlayerPerformance(float performance)
    {
        playerPerformance = performance;
    }

    public void ClearWave()
    {
        currentWaveQueue.Clear();
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }

    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }

    public int GetRemainingEnemies()
    {
        return currentWaveQueue.Count + activeEnemies.Count;
    }

    public int GetTotalEnemiesInWave()
    {
        return currentWaveQueue.Count + activeEnemies.Count;
    }

    private void OnValidate()
    {
        spawnHeight = Mathf.Max(1f, spawnHeight);
    }
}

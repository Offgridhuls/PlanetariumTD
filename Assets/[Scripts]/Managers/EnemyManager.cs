using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Planetarium;
using Planetarium.Spawning;

public class EnemyManager : SceneService
{
    [Header("Wave Configuration")]
    public WaveConfiguration waveConfig;
    [Header("Spawn Configuration")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private PortalManager portalManager;

    private Queue<(EnemySpawnData data, float delay)> currentWaveQueue = new Queue<(EnemySpawnData data, float delay)>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private float nextSpawnTime;
    private float playerPerformance = 1f;
    private GameStateManager gameState;
    private PlanetBase currentPlanet;

    // Events with proper parameters
    public event System.Action<EnemyBase> OnEnemySpawned;
    public event System.Action<EnemyBase> OnEnemyDied;

    protected override void OnInitialize()
    {
        gameState = Context.GameState;
        currentPlanet = FindObjectOfType<PlanetBase>();
        
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

        // Activate portals for the wave
        if (portalManager != null)
        {
            // Get all portal IDs needed for this wave's enemies
            var neededPortals = waveConfig.GetActiveEnemyConfigs()
                .Where(c => c.spawnMethod != WaveConfiguration.SpawnMethod.Random)
                .SelectMany(c => c.allowedPortalIds)
                .Distinct()
                .ToList();

            portalManager.ActivatePortals(neededPortals);
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

        var (enemyData, delay) = currentWaveQueue.Dequeue();
        nextSpawnTime = Time.time + delay;

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // Determine spawn method
        var enemyConfig = waveConfig.GetEnemyConfig(enemyData);
        bool usePortal = enemyConfig.spawnMethod == WaveConfiguration.SpawnMethod.Portal ||
                        (enemyConfig.spawnMethod == WaveConfiguration.SpawnMethod.Mixed && 
                         Random.value < enemyConfig.portalSpawnChance);

        if (usePortal && portalManager != null)
        {
            // Try to spawn from portal
            var portal = portalManager.GetPortalForEnemy(enemyData);
            if (portal != null)
            {
                spawnPosition = portal.GetSpawnPosition();
                spawnRotation = portal.GetSpawnRotation();
            }
            else
            {
                // Fallback to random spawn if no portal available
                spawnPosition = waveConfig.GetSpawnPosition(currentPlanet.transform.position, spawnHeight);
                spawnRotation = Quaternion.identity;
            }
        }
        else
        {
            // Use random spawn position
            spawnPosition = waveConfig.GetSpawnPosition(currentPlanet.transform.position, spawnHeight);
            spawnRotation = Quaternion.identity;
        }

        // Create the enemy
        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPosition, spawnRotation, spawnParent);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        
        if (enemy != null)
        {
            ConfigureEnemy(enemy, enemyData);
            activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
        }
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
        enemy.SetRewards(spawnData.scoreValue, spawnData.resourceValue);
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
        // Clear spawn queue first to prevent new spawns
        currentWaveQueue.Clear();
        
        // Destroy all active enemies
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        
        // Clear the list after destroying
        activeEnemies.Clear();
        
        // Reset spawn timer
        nextSpawnTime = 0f;
        
        // Deactivate all portals
        if (portalManager != null)
        {
            portalManager.DeactivatePortals();
        }
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

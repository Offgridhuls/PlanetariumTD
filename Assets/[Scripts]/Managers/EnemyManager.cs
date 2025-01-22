using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Planetarium;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Wave Configuration")]
    public WaveConfiguration waveConfig;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnHeight = 10f;

    private PlanetBase planet;
    private Queue<(EnemySpawnData data, float delay)> currentWaveQueue = new Queue<(EnemySpawnData data, float delay)>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private float nextSpawnTime;
    private float playerPerformance = 1f;

    // Events with proper parameters
    public UnityEvent<EnemyBase> OnEnemySpawned = new UnityEvent<EnemyBase>();
    public UnityEvent<EnemyBase> OnEnemyDied = new UnityEvent<EnemyBase>();

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
    }

    private void Start()
    {
        planet = FindFirstObjectByType<PlanetBase>();
        if (planet == null)
        {
            Debug.LogError("No planet found in scene!");
            return;
        }

        if (spawnParent == null)
        {
            spawnParent = transform;
        }
    }

    private void Update()
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
        var modifier = waveConfig.GetWaveModifier(GameStateManager.Instance.GetCurrentWave());
        var spawnPattern = modifier != null ? modifier.spawnPattern : WaveConfiguration.SpawnPattern.Random;

        // Calculate spawn position using pattern
        Vector3 spawnPosition = waveConfig.GetSpawnPosition(
            spawnPattern,
            GetTotalEnemiesInWave() - currentWaveQueue.Count,
            GetTotalEnemiesInWave(),
            planet.transform.position,
            spawnHeight,
            planet.GetPlanetRadius()
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
        int currentWave = GameStateManager.Instance.GetCurrentWave();
        
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

    public int GetActiveEnemyCount() => activeEnemies.Count;
    public int GetRemainingEnemies() => currentWaveQueue.Count;
    public int GetTotalEnemiesInWave() => GetActiveEnemyCount() + GetRemainingEnemies();
    public List<EnemyBase> GetActiveEnemies() => activeEnemies;
    public bool HasActiveEnemies() => activeEnemies.Count > 0 || currentWaveQueue.Count > 0;
}

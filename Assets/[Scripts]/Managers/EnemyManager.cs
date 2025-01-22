using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Wave Configuration")]
    public WaveConfiguration waveConfig; // Made public for GameStateManager
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private bool autoStartWaves = true;
    [SerializeField] private Transform spawnParent;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnHeight = 10f;

    private PlanetBase planet;
    private int currentWaveNumber = 0;
    private float nextSpawnTime;
    private float nextWaveTime;
    private Queue<(EnemySpawnData data, float delay)> currentWaveQueue = new Queue<(EnemySpawnData data, float delay)>();
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private Dictionary<string, EnemySpawnData> cachedEnemyData = new Dictionary<string, EnemySpawnData>();
    private float playerPerformance = 1f;

    public UnityEvent onEnemySpawned = new UnityEvent();
    public UnityEvent onEnemyDied = new UnityEvent();
    public System.Action<int> onWaveStart;
    public System.Action<int> onWaveComplete;
    public System.Action<float> onWaveProgress;

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
        }

        if (currentWaveQueue.Count == 0 && activeEnemies.Count == 0 && Time.time >= nextWaveTime)
        {
            StartNextWave();
        }

        UpdateActiveEnemies();
    }

    private void UpdateActiveEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }

    public void StartNextWave()
    {
        currentWaveNumber++;
        if (waveConfig == null)
        {
            Debug.LogError("No wave configuration assigned!");
            return;
        }

        onWaveStart?.Invoke(currentWaveNumber);

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

        nextWaveTime = Time.time + waveDelay;
        nextSpawnTime = Time.time;

        onWaveProgress?.Invoke(CalculateWaveProgress());
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
            spawnHeight,
            planet.GetPlanetRadius()
        );

        GameObject enemyObject = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, spawnParent);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            ConfigureEnemy(enemy, enemyData);
            activeEnemies.Add(enemy);
            onEnemySpawned.Invoke();
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
        if (enemyConfig != null)
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

        // Configure enemy with modified stats
        enemy.ProcessSpawnData(spawnData, speedMultiplier, isElite);
        enemy.onDeath.AddListener(() => OnEnemyDied(enemy));

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

    private void OnEnemyDied(EnemyBase enemy)
    {
        activeEnemies.Remove(enemy);
        onEnemyDied.Invoke();
        onWaveProgress?.Invoke(CalculateWaveProgress());
    }

    private EnemySpawnData GetEnemyData(string enemyName)
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

    public void UpdatePlayerPerformance(float performance)
    {
        playerPerformance = performance;
    }

    private float CalculateWaveProgress()
    {
        int totalEnemies = GetTotalEnemiesInWave();
        if (totalEnemies == 0) return 1f;
        return 1f - ((float)(activeEnemies.Count + currentWaveQueue.Count) / totalEnemies);
    }

    public int GetActiveEnemyCount() => activeEnemies.Count;
    public int GetRemainingEnemies() => currentWaveQueue.Count;
    public int GetTotalEnemiesInWave() => GetActiveEnemyCount() + GetRemainingEnemies();
    public List<EnemyBase> GetActiveEnemies() => activeEnemies;
    public bool IsWaveInProgress() => currentWaveQueue.Count > 0 || activeEnemies.Count > 0;
}

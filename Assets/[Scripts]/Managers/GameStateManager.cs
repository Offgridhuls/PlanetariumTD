using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public class WaveStateEvent : UnityEvent<WaveState> { }

[Serializable]
public class WaveState
{
    public int CurrentWave { get; private set; }
    public int TotalWaves { get; private set; }
    public int ActiveEnemies { get; private set; }
    public bool IsWaveInProgress { get; private set; }
    public float WaveProgress { get; private set; }

    public WaveState(int currentWave, int totalWaves, int activeEnemies, bool isWaveInProgress, float waveProgress)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        ActiveEnemies = activeEnemies;
        IsWaveInProgress = isWaveInProgress;
        WaveProgress = waveProgress;
    }
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private float timeBetweenWaves = 10f;
    
    public WaveStateEvent onWaveStateChanged = new WaveStateEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent onGameWon = new UnityEvent();

    private EnemyManager enemyManager;
    private int currentWave = 0;
    private float waveTimer;
    private bool isGameOver;

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
        enemyManager = FindFirstObjectByType<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("No EnemyManager found in scene!");
            return;
        }

        // Subscribe to enemy events
        enemyManager.onEnemySpawned.AddListener(OnEnemyStateChanged);
        enemyManager.onEnemyDied.AddListener(OnEnemyStateChanged);

        // Start first wave
        StartNextWave();
    }

    private void Update()
    {
        if (isGameOver) return;

        if (!enemyManager.IsWaveInProgress() && currentWave < GetTotalWaves())
        {
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0)
            {
                StartNextWave();
            }
        }
        else if (enemyManager.IsWaveInProgress() && enemyManager.GetActiveEnemyCount() == 0)
        {
            OnWaveCompleted();
        }
    }

    private void StartNextWave()
    {
        currentWave++;
        enemyManager.StartNextWave();
        waveTimer = timeBetweenWaves;
        NotifyWaveStateChanged();
    }

    private void OnWaveCompleted()
    {
        if (currentWave >= GetTotalWaves())
        {
            GameWon();
        }
        else
        {
            waveTimer = timeBetweenWaves;
            NotifyWaveStateChanged();
        }
    }

    private void OnEnemyStateChanged()
    {
        NotifyWaveStateChanged();
    }

    private void NotifyWaveStateChanged()
    {
        float waveProgress = 1f;
        if (enemyManager.GetTotalEnemiesInWave() > 0)
        {
            waveProgress = 1f - ((float)enemyManager.GetActiveEnemyCount() / enemyManager.GetTotalEnemiesInWave());
        }

        WaveState state = new WaveState(
            currentWave,
            GetTotalWaves(),
            enemyManager.GetActiveEnemyCount(),
            enemyManager.IsWaveInProgress(),
            waveProgress
        );
        onWaveStateChanged.Invoke(state);
    }

    private void GameWon()
    {
        isGameOver = true;
        onGameWon.Invoke();
    }

    public void GameOver()
    {
        isGameOver = true;
        onGameOver.Invoke();
    }

    private int GetTotalWaves()
    {
        if (enemyManager != null && enemyManager.waveConfig != null)
        {
            return enemyManager.waveConfig.enemyTypes.Count;
        }
        return 0;
    }

    public int GetCurrentWave() => currentWave;
    public int GetActiveEnemies() => enemyManager.GetActiveEnemyCount();
    public bool IsWaveInProgress() => enemyManager.IsWaveInProgress();
    public float GetWaveTimer() => waveTimer;
}

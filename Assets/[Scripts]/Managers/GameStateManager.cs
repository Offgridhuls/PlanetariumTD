using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;

namespace Planetarium
{
    public class GameStateChangedEventArgs : EventArgs
    {
        public GameState PreviousState { get; }
        public GameState CurrentState { get; }

        public GameStateChangedEventArgs(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }

    [Serializable]
    public class GameState
    {
        public int currentWave;
        public int score;
        public float baseHealth;
        public int currency;
        public float gameTime;
        public bool isWaveInProgress;
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        // Events
        public event EventHandler<GameStateChangedEventArgs> OnGameStateChanged;
        public event Action<int> OnWaveChanged;
        public event Action<float> OnBaseHealthChanged;
        public event Action<int> OnCurrencyChanged;
        public event Action<bool> OnWaveProgressChanged;
        public event Action<bool> OnGameOverChanged;
        public event Action<int> OnScoreChanged;

        // Public Properties
        public int CurrentWave => currentWave;
        public int CurrentScore => currentScore;
        public float CurrentBaseHealth => currentBaseHealth;
        public float MaxBaseHealth => baseHealth;
        public int Currency => currency;
        public float WaveTimer => waveTimer;
        public bool IsWaveInProgress => isWaveInProgress;
        public float GameTime => gameTime;
        public bool IsGameOver => isGameOver;
        public int EnemiesRemainingInWave => enemiesRemainingInWave;
        public WaveConfiguration WaveConfig => waveConfig;
        public float TimeBetweenWaves => timeBetweenWaves;

        [Header("Wave Configuration")]
        [SerializeField] private WaveConfiguration waveConfig;
        [SerializeField] private float timeBetweenWaves = 30f;

        [Header("Starting Values")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private int startingCurrency = 500;
        
        [Header("Current Game State")]
        [SerializeField] private int currentWave;
        [SerializeField] private int currentScore;
        [SerializeField] private float currentBaseHealth;
        [SerializeField] private int currency;
        [SerializeField] private float waveTimer;
        [SerializeField] private bool isWaveInProgress;
        [SerializeField] private float gameTime;
        [SerializeField] private bool isGameOver;
        [SerializeField] private int enemiesRemainingInWave;

        [Header("Debug Options")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private bool shouldLoad = false;
        [SerializeField] private KeyCode saveHotkey = KeyCode.F5;
        [SerializeField] private KeyCode loadHotkey = KeyCode.F9;

        private float gameStartTime;
        private EnemyManager enemyManager;
        private bool isCheckingWaveEnd;
        private GameState previousState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            enemyManager = FindFirstObjectByType<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.OnEnemySpawned.AddListener(OnEnemySpawned);
                enemyManager.OnEnemyDied.AddListener(OnEnemyKilled);
            }
            else
            {
                Debug.LogError("EnemyManager not found in scene!");
            }
        }

        private void Update()
        {
            if (isGameOver) return;

            if (isWaveInProgress)
            {
                // Check if wave is complete
                if (!isCheckingWaveEnd && enemyManager != null && !enemyManager.HasActiveEnemies())
                {
                    isCheckingWaveEnd = true;
                    EndWave();
                }
            }
            else
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0)
                {
                    StartWave();
                }
            }

            if (showDebugInfo)
            {
                gameTime = Time.time - gameStartTime;
            }

            // Debug hotkeys
            if (Input.GetKeyDown(saveHotkey) && showDebugInfo)
            {
                SaveGameState();
                Debug.Log("Game state saved manually");
            }
            if (Input.GetKeyDown(loadHotkey) && showDebugInfo)
            {
                LoadGameState();
                Debug.Log("Game state loaded manually");
            }
        }

        private void OnEnemySpawned(EnemyBase enemy)
        {
            enemiesRemainingInWave++;
            // Subscribe to enemy events
            enemy.onDeath.AddListener(() => OnEnemyKilled(enemy));
            enemy.onScoreGained.AddListener(AddScore);
            enemy.onResourceGained.AddListener(AddCurrency);
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            enemiesRemainingInWave--;
        }

        private void InitializeGame()
        {
            // Try to load saved game state
            if (LoadGameState() && shouldLoad)
            {
                Debug.Log("Loaded saved game state");
            }
            else
            {
                // Initialize new game
                currentBaseHealth = baseHealth;
                currency = startingCurrency;
                currentWave = 0;
                currentScore = 0;
                isWaveInProgress = false;
                isGameOver = false;
                enemiesRemainingInWave = 0;
                gameStartTime = Time.time;
                waveTimer = timeBetweenWaves;
            }

            // Notify listeners of initial state
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            var currentState = new GameState
            {
                currentWave = currentWave,
                score = currentScore,
                baseHealth = currentBaseHealth,
                currency = currency,
                gameTime = gameTime,
                isWaveInProgress = isWaveInProgress
            };

            OnGameStateChanged?.Invoke(this, new GameStateChangedEventArgs(previousState, currentState));
            previousState = currentState;
        }

        public void StartWave()
        {
            if (isWaveInProgress || isGameOver) return;

            SetCurrentWave(currentWave + 1);
            SetWaveInProgress(true);
            waveTimer = 0;
            enemiesRemainingInWave = 0;
            isCheckingWaveEnd = false;
            
            // Start wave in EnemyManager
            if (enemyManager != null)
            {
                enemyManager.StartWave(currentWave);
            }
            
            if (autoSaveEnabled) SaveGameState();
        }

        public void EndWave()
        {
            if (!isWaveInProgress) return;

            SetWaveInProgress(false);
            waveTimer = timeBetweenWaves;
            enemiesRemainingInWave = 0;
            isCheckingWaveEnd = false;
            
            if (IsLastWave())
            {
                OnGameOverChanged?.Invoke(true);
                isGameOver = true;
            }
            
            if (autoSaveEnabled) SaveGameState();
        }

        public void TakeDamage(float damage)
        {
            float previousHealth = currentBaseHealth;
            currentBaseHealth = Mathf.Max(0, currentBaseHealth - damage);
            
            if (currentBaseHealth <= 0 && !isGameOver)
            {
                SetGameOver(true);
            }

            OnBaseHealthChanged?.Invoke(currentBaseHealth);
            NotifyStateChanged();
        }
        
        public void AddCurrency(int amount)
        {
            int previousCurrency = currency;
            currency += amount;
            OnCurrencyChanged?.Invoke(currency);
            NotifyStateChanged();
        }

        public bool TrySpendCurrency(int amount)
        {
            if (currency >= amount)
            {
                currency -= amount;
                OnCurrencyChanged?.Invoke(currency);
                NotifyStateChanged();
                return true;
            }
            return false;
        }

        private bool IsLastWave()
        {
            return currentWave >= 30;
        }

        #region Save/Load System
        
        private string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

        public void SaveGameState()
        {
            try
            {
                GameState state = new GameState
                {
                    currentWave = currentWave,
                    score = currentScore,
                    baseHealth = currentBaseHealth,
                    currency = currency,
                    gameTime = Time.time - gameStartTime,
                    isWaveInProgress = isWaveInProgress
                };

                string json = JsonUtility.ToJson(state, true);
                File.WriteAllText(SavePath, json);

                if (showDebugInfo)
                {
                    Debug.Log($"Game saved to: {SavePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game state: {e.Message}");
            }
        }

        private bool LoadGameState()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    GameState state = JsonUtility.FromJson<GameState>(json);

                    currentWave = state.currentWave;
                    currentScore = state.score;
                    currentBaseHealth = state.baseHealth;
                    currency = state.currency;
                    gameStartTime = Time.time - state.gameTime;
                    isWaveInProgress = state.isWaveInProgress;
                    waveTimer = isWaveInProgress ? 0 : timeBetweenWaves;

                    NotifyStateChanged();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Game loaded from: {SavePath}");
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game state: {e.Message}");
            }
            return false;
        }

        public void DeleteSaveGame()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    if (showDebugInfo)
                    {
                        Debug.Log("Save game deleted");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save game: {e.Message}");
            }
        }

        #endregion

        // Utility methods for other scripts
        public int GetCurrentWave() => currentWave;
        public float GetBaseHealth() => currentBaseHealth;
        public int GetCurrency() => currency;
        public int GetScore() => currentScore;
        public float GetWaveTimer() => waveTimer;
        public float GetGameTime() => gameTime;
        public int GetEnemiesRemaining() => enemiesRemainingInWave;

        private void OnValidate()
        {
            // Ensure starting values are valid
            baseHealth = Mathf.Max(1f, baseHealth);
            startingCurrency = Mathf.Max(0, startingCurrency);
            timeBetweenWaves = Mathf.Max(1f, timeBetweenWaves);
        }

        private void SetGameOver(bool value)
        {
            if (isGameOver != value)
            {
                isGameOver = value;
                OnGameOverChanged?.Invoke(isGameOver);
                NotifyStateChanged();
            }
        }

        private void SetWaveInProgress(bool value)
        {
            if (isWaveInProgress != value)
            {
                isWaveInProgress = value;
                OnWaveProgressChanged?.Invoke(isWaveInProgress);
                NotifyStateChanged();
            }
        }

        private void SetCurrentWave(int wave)
        {
            if (currentWave != wave)
            {
                currentWave = wave;
                OnWaveChanged?.Invoke(currentWave);
                NotifyStateChanged();
            }
        }

        private void AddScore(int amount)
        {
            if (amount != 0)
            {
                currentScore += amount;
                OnScoreChanged?.Invoke(currentScore);
                NotifyStateChanged();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;
using Planetarium.UI;
using Planetarium.UI.Views;

namespace Planetarium
{
    public enum GameState
    {
        None,
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Serializable]
    public class GameStateData
    {
        public int currentWave;
        public int score;
        public float baseHealth;
        public int currency;
        public float gameTime;
        public bool isWaveInProgress;
    }

    public class GameStateChangedEventArgs : EventArgs
    {
        public GameStateData PreviousState { get; }
        public GameStateData CurrentState { get; }

        public GameStateChangedEventArgs(GameStateData previousState, GameStateData currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }

    public class GameStateManager : SceneService
    {
        // Events
        public event Action<float> OnBaseHealthChanged;
        public event Action<int> OnWaveChanged;
        public event Action<float> OnWaveTimerChanged;
        public event Action<int> OnScoreChanged;
        public event Action<bool> OnGameOverChanged;
        public event Action<bool> OnWaveStateChanged;
        public event Action<float> OnPlayerPerformanceChanged;
        public event Action<int> OnPlayerLevelChanged;
        public event Action<int> OnCurrencyChanged;
        public event Action<Dictionary<string, InventoryItem>> OnInventoryChanged;
        public event Action<string> OnItemSelected;
        public event EventHandler<GameStateChangedEventArgs> OnGameStateChanged;
        public event Action<bool> OnWaveProgressChanged;
        public event Action OnGameOver;

        // Public Properties
        public GameState State { get; private set; }
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
        private GameStateData previousState;

        // Generator tracking
        private HashSet<GeneratorBase> activeGenerators = new HashSet<GeneratorBase>();
        private bool isGameEnding = false;
        private float totalGeneratorHealth;
        private float maxTotalGeneratorHealth;

        protected override void OnInitialize()
        {
            Debug.Log("GameStateManager: OnInitialize called");
            InitializeGame();
            enemyManager = Context.EnemyManager;
            activeGenerators = new HashSet<GeneratorBase>();
            OnCurrencyChanged?.Invoke(currency);
            if (enemyManager != null)
            {
                enemyManager.OnEnemySpawned += OnEnemySpawned;
                enemyManager.OnEnemyDied += OnEnemyKilled;
                
            }
            else
            {
                Debug.LogError("EnemyManager not found in scene!");
            }
        }

        protected override void OnDeinitialize()
        {
            if (enemyManager != null)
            {
                enemyManager.OnEnemySpawned -= OnEnemySpawned;
                enemyManager.OnEnemyDied -= OnEnemyKilled;
            }

            // Clear all event subscribers
            OnGameStateChanged = null;
            OnWaveChanged = null;
            OnBaseHealthChanged = null;
            OnCurrencyChanged = null;
            OnWaveProgressChanged = null;
            OnGameOverChanged = null;
            OnScoreChanged = null;
            OnGameOver = null;
        }

        protected override void OnTick()
        {
            if (isGameOver) return;

            if (isWaveInProgress)
            {
                // Check if wave is complete
                if (!isCheckingWaveEnd && enemyManager != null && !(enemyManager.GetActiveEnemyCount() > 0))
                {
                    isCheckingWaveEnd = true;
                    EndWave();
                }
            }
            else
            {
                waveTimer -= Time.deltaTime;
                OnWaveTimerChanged?.Invoke(waveTimer);

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
            Debug.Log("GameStateManager: Initializing game state");
            // Try to load saved game state
            if (LoadGameState() && shouldLoad)
            {
                Debug.Log("Loaded saved game state");
            }
            else
            {
                // Initialize new game
                State = GameState.None;
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
            var currentState = new GameStateData
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

        public void ChangeState(GameState newState)
        {
            if (State == newState) return;

            State = newState;
            
            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    Time.timeScale = 0f;
                    SaveHighScore();
                    break;
            }

            NotifyStateChanged();
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
                ChangeState(GameState.Victory);
            }
            
            if (autoSaveEnabled) SaveGameState();
        }

        public void TakeDamage(float damage)
        {
            if (isGameOver) return;

            float previousHealth = currentBaseHealth;
            currentBaseHealth = Mathf.Max(0f, currentBaseHealth - damage);

            if (previousHealth != currentBaseHealth)
            {
                Debug.Log($"GameStateManager: Base health changed to {currentBaseHealth}");
                //UpdateBaseHealthUI();
            }

            if (currentBaseHealth <= 0 && !isGameOver)
            {
                EndGame(false);
            }
        }

        private void EndGame(bool isGameOver)
        {
            TriggerGameOver();
        }
        public void AddCurrency(int amount)
        {
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
            return currentWave >= 50;
        }

        private void SaveHighScore()
        {
            int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
            if (CurrentScore > currentHighScore)
            {
                PlayerPrefs.SetInt("HighScore", CurrentScore);
                PlayerPrefs.Save();
            }
        }

        #region Save/Load System
        
        private string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.json");

        public void SaveGameState()
        {
            try
            {
                GameStateData state = new GameStateData
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
                    GameStateData state = JsonUtility.FromJson<GameStateData>(json);

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
        

        private void SetGameOver(bool value)
        {
            if (isGameOver != value)
            {
                Debug.Log($"GameStateManager: Game over state changed to {value}");
                isGameOver = value;
                OnGameOverChanged?.Invoke(isGameOver);
                NotifyStateChanged();
            }
        }

        private void SetWaveInProgress(bool value)
        {
            if (isWaveInProgress != value)
            {
                Debug.Log($"GameStateManager: Wave state changed to {value}");
                isWaveInProgress = value;
                OnWaveProgressChanged?.Invoke(isWaveInProgress);
                NotifyStateChanged();
            }
        }

        private void SetCurrentWave(int wave)
        {
            if (currentWave != wave)
            {
                Debug.Log($"GameStateManager: Wave changed to {wave}");
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

        private void OnValidate()
        {
            // Ensure starting values are valid
            baseHealth = Mathf.Max(1f, baseHealth);
            startingCurrency = Mathf.Max(0, startingCurrency);
            timeBetweenWaves = Mathf.Max(1f, timeBetweenWaves);
        }

        public void RegisterGenerator(GeneratorBase generator)
        {
            if (generator != null)
            {
                activeGenerators.Add(generator);
                generator.OnDestroyed += OnGeneratorDestroyed;
                generator.OnHealthChanged += OnGeneratorHealthChanged;
                
                maxTotalGeneratorHealth += generator.MaxHealth;
                totalGeneratorHealth += generator.CurrentHealth;
                
                UpdateBaseHealthUI();
            }
        }

        public void UnregisterGenerator(GeneratorBase generator)
        {
            if (generator != null)
            {
                activeGenerators.Remove(generator);
                generator.OnDestroyed -= OnGeneratorDestroyed;
                generator.OnHealthChanged -= OnGeneratorHealthChanged;
                
                maxTotalGeneratorHealth -= generator.MaxHealth;
                totalGeneratorHealth -= generator.CurrentHealth;
                
                UpdateBaseHealthUI();
                
                // Check if this was the last generator
                if (activeGenerators.Count == 0 && !isGameEnding)
                {
                    TriggerGameOver();
                }
            }
        }

        private void OnGeneratorHealthChanged(float healthPercentage)
        {
            UpdateTotalGeneratorHealth();
        }

        private void UpdateTotalGeneratorHealth()
        {
            totalGeneratorHealth = 0f;
            maxTotalGeneratorHealth = 0f;

            foreach (var generator in activeGenerators)
            {
                totalGeneratorHealth += generator.CurrentHealth;
                maxTotalGeneratorHealth += generator.MaxHealth;
            }

            UpdateBaseHealthUI();
        }

        private void UpdateBaseHealthUI()
        {
            // Calculate total health including generators
            float totalHealth = currentBaseHealth + totalGeneratorHealth;
            float maxTotalHealth = baseHealth + maxTotalGeneratorHealth;
            
            // Convert to percentage
            float healthPercentage = (totalHealth / maxTotalHealth) * 100f;
            
            // Notify UI
            OnBaseHealthChanged?.Invoke(healthPercentage);
        }

        private void OnGeneratorDestroyed()
        {
            // Check remaining generators after one is destroyed
            if (activeGenerators.Count == 0 && !isGameEnding)
            {
                // Clear all generator health immediately
                totalGeneratorHealth = 0f;
                maxTotalGeneratorHealth = 0f;
                UpdateBaseHealthUI();
                
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            if (State == GameState.GameOver || isGameEnding) return;
            
            isGameEnding = true;

            // Stop game systems
            Time.timeScale = 0f;
            
            // Reset health to 0 and notify UI
            currentBaseHealth = 0f;
            totalGeneratorHealth = 0f;
            maxTotalGeneratorHealth = 0f;
            UpdateBaseHealthUI();
            
            // Stop wave timer and spawning
            if (enemyManager != null)
            {
                enemyManager.ClearWave();
                enemyManager.enabled = false;
                OnWaveProgressChanged?.Invoke(false); // Hide wave progress
                OnWaveTimerChanged?.Invoke(0f); // Reset wave timer
            }

            // Update state
            SetGameState(GameState.GameOver);

            // Close all views and show game over
            var uiManager = Scene.GetService<UIManager>();
            if (uiManager != null)
            {
                // Get all active views and close them
                var views = uiManager.GetComponentsInChildren<UIView>(true);
                foreach (var view in views)
                {
                    view.Close();
                }
                
                // Show game over view
                uiManager.OpenView<GameOverView>();
            }

            OnGameOver?.Invoke();
            isGameEnding = false;
        }

        private void SetGameState(GameState newState)
        {
            if (State == newState) return;
            
            State = newState;
            OnGameStateChanged?.Invoke(this, new GameStateChangedEventArgs(previousState, new GameStateData()));
        }
    }
}

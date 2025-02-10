using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Planetarium.Stats;
using Planetarium.UI;
using Planetarium.UI.Views;
using GameOverView = Planetarium.UI.Views.GameOverView;

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

        [Header("State Configuration")]
        [SerializeField] private GameStateTransitionConfig stateConfig;
        [SerializeField] private bool showDebugStateTransitions = true;

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
            base.OnInitialize();
            
            if (stateConfig == null)
            {
                Debug.LogError("GameStateManager: No state transition config assigned!");
                return;
            }
            
            stateConfig.Initialize();
            enemyManager = Context.EnemyManager;
            activeGenerators = new HashSet<GeneratorBase>();
            OnCurrencyChanged?.Invoke(currency);
            
            if (shouldLoad)
            {
                LoadGameState();
            }
            
            // Initialize stats
            /*TaggedStatsHelper.UpdateWaveStats(currentWave, enemiesRemainingInWave, waveTimer, false);
            TaggedStatsHelper.UpdatePlayerHealth(currentBaseHealth);
            TaggedStatsHelper.UpdateGameTime(gameTime);*/
        }

        /// <summary>
        /// Handles the transition between game states, applying all necessary changes to game systems.
        /// </summary>
        /// <param name="previousState">The state transitioning from</param>
        /// <param name="newState">The state transitioning to</param>
        private void HandleStateTransition(GameState previousState, GameState newState)
        {
            try
            {
                if (stateConfig == null)
                {
                    Debug.LogError("No state transition config assigned!");
                    return;
                }

                GameStateTransitionConfig.GameStateTransitionData transitionData;
                if (!stateConfig.TryGetTransitionData(newState, out transitionData))
                {
                    Debug.LogWarning($"No transition data found for state: {newState}, using default");
                    transitionData = stateConfig.GetDefaultTransition();
                }

                if (transitionData.enableDebugLogging && showDebugStateTransitions)
                {
                    Debug.Log($"Transitioning from {previousState} to {newState}");
                }

                // Handle time scale
                Time.timeScale = transitionData.timeScale;

                // Handle game systems
                HandleGameSystems(transitionData);

                // Handle UI
                HandleUITransition(transitionData);

                // Handle game state
                HandleGameStateTransition(transitionData);

                // Handle audio
                HandleAudioTransition(transitionData);

               

                // Create state snapshots for the event
                var previousStateData = CreateStateSnapshot();
                var newStateData = CreateStateSnapshot();

                // Notify listeners of state change
                OnGameStateChanged?.Invoke(this, new GameStateChangedEventArgs(previousStateData, newStateData));

                if (transitionData.enableDebugLogging && showDebugStateTransitions)
                {
                    Debug.Log($"State transition complete: {previousState} -> {newState}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during state transition: {e.Message}\n{e.StackTrace}");
            }
        }

        private void HandleGameSystems(GameStateTransitionConfig.GameStateTransitionData transitionData)
        {
            try
            {
                // Handle enemy manager
                if (enemyManager != null)
                {
                    enemyManager.enabled = transitionData.pauseEnemySpawning;
                    if (transitionData.clearActiveEnemies)
                    {
                        enemyManager.ClearWave();
                    }
                }

                
                if (transitionData.resetGameState)
                {
                    RestartGame();
                }
                
                // Handle other game systems based on transition data
                if (transitionData.resetGameState)
                {
                    currentBaseHealth = 100f;
                    totalGeneratorHealth = maxTotalGeneratorHealth;
                    
                    if (transitionData.resetGenerators && activeGenerators != null)
                    {
                        foreach (var generator in activeGenerators)
                        {
                            if (generator != null)
                            {
                                generator.ResetFlags();
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling game systems: {e.Message}\n{e.StackTrace}");
            }
        }

        private void HandleUITransition(GameStateTransitionConfig.GameStateTransitionData transitionData)
        {
            var uiManager = Scene?.GetService<UIManager>();
            if (uiManager != null)
            {
                // Close specified views
                foreach (var viewName in transitionData.viewsToClose)
                {
                    var view = uiManager.GetView(viewName);
                    if (view != null)
                    {
                        view.Close(true); // Use instant close during state transitions
                    }
                }

                // Open specified views
                foreach (var viewName in transitionData.viewsToOpen)
                {
                    var view = uiManager.GetView(viewName);
                    if (view != null)
                    {
                        view.Open(true); // Use instant open during state transitions
                    }
                }
            }
        }

        private void HandleGameStateTransition(GameStateTransitionConfig.GameStateTransitionData transitionData)
        {
            try
            {
                // Handle game over state
                if (transitionData.isGameOver && !isGameOver)
                {
                    SetGameOver(true);
                    OnGameOver?.Invoke();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling game state transition: {e.Message}\n{e.StackTrace}");
            }
        }

        private void HandleAudioTransition(GameStateTransitionConfig.GameStateTransitionData transitionData)
        {
            try
            {
                // Handle audio state changes based on transition data
                // TODO: Implement audio transition handling
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling audio transition: {e.Message}\n{e.StackTrace}");
            }
        }

        private GameStateData CreateStateSnapshot()
        {
            return new GameStateData
            {
                currentWave = currentWave,
                score = currentScore,
                baseHealth = currentBaseHealth,
                currency = currency,
                gameTime = gameTime,
                isWaveInProgress = isWaveInProgress
            };
        }

        public void ChangeState(GameState newState)
        {
            if (State == newState) return;

            var previousState = State;
            State = newState;

            HandleStateTransition(previousState, newState);
        }
        
        
        protected override void OnDeinitialize()
        {
            try
            {
                // Clean up enemy manager
                if (enemyManager != null)
                {
                    enemyManager.OnEnemySpawned -= OnEnemySpawned;
                    //enemyManager.OnEnemyKilled -= OnEnemyKilled;
                }

                // Clean up generators
                if (activeGenerators != null)
                {
                    foreach (var generator in activeGenerators.ToArray())
                    {
                        if (generator != null)
                        {
                            generator.OnHealthChanged -= OnGeneratorHealthChanged;
                            generator.OnDestroyed -= OnGeneratorDestroyed;
                        }
                    }
                    activeGenerators.Clear();
                }

                // Clear event listeners
                OnGameStateChanged = null;
                OnBaseHealthChanged = null;
                OnGameOver = null;
                OnWaveTimerChanged = null;
                OnScoreChanged = null;
                OnCurrencyChanged = null;
                OnWaveChanged = null;
                OnWaveStateChanged = null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnDeinitialize: {e.Message}\n{e.StackTrace}");
            }
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

            gameTime += Time.deltaTime;
           // TaggedStatsHelper.UpdateGameTime(gameTime);

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
            
            // Update wave stats
           // TaggedStatsHelper.UpdateWaveStats(currentWave, enemiesRemainingInWave, waveTimer, IsLastWave());
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            enemiesRemainingInWave--;
            // Update wave stats
           // TaggedStatsHelper.UpdateWaveStats(currentWave, enemiesRemainingInWave, waveTimer, IsLastWave());
        }

        /// <summary>
        /// Initializes the game state by loading saved data or setting default values.
        /// </summary>
        public void InitializeGame()
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

        /// <summary>
        /// Notifies all listeners of the current game state.
        /// </summary>
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

        /// <summary>
        /// Loads the main menu scene. This should only be called when returning to the main menu.
        /// Ensures high scores are saved before scene transition.
        /// </summary>
        /// <param name="sceneName">Name of the main menu scene to load</param>
        public void LoadScene(string sceneName)
        {
            try
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogError("GameStateManager: Cannot load scene - scene name is empty");
                    return;
                }

                Debug.Log($"GameStateManager: Loading main menu scene: {sceneName}");
                
                // Save high score before returning to menu
                SaveHighScore();
                
                // Load the menu scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading scene: {e.Message}");
            }
        }

        /// <summary>
        /// Restarts the game by resetting all game systems to their initial state.
        /// This includes game state, UI, generators, enemies, and resources.
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("GameStateManager: Starting game restart process");
            try
            {
                
                // Reset all managers and systems
                ResetAllSystems();
                
                ResetGameState();
                // Notify state changes
                NotifyStateChanges();
                
                Debug.Log("GameStateManager: Game restart complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameStateManager: Error during game restart: {e.Message}\n{e.StackTrace}");
            }
        }

        private void ResetAllSystems()
        {
            Debug.Log("GameStateManager: Resetting all game systems");
            
            try
            {
               

                // Reset other managers
                if (enemyManager != null)
                {
                    Debug.Log("GameStateManager: Resetting enemy manager");
                    enemyManager.enabled = true;
                    enemyManager.ClearWave();
                }
                else
                {
                    Debug.LogError("GameStateManager: EnemyManager not found during reset");
                }

                // Reset game state values
                currentBaseHealth = 100f;
                totalGeneratorHealth = maxTotalGeneratorHealth;
                
                Debug.Log("GameStateManager: All systems reset complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameStateManager: Error resetting systems: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Resets core game state variables to their initial values.
        /// </summary>
        private void ResetGameState()
        {
            // Reset game progress
            currentWave = 0;
            currentScore = 0;
            gameTime = 0f;
            waveTimer = 0f;
            
            // Reset game flags
            isWaveInProgress = false;
            isGameEnding = false;
            isGameOver = false;
            enemiesRemainingInWave = 0;
            
            // Reset resources
            currency = startingCurrency;
            
            // Reset health system
            currentBaseHealth = 100;
            totalGeneratorHealth = 0f;
            maxTotalGeneratorHealth = 0f;
        }

        /// <summary>
        /// Resets all game systems including enemies, generators, and resources.
        /// </summary>
        private void ResetGameSystems()
        {
            // Reset enemy system
            if (enemyManager != null)
            {
                enemyManager.ClearWave();
                enemyManager.enabled = true;
            }

            // Reset generators
            if (activeGenerators != null)
            {
                foreach (var generator in activeGenerators)
                {
                    if (generator != null)
                    {
                        generator.ResetFlags();
                    }
                }
            }

            // Reset resources
            var resourceManager = Scene?.GetService<ResourceManager>();
            if (resourceManager != null)
            {
                //resourceManager.ClearAllResources();
            }
        }

        /// <summary>
        /// Resets all UI elements to their default states
        /// </summary>
        private void ResetUI()
        {
            var uiManager = Scene?.GetService<UIManager>();
            if (uiManager != null)
            {
                // Reset all views
                uiManager.ResetAllViews();

                // Specifically reset PlayerInfoView
                var playerInfoView = uiManager.GetView("PlayerInfoView") as PlayerInfoView;
                if (playerInfoView != null)
                {
                    playerInfoView.ResetUI();
                }

               
            }

            // Notify UI listeners of state changes
            OnCurrencyChanged?.Invoke(currency);
            OnWaveTimerChanged?.Invoke(waveTimer);
            OnScoreChanged?.Invoke(currentScore);
            OnBaseHealthChanged?.Invoke(currentBaseHealth);
            
            if (showDebugInfo)
            {
                Debug.Log("GameStateManager: UI reset complete");
            }
        }

        /// <summary>
        /// Notifies all listeners of state changes after a reset.
        /// </summary>
        private void NotifyStateChanges()
        {
            OnCurrencyChanged?.Invoke(currency);
            OnWaveTimerChanged?.Invoke(waveTimer);
            OnScoreChanged?.Invoke(currentScore);
            OnBaseHealthChanged?.Invoke(currentBaseHealth);
            
            if (showDebugInfo)
            {
                Debug.Log($"GameStateManager: State changes notified - Currency: {currency}, Wave: {currentWave}, Score: {currentScore}");
            }
        }

        /// <summary>
        /// Starts a new wave by resetting the wave timer and spawning enemies.
        /// </summary>
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
            
            // Update wave stats
            //TaggedStatsHelper.UpdateWaveStats(currentWave, enemiesRemainingInWave, waveTimer, IsLastWave());
            
            if (autoSaveEnabled) SaveGameState();
        }

        /// <summary>
        /// Ends the current wave by resetting the wave timer and updating game state.
        /// </summary>
        public void EndWave()
        {
            if (!isWaveInProgress) return;

            SetWaveInProgress(false);
            waveTimer = timeBetweenWaves;
            enemiesRemainingInWave = 0;
            isCheckingWaveEnd = false;
            
            // Update wave stats
            //TaggedStatsHelper.UpdateWaveStats(currentWave, enemiesRemainingInWave, waveTimer, IsLastWave());
            
            if (IsLastWave())
            {
                ChangeState(GameState.Victory);
            }
            
            if (autoSaveEnabled) SaveGameState();
        }

        /// <summary>
        /// Applies damage to the base health and checks for game over conditions.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        public void TakeDamage(float damage)
        {
            if (isGameOver) return;

            if (currentBaseHealth > 0)
            {
                currentBaseHealth = Mathf.Max(0f, currentBaseHealth - damage);
                //UpdateBaseHealthUI();
            }

            /*TaggedStatsHelper.OnEnemyDamageDealt(damage);
            TaggedStatsHelper.UpdatePlayerHealth(currentBaseHealth);*/
            OnBaseHealthChanged?.Invoke(currentBaseHealth);

            if (currentBaseHealth <= 0 && !isGameOver)
            {
                TriggerGameOver();
            }
        }

        /// <summary>
        /// Ends the game by setting the game over flag and notifying listeners.
        /// </summary>
        /// <param name="isGameOver">Whether the game is over</param>
        private void EndGame(bool isGameOver)
        {
            if (State == GameState.GameOver || isGameEnding) return;
            
            isGameEnding = true;

            try
            {
                // Update state first to prevent any race conditions
                SetGameState(GameState.GameOver);
                SetGameOver(true);
                
                // Notify all listeners
                OnGameOver?.Invoke();
                
                // Save the final score
                SaveHighScore();
                
                // Stop game time
                Time.timeScale = 0f;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TriggerGameOver: {e.Message}");
            }
            finally
            {
                isGameEnding = false;
            }
        }

        /// <summary>
        /// Adds currency to the player's resources.
        /// </summary>
        /// <param name="amount">Amount of currency to add</param>
        public void AddCurrency(int amount)
        {
            currency += amount;
            //TaggedStatsHelper.OnResourceEarned("Coins", amount);
            OnCurrencyChanged?.Invoke(currency);
            NotifyStateChanged();
        }

        /// <summary>
        /// Attempts to spend currency from the player's resources.
        /// </summary>
        /// <param name="amount">Amount of currency to spend</param>
        /// <returns>Whether the currency was spent successfully</returns>
        public bool TrySpendCurrency(int amount)
        {
            if (currency >= amount)
            {
                currency -= amount;
                TaggedStatsHelper.OnResourceSpent(amount);
                OnCurrencyChanged?.Invoke(currency);
                NotifyStateChanged();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the current wave is the last wave.
        /// </summary>
        /// <returns>Whether the current wave is the last wave</returns>
        private bool IsLastWave()
        {
            return currentWave >= 50;
        }

        /// <summary>
        /// Saves the high score to the player's preferences.
        /// </summary>
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

        /// <summary>
        /// Saves the current game state to a file.
        /// </summary>
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
                Debug.LogError($"Failed to save game state: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Loads the saved game state from a file.
        /// </summary>
        /// <returns>Whether the game state was loaded successfully</returns>
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
                Debug.LogError($"Failed to load game state: {e.Message}\n{e.StackTrace}");
            }
            return false;
        }

        /// <summary>
        /// Deletes the saved game state file.
        /// </summary>
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
                Debug.LogError($"Failed to delete save game: {e.Message}\n{e.StackTrace}");
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
        

        /// <summary>
        /// Sets the game over flag and notifies listeners.
        /// </summary>
        /// <param name="value">Whether the game is over</param>
        private void SetGameOver(bool value)
        {
            if (isGameOver != value)
            {
                Debug.Log($"GameStateManager: Game over state changed to {value}");
                isGameOver = value;
                OnGameOverChanged?.Invoke(isGameOver);
                //NotifyStateChanged();
            }
        }

        /// <summary>
        /// Sets the wave in progress flag and notifies listeners.
        /// </summary>
        /// <param name="value">Whether the wave is in progress</param>
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

        /// <summary>
        /// Sets the current wave number and notifies listeners.
        /// </summary>
        /// <param name="wave">New wave number</param>
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

        /// <summary>
        /// Adds score to the player's resources and notifies listeners.
        /// </summary>
        /// <param name="amount">Amount of score to add</param>
        private void AddScore(int amount)
        {
            if (amount != 0)
            {
                currentScore += amount;
                OnScoreChanged?.Invoke(currentScore);
                NotifyStateChanged();
            }
        }

 

       

      

        /// <summary>
        /// Registers a generator with the game state manager.
        /// </summary>
        /// <param name="generator">Generator to register</param>
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

        private void OnGeneratorHealthChanged(float healthPercentage)
        {
            UpdateTotalGeneratorHealth();
        }
        
        /// <summary>
        /// Unregisters a generator from the game state manager.
        /// </summary>
        /// <param name="generator">Generator to unregister</param>
        public void UnregisterGenerator(GeneratorBase generator)
        {
            // Early exit if any object is being destroyed
            if (!this || !gameObject || !isActiveAndEnabled || generator == null || !generator.gameObject)
            {
                return;
            }

            try
            {
                if (activeGenerators.Contains(generator))
                {
                    activeGenerators.Remove(generator);
                    generator.OnHealthChanged -= OnGeneratorHealthChanged;
                    generator.OnDestroyed -= OnGeneratorDestroyed;

                    // Cache the current health values
                    maxTotalGeneratorHealth -= generator.MaxHealth;
                    totalGeneratorHealth -= generator.CurrentHealth;

                    // Only update UI if we're not being destroyed
                    if (this && gameObject && isActiveAndEnabled)
                    {
                        // Check if this was the last generator
                        bool anyActiveGenerators = activeGenerators.Any(g => g != null && !g.IsDestroyed);
                        if (!anyActiveGenerators && !isGameEnding)
                        {
                            TriggerGameOver();
                        }
                        else
                        {
                            UpdateBaseHealthUI();
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UnregisterGenerator: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Updates the total generator health and notifies listeners.
        /// </summary>
        private void UpdateTotalGeneratorHealth()
        {
            // Early exit if we're being destroyed
            if (!this || !gameObject || !isActiveAndEnabled)
            {
                return;
            }

            try
            {
                float newTotalHealth = 0f;
                float newMaxTotalHealth = 0f;

                // Calculate new totals from active generators
                foreach (var generator in activeGenerators.ToList())
                {
                    if (generator != null && !generator.IsDestroyed)
                    {
                        newTotalHealth += generator.CurrentHealth;
                        newMaxTotalHealth += generator.MaxHealth;
                    }
                }

                // Update the cached values
                totalGeneratorHealth = newTotalHealth;
                maxTotalGeneratorHealth = newMaxTotalHealth;

                // Only update UI if we're not being destroyed
                if (this && gameObject && isActiveAndEnabled)
                {
                    UpdateBaseHealthUI();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UpdateTotalGeneratorHealth: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Updates the base health UI and notifies listeners.
        /// </summary>
        private void UpdateBaseHealthUI()
        {
            try
            {
                // Check if we're being destroyed
                if (!this || !gameObject || !isActiveAndEnabled)
                {
                    return;
                }

                float totalHealth = currentBaseHealth + totalGeneratorHealth;
                float maxTotalHealth = baseHealth + maxTotalGeneratorHealth;
                
                // Convert to percentage and ensure it's not negative
                float healthPercentage = Mathf.Max(0f, (totalHealth / maxTotalHealth) * 100f);
                
                
               
                // Notify UI only if we have valid listeners and we're not being destroyed
                if (OnBaseHealthChanged != null && this && gameObject && isActiveAndEnabled)
                {
                    OnBaseHealthChanged.Invoke(healthPercentage);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UpdateBaseHealthUI: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Called when a generator is destroyed.
        /// </summary>
        private void OnGeneratorDestroyed()
        {

            try
            {
                // Update health calculations immediately if we're not being destroyed
                UpdateTotalGeneratorHealth();
                
                // Check if this was the last generator
                bool anyActiveGenerators = false;
                foreach (var gen in activeGenerators)
                {
                    if (gen != null && !gen.IsDestroyed)
                    {
                        anyActiveGenerators = true;
                        break;
                    }
                }
            
                Debug.Log("Any Active Generators? " + anyActiveGenerators);
                if (!anyActiveGenerators && !isGameEnding)
                {
                    TriggerGameOver();
                }

            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnGeneratorDestroyed: {e.Message}\n{e.StackTrace}");
            }
        }

  


        /// <summary>
        /// Triggers the game over sequence.
        /// </summary>
        private void TriggerGameOver()
        {
            Debug.Log("GAME OVER TRIGGERED!");
            ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Sets the game state and notifies listeners.
        /// </summary>
        /// <param name="newState">New game state</param>
        private void SetGameState(GameState newState)
        {
            if (State == newState) return;
            
            var previousData = new GameStateData
            {
                currentWave = currentWave,
                score = currentScore,
                baseHealth = currentBaseHealth,
                currency = currency,
                gameTime = gameTime,
                isWaveInProgress = isWaveInProgress
            };
            
            State = newState;
            
            var currentData = new GameStateData
            {
                currentWave = currentWave,
                score = currentScore,
                baseHealth = currentBaseHealth,
                currency = currency,
                gameTime = gameTime,
                isWaveInProgress = isWaveInProgress
            };
            
            OnGameStateChanged?.Invoke(this, new GameStateChangedEventArgs(previousData, currentData));
        }

        public void StartNewGame()
        {
            // Reset game state
            currentWave = 0;
            currentScore = 0;
            currentBaseHealth = baseHealth;
            currency = startingCurrency;
            waveTimer = timeBetweenWaves;
            isWaveInProgress = false;
            gameTime = 0f;
            isGameOver = false;
            
            // Initialize starting stats
            /*TaggedStatsHelper.UpdatePlayerHealth(currentBaseHealth);
            TaggedStatsHelper.UpdateGameTime(gameTime);
            TaggedStatsHelper.UpdateWaveStats(
                currentWave: currentWave,
                enemiesRemaining: 0,
                timeUntilNext: waveTimer,
                isFinal: false
            );
            */

            // Change state to playing
            ChangeState(GameState.Playing);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            gameTime += Time.deltaTime;
            //TaggedStatsHelper.UpdateGameTime(gameTime);

            if (isWaveInProgress)
            {
                // Update wave stats
                /*TaggedStatsHelper.UpdateWaveStats(
                    currentWave: currentWave,
                    enemiesRemaining: enemiesRemainingInWave,
                    timeUntilNext: 0,
                    isFinal: IsLastWave()
                );*/
            }
            else
            {
                waveTimer -= Time.deltaTime;
                /*TaggedStatsHelper.UpdateWaveStats(
                    currentWave: currentWave,
                    enemiesRemaining: enemiesRemainingInWave,
                    timeUntilNext: waveTimer,
                    isFinal: IsLastWave()
                );*/
                OnWaveTimerChanged?.Invoke(waveTimer);
            }
        }

        /*public void TakeDamage(float damage)
        {
            if (isGameOver) return;

            currentBaseHealth = Mathf.Max(0f, currentBaseHealth - damage);
            OnBaseHealthChanged?.Invoke(currentBaseHealth / baseHealth);

            TaggedStatsHelper.SetPlayerHealth(currentBaseHealth);

            if (currentBaseHealth <= 0)
            {
                GameOver(false);
            }
        }*/

        private void GameOver(bool victory)
        {
            if (isGameOver) return;

            isGameOver = true;
            State = victory ? GameState.Victory : GameState.GameOver;
            
            // Update final stats before game over
            /*TaggedStatsHelper.UpdateWaveStats(
                currentWave: currentWave,
                enemiesRemaining: enemiesRemainingInWave,
                timeUntilNext: 0,
                isFinal: true
            );*/

            OnGameOverChanged?.Invoke(victory);
            OnGameOver?.Invoke();
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace Planetarium
{
    [CreateAssetMenu(fileName = "GameStateTransitionConfig", menuName = "Planetarium/Game State/Transition Config")]
    public class GameStateTransitionConfig : ScriptableObject
    {
        [System.Serializable]
        public class GameStateTransitionData
        {
            [Header("State Settings")]
            [Tooltip("The game state this transition configuration applies to")]
            public GameState state;

            [Tooltip("Time scale to set when entering this state")]
            [Range(0f, 1f)]
            public float timeScale = 1f;

            [Header("Game Systems")]
            [Tooltip("Whether to pause enemy spawning in this state")]
            public bool pauseEnemySpawning;

            [Tooltip("Whether to clear active enemies when entering this state")]
            public bool clearActiveEnemies;

            [Tooltip("Whether to save the high score when entering this state")]
            public bool saveHighScore;

            [Tooltip("Whether to reset the wave timer when entering this state")]
            public bool resetWaveTimer;

            [Tooltip("Whether to reset generators when entering this state")]
            public bool resetGenerators;

            [Tooltip("Whether to clear all resources when entering this state")]
            public bool clearResources;

            [Tooltip("Whether the game is now over")]
            public bool isGameOver;

            [Header("UI Management")]
            [Tooltip("Views to automatically close when entering this state")]
            public string[] viewsToClose;

            [Tooltip("Views to automatically open when entering this state")]
            public string[] viewsToOpen;

            [Header("Game State")]
            [Tooltip("Whether to trigger game initialization when entering this state")]
            public bool triggerInitialize;

            [Tooltip("Whether to trigger cleanup when entering this state")]
            public bool triggerCleanup;

            [Tooltip("Whether to reset game state variables when entering this state")]
            public bool resetGameState;

            [Header("Audio")]
            [Tooltip("Whether to pause audio when entering this state")]
            public bool pauseAudio;

            [Tooltip("Background music track to play in this state")]
            public string musicTrack;

            [Header("Debug")]
            [Tooltip("Whether to log debug information for this state transition")]
            public bool enableDebugLogging = true;
        }

        [Header("State Transitions")]
        [Tooltip("List of state transition configurations")]
        [SerializeField] private GameStateTransitionData[] stateTransitions;

        private Dictionary<GameState, GameStateTransitionData> transitionLookup;

        /// <summary>
        /// Initializes the transition lookup dictionary for efficient state access.
        /// </summary>
        public void Initialize()
        {
            transitionLookup = new Dictionary<GameState, GameStateTransitionData>();
            foreach (var transition in stateTransitions)
            {
                if (!transitionLookup.ContainsKey(transition.state))
                {
                    transitionLookup.Add(transition.state, transition);
                }
                else
                {
                    Debug.LogWarning($"Duplicate state transition found for {transition.state}");
                }
            }
        }

        /// <summary>
        /// Attempts to get transition data for a specific game state.
        /// </summary>
        /// <param name="state">The game state to get transition data for</param>
        /// <param name="data">The transition data if found</param>
        /// <returns>True if transition data was found, false otherwise</returns>
        public bool TryGetTransitionData(GameState state, out GameStateTransitionData data)
        {
            if (transitionLookup == null)
            {
                Initialize();
            }
            return transitionLookup.TryGetValue(state, out data);
        }

        /// <summary>
        /// Gets the default transition data for when no specific configuration is found.
        /// </summary>
        /// <returns>Default transition data</returns>
        public GameStateTransitionData GetDefaultTransition()
        {
            return new GameStateTransitionData
            {
                timeScale = 1f,
                pauseEnemySpawning = true,
                clearActiveEnemies = false,
                saveHighScore = false,
                resetWaveTimer = false,
                resetGenerators = false,
                clearResources = false,
                viewsToClose = new string[0],
                viewsToOpen = new string[0],
                triggerInitialize = false,
                triggerCleanup = false,
                resetGameState = false,
                pauseAudio = false,
                enableDebugLogging = true
            };
        }
    }
}

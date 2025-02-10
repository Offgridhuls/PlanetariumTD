using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Planetarium.UI
{
    public class PlayerInfoView : UIView
    {
        [Header("UI References")]
        [SerializeField] private Image planetHealthBarFill;
        [SerializeField] private TextMeshProUGUI waveNumberText;
        [SerializeField] private TextMeshProUGUI nextWaveTimerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI planetHealthText;
        [SerializeField] private Gradient healthBarGradient;
        [SerializeField] private Button startWaveEarlyButton;
        [SerializeField] private GameObject waveTimerContainer;

        private GameStateManager gameState;
        private float displayedHealthPercent;

        private void OnValidate()
        {
            if (planetHealthBarFill == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: planetHealthBarFill is not assigned!");
            if (waveNumberText == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: waveNumberText is not assigned!");
            if (nextWaveTimerText == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: nextWaveTimerText is not assigned!");
            if (scoreText == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: scoreText is not assigned!");
            if (playerLevelText == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: playerLevelText is not assigned!");
            if (planetHealthText == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: planetHealthText is not assigned!");
            if (healthBarGradient == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: healthBarGradient is not assigned!");
            //if (waveTimerContainer == null)
            //    Debug.LogError($"PlayerInfoView on {gameObject.name}: waveTimerContainer is not assigned!");
        }

        protected override void OnInitialize()
        {
            try
            {
                base.OnInitialize();

                ValidateReferences();

                // Get GameStateManager reference
                gameState = Context.GameState;
                if (gameState != null)
                {
                    // Subscribe to events
                    gameState.OnBaseHealthChanged += UpdatePlanetHealth;
                    gameState.OnWaveChanged += UpdateWave;
                    gameState.OnWaveTimerChanged += UpdateWaveTimer;
                    gameState.OnScoreChanged += UpdateScore;
                    gameState.OnPlayerLevelChanged += SetPlayerLevel;
                    gameState.OnWaveStateChanged += UpdateWaveState;
                }
                else
                {
                    Debug.LogError("PlayerInfoView: Failed to get GameStateManager reference!");
                }

                if (startWaveEarlyButton != null)
                {
                    startWaveEarlyButton.onClick.AddListener(OnStartWaveEarlyClicked);
                }

                ResetUI();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnInitialize: {e.Message}\n{e.StackTrace}");
            }
        }

        protected override void OnDeinitialize()
        {
            try
            {
                if (gameState != null)
                {
                    // Unsubscribe from events
                    gameState.OnBaseHealthChanged -= UpdatePlanetHealth;
                    gameState.OnWaveChanged -= UpdateWave;
                    gameState.OnWaveTimerChanged -= UpdateWaveTimer;
                    gameState.OnScoreChanged -= UpdateScore;
                    gameState.OnPlayerLevelChanged -= SetPlayerLevel;
                    gameState.OnWaveStateChanged -= UpdateWaveState;
                }

                if (startWaveEarlyButton != null)
                {
                    startWaveEarlyButton.onClick.RemoveListener(OnStartWaveEarlyClicked);
                }

                // Clear references
                planetHealthText = null;
                planetHealthBarFill = null;
                waveNumberText = null;
                nextWaveTimerText = null;
                scoreText = null;
                playerLevelText = null;
                healthBarGradient = null;
                gameState = null;
                startWaveEarlyButton = null;
                waveTimerContainer = null;

                base.OnDeinitialize();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnDeinitialize: {e.Message}\n{e.StackTrace}");
            }
        }

        private void ValidateReferences()
        {
            if (planetHealthBarFill == null) Debug.LogError($"PlayerInfoView: planetHealthBarFill is null on {gameObject.name}!");
            if (waveNumberText == null) Debug.LogError($"PlayerInfoView: waveNumberText is null on {gameObject.name}!");
            if (nextWaveTimerText == null) Debug.LogError($"PlayerInfoView: nextWaveTimerText is null on {gameObject.name}!");
            if (scoreText == null) Debug.LogError($"PlayerInfoView: scoreText is null on {gameObject.name}!");
            if (playerLevelText == null) Debug.LogError($"PlayerInfoView: playerLevelText is null on {gameObject.name}!");
            if (planetHealthText == null) Debug.LogError($"PlayerInfoView: planetHealthText is null on {gameObject.name}!");
            if (healthBarGradient == null) Debug.LogError($"PlayerInfoView: healthBarGradient is null on {gameObject.name}!");
            if (waveTimerContainer == null) Debug.LogError($"PlayerInfoView: waveTimerContainer is null on {gameObject.name}!");
        }

        /// <summary>
        /// Resets all UI elements to their default state
        /// </summary>
        public void ResetUI()
        {
            try
            {
                // Reset health display
                if (planetHealthBarFill != null)
                {
                    planetHealthBarFill.fillAmount = 1f;
                    planetHealthBarFill.color = healthBarGradient.Evaluate(1f);
                }
                
                if (planetHealthText != null)
                {
                    planetHealthText.text = "100%";
                }

                // Reset wave display
                if (waveNumberText != null)
                {
                    waveNumberText.text = "Wave 1";
                }

                // Reset timer
                if (nextWaveTimerText != null)
                {
                    nextWaveTimerText.text = "00:00";
                }

                // Reset score
                if (scoreText != null)
                {
                    scoreText.text = "Score: 0";
                }

                // Reset player level
                if (playerLevelText != null)
                {
                    playerLevelText.text = "Level 1";
                }

                // Reset wave timer container
                if (waveTimerContainer != null)
                {
                    waveTimerContainer.SetActive(true);
                }

                // Reset start wave button
                if (startWaveEarlyButton != null)
                {
                    startWaveEarlyButton.gameObject.SetActive(true);
                }

                displayedHealthPercent = 1f;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting PlayerInfoView UI: {e.Message}");
            }
        }

        public void UpdatePlanetHealth(float currentHealth)
        {
            // Early exit if being destroyed or disabled
            if (!this || !gameObject || !isActiveAndEnabled)
            {
                return;
            }

            try
            {
                if (planetHealthText != null && gameState != null)
                {
                    planetHealthText.text = $"{Mathf.CeilToInt(currentHealth)}/{gameState.MaxBaseHealth}";
                }

                float healthPercent = gameState != null ? 
                    Mathf.Clamp01(currentHealth / gameState.MaxBaseHealth) : 0f;

                if (planetHealthBarFill != null && this && gameObject && isActiveAndEnabled)
                {
                    planetHealthBarFill.fillAmount = healthPercent;
                    if (healthBarGradient != null)
                    {
                        planetHealthBarFill.color = healthBarGradient.Evaluate(healthPercent);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UpdatePlanetHealth: {e.Message}\n{e.StackTrace}");
            }
        }

        private void UpdateWave(int wave)
        {
            Debug.Log($"PlayerInfoView: UpdateWave called with wave: {wave} on {gameObject.name}");
            if (waveNumberText != null)
            {
                waveNumberText.text = $"Wave {wave}";
            }
        }

        private void UpdateWaveTimer(float time)
        {
           // Debug.Log($"PlayerInfoView: UpdateWaveTimer called with time: {time} on {gameObject.name}");
            if (nextWaveTimerText != null)
            {
                if (time <= 0)
                {
                    nextWaveTimerText.text = "Wave Incoming!";
                }
                else
                {
                    int seconds = Mathf.CeilToInt(time);
                    nextWaveTimerText.text = $"Next Wave: {seconds}s";
                }
            }
        }

        private void UpdateScore(int newScore)
        {
            //Debug.Log($"PlayerInfoView: UpdateScore called with score: {newScore} on {gameObject.name}");
            if (scoreText != null)
            {
                scoreText.text = $"Score: {newScore}";
            }
        }

        private void SetPlayerLevel(int level)
        {
            Debug.Log($"PlayerInfoView: SetPlayerLevel called with level: {level} on {gameObject.name}");
            if (playerLevelText != null)
            {
                playerLevelText.text = $"Level {level}";
               // PlaySound(Context.GetAudioSetup("LevelUp"));
            }
        }

        private void UpdateWaveState(bool isWaveActive)
        {
            Debug.Log($"PlayerInfoView: UpdateWaveState called with state: {isWaveActive} on {gameObject.name}");
            if (waveTimerContainer != null)
            {
                waveTimerContainer.SetActive(!isWaveActive);
            }

            if (startWaveEarlyButton != null)
            {
                startWaveEarlyButton.gameObject.SetActive(!isWaveActive);
            }
        }

        private void OnStartWaveEarlyClicked()
        {
            Debug.Log($"PlayerInfoView: OnStartWaveEarlyClicked on {gameObject.name}");
            PlayClickSound();
            if (gameState != null)
            {
               // gameState.StartWaveEarly();
            }
        }
    }
}

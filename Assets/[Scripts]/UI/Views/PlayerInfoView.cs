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
            if (waveTimerContainer == null)
                Debug.LogError($"PlayerInfoView on {gameObject.name}: waveTimerContainer is not assigned!");
        }

        protected override void OnInitialize()
        {
            Debug.Log($"PlayerInfoView: OnInitialize called on {gameObject.name}");
            base.OnInitialize();

            if (Context == null)
            {
                Debug.LogError($"PlayerInfoView: Context is null on {gameObject.name}!");
                return;
            }

            gameState = Context.GameState;

            if (gameState != null)
            {
                Debug.Log($"PlayerInfoView: Found GameState, subscribing to events on {gameObject.name}");
                UpdatePlanetHealth(gameState.GetBaseHealth());
                UpdateWave(gameState.GetCurrentWave());
                UpdateWaveTimer(gameState.GetWaveTimer());
                UpdateScore(gameState.CurrentScore);
                //SetPlayerLevel(gameState.PlayerLevel);

                gameState.OnBaseHealthChanged += UpdatePlanetHealth;
                gameState.OnWaveChanged += UpdateWave;
                gameState.OnWaveTimerChanged += UpdateWaveTimer;
                gameState.OnScoreChanged += UpdateScore;
                gameState.OnPlayerLevelChanged += SetPlayerLevel;
                gameState.OnWaveStateChanged += UpdateWaveState;
            }
            else
            {
                Debug.LogError($"PlayerInfoView: GameState is null on {gameObject.name}!");
            }

            if (startWaveEarlyButton != null)
            {
                startWaveEarlyButton.onClick.AddListener(OnStartWaveEarlyClicked);
            }

            ValidateReferences();
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

        protected override void OnDeinitialize()
        {
            Debug.Log($"PlayerInfoView: OnDeinitialize called on {gameObject.name}");
            base.OnDeinitialize();

            if (gameState != null)
            {
                Debug.Log($"PlayerInfoView: Unsubscribing from events on {gameObject.name}");
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
        }

        private void UpdatePlanetHealth(float currentHealth)
        {
            Debug.Log($"PlayerInfoView: UpdatePlanetHealth called with health: {currentHealth} on {gameObject.name}");
            if (planetHealthText != null)
            {
                planetHealthText.text = $"{Mathf.CeilToInt(currentHealth)}/{gameState.MaxBaseHealth}";
            }

            float healthPercent = Mathf.Clamp01(currentHealth / gameState.MaxBaseHealth);

            if (planetHealthBarFill != null)
            {
                planetHealthBarFill.fillAmount = healthPercent;
                if (healthBarGradient != null)
                {
                    planetHealthBarFill.color = healthBarGradient.Evaluate(healthPercent);
                }
                Debug.Log($"PlayerInfoView: Health bar updated - Fill: {healthPercent}, Color: {planetHealthBarFill.color} on {gameObject.name}");
            }
            else
            {
                Debug.LogError($"PlayerInfoView: planetHealthBarFill is null during health update on {gameObject.name}!");
            }

            // Play warning sound if health is low
            if (healthPercent <= 0.25f)
            {
               // PlaySound(Context.GetAudioSetup("LowHealthWarning"));
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
            Debug.Log($"PlayerInfoView: UpdateScore called with score: {newScore} on {gameObject.name}");
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

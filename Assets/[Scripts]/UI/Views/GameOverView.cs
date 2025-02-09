using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Planetarium.UI;

namespace Planetarium.UI.Views
{
    public class GameOverView : UIView
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI wavesSurvivedText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Animation")]
        [SerializeField] private float elementDelay = 0.2f;
        [SerializeField] private Animator viewAnimator;
        
        [Header("Scene Loading")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "Game";
        
        private GameStateManager gameState;
        
        public event Action OnRestartRequested;
        public event Action OnMainMenuRequested;

        protected void Awake()
        {
      
           
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            try
            {
                gameState = Context.GameState;
                
                if (restartButton != null)
                    restartButton.onClick.AddListener(HandleRestart);
                
                if (mainMenuButton != null)
                    mainMenuButton.onClick.AddListener(HandleMainMenu);
                
                // Ensure the view is visible
                if (gameObject != null)
                {
                    gameObject.SetActive(true);
                }
                
                // Update UI state
                Show();
                
                Debug.Log("GameOverView initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing GameOverView: {e.Message}");
            }
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            try
            {
                if (restartButton != null)
                    restartButton.onClick.RemoveListener(HandleRestart);
                
                if (mainMenuButton != null)
                    mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deinitializing GameOverView: {e.Message}");
            }
        }

        public void Show()
        {
            try
            {
                if (gameState == null)
                {
                    Debug.LogError("GameOverView: GameState is null");
                    return;
                }
                
                // Update UI elements
                if (wavesSurvivedText != null)
                    wavesSurvivedText.text = $"Waves Survived: {gameState.CurrentWave}";
                
                if (scoreText != null)
                    scoreText.text = $"Score: {gameState.CurrentScore:N0}";
                
                if (highScoreText != null)
                {
                    int highScore = PlayerPrefs.GetInt("HighScore", 0);
                    if (gameState.CurrentScore > highScore)
                    {
                        highScore = gameState.CurrentScore;
                        PlayerPrefs.SetInt("HighScore", highScore);
                        PlayerPrefs.Save();
                    }
                    highScoreText.text = $"High Score: {highScore:N0}";
                }

                // Ensure view is visible
                if (gameObject != null && !gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
                
                // Play animation if available
                if (viewAnimator != null)
                {
                    viewAnimator.SetTrigger("Show");
                }
                
                Debug.Log("GameOverView shown");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing GameOverView: {e.Message}");
            }
        }

        private void HandleRestart()
        {
            try
            {
                if (gameState == null)
                {
                    Debug.LogError("GameOverView: Cannot restart - GameState is null");
                    return;
                }

                // Close this view
                Close(true);
                
                // Restart the game using the new method
                gameState.RestartGame();
                
                Debug.Log("Game restart initiated");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error restarting game: {e.Message}");
            }
        }

        private void HandleMainMenu()
        {
            try
            {
                if (gameState == null)
                {
                    Debug.LogError("GameOverView: Cannot return to menu - GameState is null");
                    return;
                }

                if (string.IsNullOrEmpty(mainMenuSceneName))
                {
                    Debug.LogError("GameOverView: Cannot return to menu - main menu scene name not set");
                    return;
                }

                // Close this view
                Close(true);
                
                // Load the main menu scene
                gameState.LoadScene(mainMenuSceneName);
                
                Debug.Log($"Loading main menu scene: {mainMenuSceneName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error returning to main menu: {e.Message}");
            }
        }

        protected void OnDestroy()
        {
            try
            {
                if (restartButton != null)
                    restartButton.onClick.RemoveListener(HandleRestart);
                
                if (mainMenuButton != null)
                    mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cleaning up GameOverView: {e.Message}");
            }
        }
    }
}

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
        
        private GameStateManager gameState;
        
        public event Action OnRestartRequested;
        public event Action OnMainMenuRequested;

        protected void Awake()
        {
      
           
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            gameState = Context.GameState;
            
            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestart);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(HandleMainMenu);
            
            
            Show();
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
          
        }
        public void Show()
        {
            
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

         
        }

        private void HandleRestart()
        {
            //TODO: Maybe just handle the logic here instead of this..
            OnRestartRequested?.Invoke();
        }

        private void HandleMainMenu()
        {
            //TODO: Maybe just handle the logic here instead of this..
            OnMainMenuRequested?.Invoke();
        }

        protected void OnDestroy()
        {
        
            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestart);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(HandleMainMenu);
        }
    }
}

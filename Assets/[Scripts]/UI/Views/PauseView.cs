using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium.UI
{
    public class PauseView : UIView
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button closeSettingsButton;

        private bool wasGamePaused;

        protected void Awake()
        {
          
            
            resumeButton.onClick.AddListener(OnResumeClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);

            // Initialize settings panel
            settingsPanel.SetActive(false);
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
            Time.timeScale = 0f;
            wasGamePaused = true;
        }

        public override void Close(bool instant = false)
        {
            if (wasGamePaused)
            {
                Time.timeScale = 1f;
                wasGamePaused = false;
            }
            settingsPanel.SetActive(false);
            base.Close(instant);
        }

        private void OnResumeClicked()
        {
            Close();
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            // Add your scene reload logic here
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnSettingsClicked()
        {
            settingsPanel.SetActive(true);
            // Load and set current volume values
            // You'll need to implement audio management
            // musicVolumeSlider.value = AudioManager.MusicVolume;
            // sfxVolumeSlider.value = AudioManager.SFXVolume;
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            // Add your main menu scene load logic here
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnCloseSettingsClicked()
        {
            settingsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Ensure time scale is restored if view is destroyed while game is paused
            if (wasGamePaused)
            {
                Time.timeScale = 1f;
            }
        }
    }
}

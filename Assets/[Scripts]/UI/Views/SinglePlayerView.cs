using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Planetarium.SaveSystem;
using Planetarium.Core.Camera;
using Planetarium.Stats;

namespace Planetarium.UI
{
    public class SinglePlayerView : UIView
    {
        [Header("Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button backButton;

        [Header("Game Settings")]
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private GameObject saveSlotPrefab;
        [SerializeField] private Transform saveSlotContainer;

        [Header("Scene")]
        [SerializeField] private Transform focusPoint;

        [Header("Planet Tags")]
        [SerializeField] private PlanetTagVisualizerInteractions _planetTagVisualizer;
        [SerializeField] private KeyCode _togglePlanetTagsKey = KeyCode.F3;
        [SerializeField] private bool _debugMode = true;

        private SaveManager _saveManager;
        private CameraStateService _cameraService;
        private GameObject _currentSaveSlotList;
        private bool _planetVisualizersEnabled = true;
        private bool _initialized = false;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            LogDebug("Initializing SinglePlayerView");

            _saveManager = Context.scene.GetService<SaveManager>();
            _cameraService = Context.scene.GetService<CameraStateService>();

            if (!_initialized)
            {
                InitializeView();
            }

            // Setup button listeners
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(() => {
                    PlayClickSound();
                    StartNewGame();
                });
            }

            if (loadGameButton != null)
            {
                loadGameButton.onClick.AddListener(() => {
                    PlayClickSound();
                    ShowSaveSlots();
                });
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => {
                    PlayClickSound();
                    HideSaveSlots();
                    UIManager.TransitionToView<MainMenuView>(this);
                });
            }

            // Setup dropdown if available
            if (difficultyDropdown != null)
            {
                difficultyDropdown.ClearOptions();
                difficultyDropdown.AddOptions(new[] { "Easy", "Normal", "Hard" }.ToList());
            }

            // Create focus point if not set
            if (focusPoint == null)
            {
                focusPoint = new GameObject("CameraFocusPoint").transform;
                focusPoint.position = Vector3.zero;
            }
        }

        private void InitializeView()
        {
            LogDebug("Initializing view state");
            _initialized = true;

            if (_planetTagVisualizer != null)
            {
                _planetTagVisualizer.gameObject.SetActive(true);
                _planetVisualizersEnabled = true;
                _planetTagVisualizer.SetCamera(Context.MainCamera);

                // Find all planets and create visualizers
                var planets = GameObject.FindGameObjectsWithTag("Planet")
                    .Select(p => p.GetComponent<TaggedComponent>())
                    .Where(tc => tc != null);

                foreach (var planet in planets)
                {
                    _planetTagVisualizer.CreateVisualizerFor(planet);
                    LogDebug($"Created visualizer for planet: {planet.gameObject.name}");
                }
            }
            else
            {
                LogDebug("Warning: Planet tag visualizer is not assigned!");
            }

            LogDebug("View initialization complete");
        }

        private void Update()
        {
            base.OnTick();

            // Handle toggle key for planet tags
            if (Input.GetKeyDown(_togglePlanetTagsKey))
            {
                TogglePlanetVisualizers();
            }
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
            LogDebug("Opening SinglePlayerView");

            if (useAnimation)
            {
                AnimateElements(true);
            }

            // Transition to interactive camera state
            if (_cameraService != null)
            {
                _cameraService.TransitionToState(CameraState.Interactive, focusPoint.position);
                LogDebug("Transitioned to interactive camera state");
            }

            // Enable planet tag visualization
            if (_planetTagVisualizer != null)
            {
                _planetTagVisualizer.gameObject.SetActive(true);
                _planetTagVisualizer.SetCamera(Context.MainCamera);
            }

            if (_saveManager == null)
            {
                _saveManager = Context.scene.GetService<SaveManager>();
            }
            
            // Update player name from last save if available
            if (_saveManager.HasActiveSave && !string.IsNullOrEmpty(_saveManager.CurrentSave.playerName))
            {
                if (playerNameInput != null)
                    playerNameInput.text = _saveManager.CurrentSave.playerName;
            }
        }

        public override void Close(bool instant = false)
        {
            base.Close(instant);

            if (useAnimation)
            {
                AnimateElements(false);
            }

            // Return camera to orbit state for main menu
            if (_cameraService != null)
            {
                _cameraService.TransitionToState(CameraState.Orbit, Vector3.zero);
            }

            // Disable planet tag visualization
            if (_planetTagVisualizer != null)
            {
                _planetTagVisualizer.gameObject.SetActive(false);
            }

            HideSaveSlots();
        }

        private void AnimateElements(bool opening)
        {
            if (layoutContainer == null) return;

            float delay = 0f;
            foreach (Transform child in layoutContainer)
            {
                if (opening)
                {
                    // Setup initial state
                    child.localScale = Vector3.zero;
                    // Animate in
                    child.DOScale(Vector3.one, animationDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutBack);
                }
                else
                {
                    // Animate out
                    child.DOScale(Vector3.zero, animationDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.InBack);
                }
                delay += 0.1f;
            }
        }

        private async void StartNewGame()
        {
            string playerName = playerNameInput != null ? playerNameInput.text : "Player";
            if (string.IsNullOrEmpty(playerName))
            {
                // TODO: Show error message
                Debug.LogWarning("Player name cannot be empty");
                return;
            }

            // Create new game save
            _saveManager.CreateNewGame(playerName);
            
            // TODO: Transition to game scene
            Debug.Log($"Starting new game with player: {playerName}");
        }

        private void ShowSaveSlots()
        {
            HideSaveSlots();

            if (saveSlotContainer != null && saveSlotPrefab != null)
            {
                _currentSaveSlotList = new GameObject("SaveSlotList");
                _currentSaveSlotList.transform.SetParent(saveSlotContainer, false);

                var saves = _saveManager.GetAvailableSaves();
                foreach (var save in saves)
                {
                    var slot = Instantiate(saveSlotPrefab, _currentSaveSlotList.transform);
                    SetupSaveSlot(slot, save);
                }
            }
        }

        private void HideSaveSlots()
        {
            if (_currentSaveSlotList != null)
            {
                Destroy(_currentSaveSlotList);
                _currentSaveSlotList = null;
            }
        }

        private void SetupSaveSlot(GameObject slotObject, SaveFileInfo saveInfo)
        {
            var nameText = slotObject.GetComponentInChildren<TextMeshProUGUI>();
            var loadButton = slotObject.GetComponentInChildren<Button>();
            var deleteButton = slotObject.transform.Find("DeleteButton")?.GetComponent<Button>();

            if (nameText != null)
            {
                nameText.text = $"{saveInfo.playerName} - {saveInfo.lastSaveTime:g}\nPlanet Systems: {saveInfo.unlockedSystems}";
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(async () => {
                    PlayClickSound();
                    if (await _saveManager.LoadGame(saveInfo.fileName))
                    {
                        // TODO: Transition to game scene
                        Debug.Log($"Loaded game: {saveInfo.fileName}");
                    }
                });
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => {
                    PlayClickSound();
                    _saveManager.DeleteSave(saveInfo.fileName);
                    ShowSaveSlots(); // Refresh the list
                });
            }
        }

        private void TogglePlanetVisualizers()
        {
            _planetVisualizersEnabled = !_planetVisualizersEnabled;
            LogDebug($"Toggling planet visualizers: {_planetVisualizersEnabled}");
            
            if (_planetTagVisualizer != null)
            {
                _planetTagVisualizer.SetActive(_planetVisualizersEnabled);
            }
        }

        private void OnPlanetInteraction(GameObject planet)
        {
            if (planet != null)
            {
                LogDebug($"Planet interaction: {planet.name}");
                // Handle planet selection, show details, etc.
            }
        }

        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[SinglePlayerView] {message}");
            }
        }
    }
}

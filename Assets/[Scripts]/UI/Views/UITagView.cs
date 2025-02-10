using UnityEngine;
using System.Collections.Generic;
using Planetarium.Stats;
using Planetarium.Stats.Debug;
using UnityEngine.SceneManagement;

namespace Planetarium.UI
{
    public class UITagView : UIView
    {
        // PRIVATE MEMBERS
        [Header("Components")]
        [SerializeField]
        private UITagVisualizerInteractions _tagInteractions;

        [Header("Settings")]
        [SerializeField]
        private KeyCode _toggleKey = KeyCode.F4;
        [SerializeField]
        private bool _debugMode = true;

        private List<TaggedComponent> _trackedComponents = new List<TaggedComponent>();
        private bool _visualizersEnabled = true;
        private Camera _mainCamera;
        private bool _initialized = false;
        private float _debugUpdateTimer = 0f;
        private const float DEBUG_LOG_INTERVAL = 1f; // Log debug info every second

        private static UITagView _instance;
        public static UITagView Instance => _instance;

        // UIView INTERFACE
        protected override void OnInitialize()
        {
            base.OnInitialize();
            LogDebug("Initializing UITagView");
            
            if (_instance == null)
            {
                _instance = this;
                LogDebug("Set as singleton instance");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("Multiple UITagView instances found. Only one should exist.");
                Destroy(gameObject);
                return;
            }

            if (!_initialized)
            {
                InitializeView();
            }
        }

        private void InitializeView()
        {
            LogDebug("Initializing view state");
            _initialized = true;
            _mainCamera = Camera.main;
            _tagInteractions.SetActive(true);
            _visualizersEnabled = true;

            // Force this view to stay open
            startOpen = true;
            Open(true);
            LogDebug("View initialization complete");
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            LogDebug("Deinitializing UITagView");

            if (_instance == this)
            {
                _instance = null;
            }

            CleanupVisualizers();
            _initialized = false;
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
            LogDebug("Opening UITagView");
            
            // Ensure we're properly initialized when opened
            if (!_initialized)
            {
                InitializeView();
            }
            
            // Re-create visualizers for all tracked components
            if (_tagInteractions != null)
            {
                LogDebug($"Recreating visualizers for {_trackedComponents.Count} tracked components");
                foreach (var component in _trackedComponents)
                {
                    if (component != null)
                    {
                        _tagInteractions.CreateVisualizerFor(component);
                    }
                }
            }
        }

        public override void Close(bool instant = false)
        {
            LogDebug("Close requested - cleaning up visualizers but staying active");
            CleanupVisualizers();
        }

        private void CleanupVisualizers()
        {
            LogDebug($"Cleaning up visualizers. Current tracked components: {_trackedComponents.Count}");
            if (_tagInteractions != null)
            {
                _tagInteractions.RemoveAllVisualizers();
            }
            _trackedComponents.Clear();
            LogDebug("Visualizer cleanup complete");
        }

        protected override void OnTick()
        {
            base.OnTick();

            // Handle toggle key
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleVisualizers();
            }

            // Always update visualizers if we have interactions
            if (_tagInteractions != null)
            {
                UpdateVisualizers();
            }

            // Debug logging
            if (_debugMode)
            {
                _debugUpdateTimer += Time.deltaTime;
                if (_debugUpdateTimer >= DEBUG_LOG_INTERVAL)
                {
                    LogDebug($"Active tracked components: {_trackedComponents.Count}, Visualizers enabled: {_visualizersEnabled}");
                    _debugUpdateTimer = 0f;
                }
            }
        }

        // PUBLIC MEMBERS
        public bool IsInitialized => _initialized;

        public void AddTrackedComponent(TaggedComponent component)
        {
            if (component == null)
            {
                LogDebug("Attempted to add null component");
                return;
            }

            if (!_initialized)
            {
                LogDebug("Cannot add component - view not initialized");
                return;
            }

            if (_trackedComponents.Contains(component))
            {
                LogDebug($"Component {component.gameObject.name} already tracked");
                return;
            }

            _trackedComponents.Add(component);
            LogDebug($"Added tracked component: {component.gameObject.name}, Total: {_trackedComponents.Count}");
            
            if (_tagInteractions != null && _visualizersEnabled)
            {
                _tagInteractions.CreateVisualizerFor(component);
            }
        }

        public void RemoveTrackedComponent(TaggedComponent component)
        {
            if (component == null)
            {
                LogDebug("Attempted to remove null component");
                return;
            }

            if (!_initialized)
            {
                LogDebug("Cannot remove component - view not initialized");
                return;
            }

            if (_trackedComponents.Remove(component))
            {
                LogDebug($"Removed tracked component: {component.gameObject.name}, Total: {_trackedComponents.Count}");
                
                if (_tagInteractions != null)
                {
                    _tagInteractions.RemoveVisualizerFor(component);
                }
            }
        }

        // PRIVATE MEMBERS
        private void ToggleVisualizers()
        {
            _visualizersEnabled = !_visualizersEnabled;
            LogDebug($"Toggling visualizers: {_visualizersEnabled}");
            
            if (_tagInteractions != null)
            {
                if (_visualizersEnabled)
                {
                    // Recreate all visualizers
                    foreach (var component in _trackedComponents)
                    {
                        if (component != null)
                        {
                            _tagInteractions.CreateVisualizerFor(component);
                        }
                    }
                }
                else
                {
                    // Remove all visualizers
                    _tagInteractions.RemoveAllVisualizers();
                }
            }
        }

        private void UpdateVisualizers()
        {
            if (!_initialized || _mainCamera == null) return;

            _tagInteractions.UpdateVisualizerPositions(_mainCamera);
           
        }

        private void OnSceneLoaded()
        {
            // Reset camera reference since it might have changed
            _mainCamera = Camera.main;
            
            // Clean up old visualizers
            CleanupVisualizers();
            
            // Make sure interactions are in correct state and enabled
            if (_tagInteractions != null)
            {
                _tagInteractions.SetActive(true);
                _visualizersEnabled = true;
            }
        }

        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[UITagView] {message}");
            }
        }
    }
}

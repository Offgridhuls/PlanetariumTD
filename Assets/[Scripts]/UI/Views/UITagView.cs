using UnityEngine;
using System.Collections.Generic;
using Planetarium.Stats;
using Planetarium.Stats.Debug;

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

        private List<TaggedComponent> _trackedComponents = new List<TaggedComponent>();
        private bool _visualizersEnabled = true;
        private Camera _mainCamera;

        private static UITagView _instance;
        public static UITagView Instance => _instance;

        // UIView INTERFACE
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("Multiple UITagView instances found. Only one should exist.");
                Destroy(gameObject);
                return;
            }

            _mainCamera = Camera.main;
            _tagInteractions.SetActive(_visualizersEnabled);
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();

            foreach (var component in _trackedComponents.ToArray())
            {
                if (component != null)
                {
                    RemoveTrackedComponent(component);
                }
            }
            _trackedComponents.Clear();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            // Handle toggle key
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleVisualizers();
            }

            if (_visualizersEnabled && _tagInteractions != null)
            {
                UpdateVisualizers();
            }
        }

        // PUBLIC MEMBERS
        public void AddTrackedComponent(TaggedComponent component)
        {
            if (component == null || _trackedComponents.Contains(component))
                return;

            _trackedComponents.Add(component);
            
            if (_visualizersEnabled && _tagInteractions != null)
            {
                _tagInteractions.CreateVisualizerFor(component);
            }
        }

        public void RemoveTrackedComponent(TaggedComponent component)
        {
            if (component == null)
                return;

            _trackedComponents.Remove(component);
            
            if (_tagInteractions != null)
            {
                _tagInteractions.RemoveVisualizerFor(component);
            }
        }

        // PRIVATE MEMBERS
        private void ToggleVisualizers()
        {
            _visualizersEnabled = !_visualizersEnabled;
            _tagInteractions.SetActive(_visualizersEnabled);
            
            if (_visualizersEnabled)
            {
                // Recreate visualizers for all tracked components
                foreach (var component in _trackedComponents)
                {
                    _tagInteractions.CreateVisualizerFor(component);
                }
            }
            else
            {
                _tagInteractions.RemoveAllVisualizers();
            }
        }

        private void UpdateVisualizers()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            _tagInteractions.UpdateVisualizerPositions(_mainCamera);
        }
    }
}

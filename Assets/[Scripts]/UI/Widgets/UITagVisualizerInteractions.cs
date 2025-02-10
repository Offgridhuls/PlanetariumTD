using UnityEngine;
using System.Collections.Generic;
using Planetarium.Stats;

namespace Planetarium.UI
{
    public class UITagVisualizerInteractions : MonoBehaviour
    {
        // PRIVATE MEMBERS
        [Header("Settings")]
        [SerializeField]
        private UITagGroup _tagGroupPrefab;

        [SerializeField]
        private float _maxDistance = 100f;
        
        [SerializeField]
        private float _minDistance = 10f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = true;

        private Dictionary<TaggedComponent, UITagGroup> _activeVisualizers = new Dictionary<TaggedComponent, UITagGroup>();
        private bool _isActive = true;

        // PUBLIC MEMBERS
        public void SetActive(bool active)
        {
            _isActive = active;
            gameObject.SetActive(active);
            LogDebug($"Set visualizer interactions active: {active}");

            if (!active)
            {
                RemoveAllVisualizers();
            }
        }

        public void CreateVisualizerFor(TaggedComponent target)
        {
            if (!_isActive || target == null || _activeVisualizers.ContainsKey(target) || _tagGroupPrefab == null)
            {
                if (target == null)
                {
                    LogDebug($"Attempted to create visualizer for null target");
                }
                else if (_activeVisualizers.ContainsKey(target))
                {
                    LogDebug($"Visualizer already exists for {target.gameObject.name}");
                }
                return;
            }

            var visualizer = Instantiate(_tagGroupPrefab, transform);
            _activeVisualizers[target] = visualizer;
            
            visualizer.Initialize(target);
            UpdateVisualizerPosition(target, visualizer, Camera.main);
            LogDebug($"Created visualizer for {target.gameObject.name}, Total active: {_activeVisualizers.Count}");
        }

        public void RemoveVisualizerFor(TaggedComponent target)
        {
            if (target == null)
            {
                LogDebug("Attempted to remove visualizer for null target");
                return;
            }

            if (_activeVisualizers.TryGetValue(target, out var visualizer))
            {
                if (visualizer != null)
                {
                    Destroy(visualizer.gameObject);
                }
                _activeVisualizers.Remove(target);
                LogDebug($"Removed visualizer for {target.gameObject.name}, Total active: {_activeVisualizers.Count}");
            }
        }

        public void RemoveAllVisualizers()
        {
            LogDebug($"Removing all visualizers. Count before removal: {_activeVisualizers.Count}");
            foreach (var visualizer in _activeVisualizers.Values)
            {
                if (visualizer != null)
                {
                    Destroy(visualizer.gameObject);
                }
            }
            _activeVisualizers.Clear();
            LogDebug("All visualizers removed");
        }

        public void UpdateVisualizerPositions(Camera camera)
        {
            if (!_isActive || camera == null)
                return;

            int updatedCount = 0;
            List<TaggedComponent> toRemove = new List<TaggedComponent>();

            foreach (var kvp in _activeVisualizers)
            {
                var target = kvp.Key;
                var visualizer = kvp.Value;

                if (target == null || target.gameObject == null || visualizer == null)
                {
                    toRemove.Add(target);
                    continue;
                }

                UpdateVisualizerPosition(target, visualizer, camera);
                updatedCount++;
            }

            // Clean up destroyed targets
            foreach (var target in toRemove)
            {
                RemoveVisualizerFor(target);
            }

            if (_debugMode && Time.frameCount % 60 == 0) // Log every 60 frames
            {
                LogDebug($"Updated {updatedCount}/{_activeVisualizers.Count} visualizer positions");
            }
        }

        private void UpdateVisualizerPosition(TaggedComponent target, UITagGroup visualizer, Camera camera)
        {
            visualizer.UpdatePosition(camera);

            // Update appearance based on distance
            float distance = Vector3.Distance(camera.transform.position, target.transform.position);
            visualizer.UpdateAppearance(distance, _minDistance, _maxDistance);
        }

        private void OnDestroy()
        {
            RemoveAllVisualizers();
        }

        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[UITagVisualizer] {message}");
            }
        }
    }
}

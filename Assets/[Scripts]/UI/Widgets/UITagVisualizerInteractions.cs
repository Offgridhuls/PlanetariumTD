using UnityEngine;
using System.Collections.Generic;
using Planetarium.Stats;

namespace Planetarium.UI
{
    [System.Serializable]
    public class TagPrefabMapping
    {
        [SerializeField] public GameplayTag tag;
        [SerializeField] public UITagGroup prefab;
    }

    public class UITagVisualizerInteractions : MonoBehaviour
    {
        // PRIVATE MEMBERS
        [Header("Settings")]
        [SerializeField]
        protected UITagGroup _defaultTagGroupPrefab;

        [SerializeField]
        protected List<TagPrefabMapping> _tagSpecificPrefabs = new List<TagPrefabMapping>();

        [SerializeField]
        protected float _maxDistance = 100f;
        
        [SerializeField]
        protected float _minDistance = 10f;

        [Header("Debug")]
        [SerializeField] protected bool _debugMode = true;

        protected Dictionary<TaggedComponent, UITagGroup> _activeVisualizers = new Dictionary<TaggedComponent, UITagGroup>();
        protected bool _isActive = true;
        protected Dictionary<GameplayTag, UITagGroup> _prefabByTag = new Dictionary<GameplayTag, UITagGroup>();

        private void Awake()
        {
            // Initialize the tag-to-prefab mapping
            foreach (var mapping in _tagSpecificPrefabs)
            {
                if (mapping.tag != null && mapping.prefab != null)
                {
                    _prefabByTag[mapping.tag] = mapping.prefab;
                }
            }
        }

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
            if (!_isActive || target == null || _activeVisualizers.ContainsKey(target))
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

            // Select the appropriate prefab based on tags
            UITagGroup prefabToUse = _defaultTagGroupPrefab;
            
            // Check if the tagged component has any tags that map to specific prefabs
            foreach (var tag in target.Tags)
            {
                if (_prefabByTag.TryGetValue(tag, out var tagSpecificPrefab))
                {
                    prefabToUse = tagSpecificPrefab;
                    LogDebug($"Using tag-specific prefab for {tag.TagName} on {target.gameObject.name}");
                    break; // Use the first matching tag's prefab
                }
            }

            if (prefabToUse == null)
            {
                LogDebug($"No valid prefab found for {target.gameObject.name}");
                return;
            }

            var visualizer = Instantiate(prefabToUse, transform);
            _activeVisualizers[target] = visualizer;
            
            visualizer.Initialize(target);
            UpdateVisualizerPosition(target, visualizer, UnityEngine.Camera.main);
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

        public void UpdateVisualizerPositions(UnityEngine.Camera camera)
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

        public void UpdateVisualizerPosition(TaggedComponent target, UITagGroup visualizer, UnityEngine.Camera camera)
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

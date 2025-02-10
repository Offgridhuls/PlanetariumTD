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

        private Dictionary<TaggedComponent, UITagGroup> _activeVisualizers = new Dictionary<TaggedComponent, UITagGroup>();
        private bool _isActive = true;

        // PUBLIC MEMBERS
        public void SetActive(bool active)
        {
            _isActive = active;
            gameObject.SetActive(active);

            if (!active)
            {
                RemoveAllVisualizers();
            }
        }

        public void CreateVisualizerFor(TaggedComponent target)
        {
            if (!_isActive || target == null || _activeVisualizers.ContainsKey(target) || _tagGroupPrefab == null)
                return;

            var visualizer = Instantiate(_tagGroupPrefab, transform);
            _activeVisualizers[target] = visualizer;
            
            visualizer.Initialize(target);
            UpdateVisualizerPosition(target, visualizer, Camera.main);
        }

        public void RemoveVisualizerFor(TaggedComponent target)
        {
            if (target == null || !_activeVisualizers.TryGetValue(target, out var visualizer))
                return;

            if (visualizer != null)
            {
                Destroy(visualizer.gameObject);
            }
            _activeVisualizers.Remove(target);
        }

        public void RemoveAllVisualizers()
        {
            foreach (var visualizer in _activeVisualizers.Values)
            {
                if (visualizer != null)
                {
                    Destroy(visualizer.gameObject);
                }
            }
            _activeVisualizers.Clear();
        }

        public void UpdateVisualizerPositions(Camera camera)
        {
            if (!_isActive || camera == null)
                return;

            var toRemove = new List<TaggedComponent>();

            foreach (var kvp in _activeVisualizers)
            {
                var target = kvp.Key;
                var visualizer = kvp.Value;

                if (target == null || !target.gameObject.activeInHierarchy || visualizer == null)
                {
                    toRemove.Add(target);
                    continue;
                }

                UpdateVisualizerPosition(target, visualizer, camera);
            }

            foreach (var target in toRemove)
            {
                RemoveVisualizerFor(target);
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
    }
}

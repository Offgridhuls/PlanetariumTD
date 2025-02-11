using UnityEngine;
using System.Collections.Generic;
using Planetarium.Stats;
using Planetarium.Stats.Debug;

namespace Planetarium.UI
{
    public class PlanetTagVisualizerInteractions : UITagVisualizerInteractions
    {
        [SerializeField] private PlanetTagGroup _planetTagGroupPrefab;

        [Header("Planet Tag Settings")] 
        [SerializeField] private bool _scaleWithDistance = true;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 2f;
        [SerializeField] private bool _fadeWithDistance = true;
        [SerializeField] private float _fadeStartDistance = 50f;
        [SerializeField] private float _fadeEndDistance = 100f;

        private Dictionary<TaggedComponent, PlanetTagGroup> _planetVisualizers;
        private bool _visualizersEnabled;
        private UnityEngine.Camera _camera;

        private void Awake()
        {
            _planetVisualizers = new Dictionary<TaggedComponent, PlanetTagGroup>();
            _visualizersEnabled = true;
        }

        private void OnEnable()
        {
            _visualizersEnabled = true;
            if (_camera != null)
            {
                UpdateVisualizerPositions(_camera);
            }
        }

        public void SetCamera(UnityEngine.Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError("[PlanetTagVisualizer] Attempted to set null camera!");
                return;
            }

            _camera = camera;
            if (_visualizersEnabled)
            {
                UpdateVisualizerPositions(_camera);
            }
        }

        private void Update()
        {
            if (!_visualizersEnabled || _camera == null) return;
            UpdateVisualizerPositions(_camera);
        }

        public new void SetActive(bool active)
        {
            _visualizersEnabled = active;
            gameObject.SetActive(active);

            // Update positions immediately when activated
            if (active && _camera != null)
            {
                UpdateVisualizerPositions(_camera);
            }
        }

        public new void CreateVisualizerFor(TaggedComponent target)
        {
            if (!_visualizersEnabled || target == null || _planetVisualizers.ContainsKey(target) || _planetTagGroupPrefab == null)
            {
                Debug.LogWarning($"[PlanetTagVisualizer] Cannot create visualizer: Active={_visualizersEnabled}, Target={target}, HasVisualizer={_planetVisualizers.ContainsKey(target)}, Prefab={_planetTagGroupPrefab != null}");
                return;
            }

            // Only create visualizers for objects tagged as planets
            if (!target.CompareTag("Planet"))
            {
                Debug.Log($"[PlanetTagVisualizer] Object {target.gameObject.name} is not tagged as Planet");
                return;
            }

            var visualizer = Instantiate(_planetTagGroupPrefab, transform);
            _planetVisualizers[target] = visualizer;
            visualizer.Initialize(target);

            // Update position immediately if we have a camera
            if (_camera != null)
            {
                UpdateVisualizerPosition(target, visualizer, _camera);
            }

            Debug.Log($"[PlanetTagVisualizer] Created visualizer for {target.gameObject.name}");
        }

        public new void RemoveVisualizerFor(TaggedComponent target)
        {
            if (target == null || !_planetVisualizers.ContainsKey(target)) return;

            var visualizer = _planetVisualizers[target];
            if (visualizer != null)
            {
                Destroy(visualizer.gameObject);
            }

            _planetVisualizers.Remove(target);
        }

        public new void RemoveAllVisualizers()
        {
            foreach (var visualizer in _planetVisualizers.Values)
            {
                if (visualizer != null)
                {
                    Destroy(visualizer.gameObject);
                }
            }

            _planetVisualizers.Clear();
        }

        private void UpdateVisualizerPosition(TaggedComponent target, PlanetTagGroup visualizer, UnityEngine.Camera camera)
        {
            if (target == null || target.gameObject == null || visualizer == null || camera == null)
                return;

            // Update base position
            Vector3 screenPos = camera.WorldToScreenPoint(target.transform.position);
            
            // Check if the planet is behind the camera
            if (screenPos.z < 0)
            {
                visualizer.gameObject.SetActive(false);
                return;
            }

            // Convert to screen space
            screenPos.z = 0;
            visualizer.transform.position = screenPos;

            // Get distance to camera
            float distanceToCamera = Vector3.Distance(camera.transform.position, target.transform.position);

            // Scale based on distance
            if (_scaleWithDistance)
            {
                float scaleMultiplier = Mathf.Clamp(1f / (distanceToCamera * 0.1f), _minScale, _maxScale);
                visualizer.transform.localScale = Vector3.one * scaleMultiplier;
            }

            // Update visibility
            visualizer.gameObject.SetActive(true);
        }

        public new void UpdateVisualizerPositions(UnityEngine.Camera camera)
        {
            if (!_visualizersEnabled || camera == null)
            {
                return;
            }

            foreach (var kvp in _planetVisualizers)
            {
                UpdateVisualizerPosition(kvp.Key, kvp.Value, camera);
            }
        }

        private void OnDestroy()
        {
            RemoveAllVisualizers();
        }
    }
}

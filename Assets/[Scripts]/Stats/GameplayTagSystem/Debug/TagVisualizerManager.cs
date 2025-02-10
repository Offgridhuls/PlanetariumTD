using UnityEngine;
using System.Collections;

namespace Planetarium.Stats.Debug
{
    /// <summary>
    /// Manager component that handles creation and removal of tag visualizers.
    /// Add this to a GameObject in your scene to manage tag visualization.
    /// </summary>
    public class TagVisualizerManager : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [Tooltip("Automatically show visualizers for all tagged objects")]
        [SerializeField] private bool showAllVisualizers = true;

        [Tooltip("Key to toggle all visualizers")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        private static TagVisualizerManager instance;
        private bool isDestroying = false;

        public static TagVisualizerManager Instance
        {
            get
            {
                if (instance == null && !isQuitting)
                {
                    instance = FindObjectOfType<TagVisualizerManager>();
                }
                return instance;
            }
        }

        private static bool isQuitting = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (showAllVisualizers)
            {
                ShowAll();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleAll();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isDestroying = true;
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Shows visualizers for all tagged objects in the scene.
        /// </summary>
        public void ShowAll()
        {
            if (isDestroying || isQuitting) return;
            StartCoroutine(ShowAllCoroutine());
        }

        private IEnumerator ShowAllCoroutine()
        {
            yield return null; // Wait one frame to avoid physics/animation conflicts
            if (!isDestroying && !isQuitting)
            {
                TagVisualizerPrefab.CreateForAllTaggedComponents();
            }
        }

        /// <summary>
        /// Hides all visualizers in the scene.
        /// </summary>
        public void HideAll()
        {
            if (isDestroying || isQuitting) return;
            StartCoroutine(HideAllCoroutine());
        }

        private IEnumerator HideAllCoroutine()
        {
            yield return null;
            if (!isDestroying && !isQuitting)
            {
                TagVisualizerPrefab.RemoveAll();
            }
        }

        /// <summary>
        /// Toggles visibility of all visualizers.
        /// </summary>
        public void ToggleAll()
        {
            if (isDestroying || isQuitting) return;
            
            var existing = FindObjectOfType<TagVisualizer>();
            if (existing != null)
            {
                HideAll();
            }
            else
            {
                ShowAll();
            }
        }

        /// <summary>
        /// Shows visualizer for a specific tagged object.
        /// </summary>
        public void ShowFor(GameObject target)
        {
            if (isDestroying || isQuitting || target == null) return;
            StartCoroutine(ShowForCoroutine(target));
        }

        private IEnumerator ShowForCoroutine(GameObject target)
        {
            yield return null; // Wait one frame to avoid physics/animation conflicts
            if (!isDestroying && !isQuitting)
            {
                TagVisualizerPrefab.Create(target);
            }
        }

        /// <summary>
        /// Hides visualizer for a specific tagged object.
        /// </summary>
        public void HideFor(GameObject target)
        {
            if (isDestroying || isQuitting || target == null) return;
            StartCoroutine(HideForCoroutine(target));
        }

        private IEnumerator HideForCoroutine(GameObject target)
        {
            yield return null; // Wait one frame to avoid physics/animation conflicts
            if (!isDestroying && !isQuitting)
            {
                TagVisualizerPrefab.Remove(target);
            }
        }
    }
}

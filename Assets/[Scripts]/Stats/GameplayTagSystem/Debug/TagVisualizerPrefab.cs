using UnityEngine;

namespace Planetarium.Stats.Debug
{
    /// <summary>
    /// Helper component to create TagVisualizer instances from a prefab.
    /// </summary>
    public static class TagVisualizerPrefab
    {
        private static GameObject prefabInstance;

        /// <summary>
        /// Creates a TagVisualizer for the specified GameObject.
        /// </summary>
        public static TagVisualizer Create(GameObject target)
        {
            if (target == null) return null;

            // Ensure target has TaggedComponent
            var taggedComponent = target.GetComponent<TaggedComponent>();
            if (taggedComponent == null)
            {
                UnityEngine.Debug.LogError($"Cannot create TagVisualizer for {target.name}: Missing TaggedComponent!");
                return null;
            }

            // Try to find existing visualizer
            var existing = target.GetComponent<TagVisualizer>();
            if (existing != null) return existing;

            // Add visualizer directly to the target
            var visualizer = target.AddComponent<TagVisualizer>();
            
            // Copy settings from prefab if available
            if (prefabInstance == null)
            {
                prefabInstance = UnityEngine.Resources.Load<GameObject>("TagVisualizerPrefab");
            }

            if (prefabInstance != null)
            {
                var prefabVisualizer = prefabInstance.GetComponent<TagVisualizer>();
                if (prefabVisualizer != null)
                {
                    // Copy serialized fields from prefab
                    var fields = typeof(TagVisualizer).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0)
                        {
                            field.SetValue(visualizer, field.GetValue(prefabVisualizer));
                        }
                    }
                }
            }

            return visualizer;
        }

        /// <summary>
        /// Creates TagVisualizers for all TaggedComponents in the scene.
        /// </summary>
        public static void CreateForAllTaggedComponents()
        {
            var taggedComponents = UnityEngine.Object.FindObjectsOfType<TaggedComponent>();
            foreach (var tagged in taggedComponents)
            {
                Create(tagged.gameObject);
            }
        }

        /// <summary>
        /// Removes all TagVisualizers in the scene.
        /// </summary>
        public static void RemoveAll()
        {
            var visualizers = UnityEngine.Object.FindObjectsOfType<TagVisualizer>();
            foreach (var visualizer in visualizers)
            {
                UnityEngine.Object.Destroy(visualizer);
            }
        }

        /// <summary>
        /// Removes the TagVisualizer from a specific GameObject.
        /// </summary>
        public static void Remove(GameObject target)
        {
            if (target == null) return;
            
            var visualizer = target.GetComponent<TagVisualizer>();
            if (visualizer != null)
            {
                UnityEngine.Object.Destroy(visualizer);
            }
        }
    }
}

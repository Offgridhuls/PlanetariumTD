using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium.Stats
{
    public class TaggedObjectFilter : SceneService
    {
        private Dictionary<GameplayTag, HashSet<TaggedComponent>> taggedObjects = new Dictionary<GameplayTag, HashSet<TaggedComponent>>();
        private Dictionary<TaggedComponent, HashSet<GameplayTag>> objectTags = new Dictionary<TaggedComponent, HashSet<GameplayTag>>();
        private Dictionary<TaggedComponent, (System.Action<GameplayTag>, System.Action<GameplayTag>)> eventHandlers = 
            new Dictionary<TaggedComponent, (System.Action<GameplayTag>, System.Action<GameplayTag>)>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            UnityEngine.Debug.Log($"[TaggedObjectFilter] Initialized");
        }

        protected override void OnDeinitialize()
        {
            // Clean up all registrations
            var components = objectTags.Keys.ToList();
            foreach (var component in components)
            {
                if (component != null)
                {
                    UnregisterTaggedObject(component);
                }
            }
            
            taggedObjects.Clear();
            objectTags.Clear();
            eventHandlers.Clear();
            
            base.OnDeinitialize();
            UnityEngine.Debug.Log($"[TaggedObjectFilter] Deinitialized");
        }

        public void RegisterTaggedObject(TaggedComponent component)
        {
            if (component == null) return;
            
            // Unregister first if already registered to prevent duplicates
            if (eventHandlers.ContainsKey(component))
            {
                UnregisterTaggedObject(component);
            }
            
            // Initialize sets if needed
            if (!objectTags.ContainsKey(component))
            {
                objectTags[component] = new HashSet<GameplayTag>();
            }

            // Register initial tags
            foreach (var tag in component.Tags)
            {
                AddTagToIndex(tag, component);
            }

            // Create and store event handlers
            System.Action<GameplayTag> addHandler = (tag) => AddTagToIndex(tag, component);
            System.Action<GameplayTag> removeHandler = (tag) => RemoveTagFromIndex(tag, component);
            
            // Store handlers for cleanup
            eventHandlers[component] = (addHandler, removeHandler);
            
            // Subscribe to tag events
            component.OnTagAdded += addHandler;
            component.OnTagRemoved += removeHandler;
            
           // UnityEngine.Debug.Log($"[TaggedObjectFilter] Registered {component.gameObject.name} with {component.Tags.Count} tags");
        }

        public void UnregisterTaggedObject(TaggedComponent component)
        {
            if (component == null) return;
            
            // Unsubscribe from events
            if (eventHandlers.TryGetValue(component, out var handlers))
            {
                component.OnTagAdded -= handlers.Item1;
                component.OnTagRemoved -= handlers.Item2;
                eventHandlers.Remove(component);
            }
            
            // Remove from tag indices
            if (objectTags.TryGetValue(component, out var tags))
            {
                foreach (var tag in tags.ToList()) // Create a copy to avoid modification during enumeration
                {
                    if (taggedObjects.TryGetValue(tag, out var components))
                    {
                        components.Remove(component);
                        if (components.Count == 0)
                        {
                            taggedObjects.Remove(tag);
                        }
                    }
                }
                objectTags.Remove(component);
            }
            
            // UnityEngine.Debug.Log($"[TaggedObjectFilter] Unregistered {component?.gameObject.name ?? "null"}");
        }

        private void AddTagToIndex(GameplayTag tag, TaggedComponent component)
        {
            // Add to tag index
            if (!taggedObjects.ContainsKey(tag))
            {
                taggedObjects[tag] = new HashSet<TaggedComponent>();
            }
            taggedObjects[tag].Add(component);

            // Add to object index
            if (objectTags.ContainsKey(component))
            {
                objectTags[component].Add(tag);
            }
        }

        private void RemoveTagFromIndex(GameplayTag tag, TaggedComponent component)
        {
            if (taggedObjects.TryGetValue(tag, out var components))
            {
                components.Remove(component);
                if (components.Count == 0)
                {
                    taggedObjects.Remove(tag);
                }
            }

            if (objectTags.ContainsKey(component))
            {
                objectTags[component].Remove(tag);
            }
        }

        public IEnumerable<TaggedComponent> GetObjectsWithTag(GameplayTag tag)
        {
            return taggedObjects.TryGetValue(tag, out var components) ? components : Enumerable.Empty<TaggedComponent>();
        }

        public IEnumerable<TaggedComponent> GetObjectsWithAnyTag(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return Enumerable.Empty<TaggedComponent>();

            return tags.SelectMany(tag => GetObjectsWithTag(tag)).Distinct();
        }

        public IEnumerable<TaggedComponent> GetObjectsWithAllTags(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return Enumerable.Empty<TaggedComponent>();

            var result = GetObjectsWithTag(tags[0]);
            for (int i = 1; i < tags.Length; i++)
            {
                result = result.Intersect(GetObjectsWithTag(tags[i]));
            }
            return result;
        }

        public IEnumerable<T> GetComponentsWithTag<T>(GameplayTag tag) where T : Component
        {
            return GetObjectsWithTag(tag)
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null);
        }

        public IEnumerable<T> GetComponentsWithAnyTag<T>(params GameplayTag[] tags) where T : Component
        {
            return GetObjectsWithAnyTag(tags)
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null);
        }

        public IEnumerable<T> GetComponentsWithAllTags<T>(params GameplayTag[] tags) where T : Component
        {
            return GetObjectsWithAllTags(tags)
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null);
        }
    }
}

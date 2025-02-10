using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium.Stats
{
    public class TaggedObjectFilter : MonoBehaviour
    {
        private static TaggedObjectFilter instance;
        public static TaggedObjectFilter Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("TaggedObjectFilter");
                    instance = go.AddComponent<TaggedObjectFilter>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<GameplayTag, HashSet<TaggedComponent>> taggedObjects = new Dictionary<GameplayTag, HashSet<TaggedComponent>>();
        private Dictionary<int, HashSet<GameplayTag>> objectTags = new Dictionary<int, HashSet<GameplayTag>>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterTaggedObject(TaggedComponent component)
        {
            int id = component.GetInstanceID();
            
            // Initialize sets if needed
            if (!objectTags.ContainsKey(id))
            {
                objectTags[id] = new HashSet<GameplayTag>();
            }

            // Register initial tags
            foreach (var tag in component.Tags)
            {
                AddTagToIndex(tag, component);
            }

            // Subscribe to tag events
            component.OnTagAdded += (tag) => AddTagToIndex(tag, component);
            component.OnTagRemoved += (tag) => RemoveTagFromIndex(tag, component);
        }

        public void UnregisterTaggedObject(TaggedComponent component)
        {
            int id = component.GetInstanceID();
            
            if (objectTags.TryGetValue(id, out var tags))
            {
                foreach (var tag in tags)
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
                objectTags.Remove(id);
            }
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
            int id = component.GetInstanceID();
            objectTags[id].Add(tag);
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

            int id = component.GetInstanceID();
            if (objectTags.ContainsKey(id))
            {
                objectTags[id].Remove(tag);
            }
        }

        public IEnumerable<TaggedComponent> GetObjectsWithTag(GameplayTag tag)
        {
            if (taggedObjects.TryGetValue(tag, out var components))
            {
                return components;
            }
            return Enumerable.Empty<TaggedComponent>();
        }

        public IEnumerable<TaggedComponent> GetObjectsWithAnyTag(params GameplayTag[] tags)
        {
            var result = new HashSet<TaggedComponent>();
            foreach (var tag in tags)
            {
                if (taggedObjects.TryGetValue(tag, out var components))
                {
                    result.UnionWith(components);
                }
            }
            return result;
        }

        public IEnumerable<TaggedComponent> GetObjectsWithAllTags(params GameplayTag[] tags)
        {
            if (tags.Length == 0) return Enumerable.Empty<TaggedComponent>();

            // Start with objects that have the first tag
            if (!taggedObjects.TryGetValue(tags[0], out var result))
            {
                return Enumerable.Empty<TaggedComponent>();
            }

            var filteredResult = new HashSet<TaggedComponent>(result);

            // Filter by each additional tag
            for (int i = 1; i < tags.Length; i++)
            {
                if (!taggedObjects.TryGetValue(tags[i], out var components))
                {
                    return Enumerable.Empty<TaggedComponent>();
                }

                filteredResult.IntersectWith(components);
                if (filteredResult.Count == 0)
                {
                    break;
                }
            }

            return filteredResult;
        }

        public IEnumerable<TaggedComponent> GetObjectsMatchingQuery(GameplayTagQuery query)
        {
            return FindObjectsOfType<TaggedComponent>().Where(c => c.MatchesQuery(query));
        }

        public IEnumerable<T> GetComponentsWithTag<T>(GameplayTag tag) where T : Component
        {
            return GetObjectsWithTag(tag)
                .Select(c => c.GetComponent<T>())
                .Where(c => c != null);
        }

        public IEnumerable<T> GetComponentsWithAnyTag<T>(params GameplayTag[] tags) where T : Component
        {
            return GetObjectsWithAnyTag(tags)
                .Select(c => c.GetComponent<T>())
                .Where(c => c != null);
        }

        public IEnumerable<T> GetComponentsWithAllTags<T>(params GameplayTag[] tags) where T : Component
        {
            return GetObjectsWithAllTags(tags)
                .Select(c => c.GetComponent<T>())
                .Where(c => c != null);
        }
    }
}

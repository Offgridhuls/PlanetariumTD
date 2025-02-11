using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    /// <summary>
    /// MonoBehaviour component that manages gameplay tags for a GameObject.
    /// Provides an interface for adding, removing, and querying tags, and raises events when tags change.
    /// </summary>
    public class TaggedComponent : MonoBehaviour
    {
        /// <summary>
        /// Event raised when a tag is added to this component
        /// </summary>
        public event Action<GameplayTag> OnTagAdded;

        /// <summary>
        /// Event raised when a tag is removed from this component
        /// </summary>
        public event Action<GameplayTag> OnTagRemoved;

        /// <summary>
        /// The container that stores and manages the actual tags
        /// </summary>
        [SerializeField] private GameplayTagContainer tags = new GameplayTagContainer();

        /// <summary>
        /// The parent object that this component is attached to
        /// </summary>
        public ITaggable Parent { get; private set; }

        /// <summary>
        /// All tags currently applied to this component
        /// </summary>
        public IReadOnlyList<GameplayTag> Tags => tags.Tags;

        private TaggedObjectFilter tagFilter;

        #region Unity Lifecycle
        private void Awake()
        {
            Parent = GetComponent<ITaggable>();
            tagFilter = FindFirstObjectByType<TaggedObjectFilter>();
        }

        private void OnEnable()
        {
            if (tagFilter != null)
            {
                tagFilter.RegisterTaggedObject(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[TaggedComponent] No TaggedObjectFilter found for {gameObject.name}");
            }
        }

        private void OnDisable()
        {
            if (tagFilter != null)
            {
                tagFilter.UnregisterTaggedObject(this);
            }
        }
        #endregion

        #region Tag Management
        /// <summary>
        /// Adds a tag to this component
        /// </summary>
        /// <param name="tag">The tag to add</param>
        public void AddTag(GameplayTag tag)
        {
            if (tags.AddTag(tag))
            {
                OnTagAdded?.Invoke(tag);
                Parent?.OnTagAdded(tag);
            }
        }

        /// <summary>
        /// Removes a tag from this component
        /// </summary>
        /// <param name="tag">The tag to remove</param>
        public void RemoveTag(GameplayTag tag)
        {
            if (tags.RemoveTag(tag))
            {
                OnTagRemoved?.Invoke(tag);
                Parent?.OnTagRemoved(tag);
            }
        }

        /// <summary>
        /// Checks if this component has a specific tag
        /// </summary>
        /// <param name="tag">The tag to check for</param>
        /// <returns>True if the component has the tag</returns>
        public bool HasTag(GameplayTag tag)
        {
            return tags.HasExactTag(tag);
        }

        /// <summary>
        /// Checks if this component has any of the specified tags
        /// </summary>
        /// <param name="searchTags">The tags to check for</param>
        /// <returns>True if the component has any of the tags</returns>
        public bool HasAnyTag(params GameplayTag[] searchTags)
        {
            foreach (var tag in searchTags)
            {
                if (HasTag(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if this component has all of the specified tags
        /// </summary>
        /// <param name="searchTags">The tags to check for</param>
        /// <returns>True if the component has all the tags</returns>
        public bool HasAllTags(params GameplayTag[] searchTags)
        {
            foreach (var tag in searchTags)
            {
                if (!HasTag(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if this component matches a specific tag query
        /// </summary>
        /// <param name="query">The query to match against</param>
        /// <returns>True if the component matches the query</returns>
        public bool MatchesQuery(GameplayTagQuery query)
        {
            return tags.MatchesQuery(query);
        }

        /// <summary>
        /// Gets all tags from this component and appends them to a container
        /// </summary>
        /// <param name="container">The container to append tags to</param>
        public void GetTags(GameplayTagContainer container)
        {
            container.AppendTags(tags);
        }
        #endregion
    }
}

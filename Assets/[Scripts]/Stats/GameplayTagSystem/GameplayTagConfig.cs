using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Planetarium.Stats
{
    /// <summary>
    /// ScriptableObject that manages the configuration and validation of gameplay tags.
    /// This is the central repository for all valid tags in the game and their relationships.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTagConfig", menuName = "PlanetariumTD/Gameplay Tag Config")]
    public class GameplayTagConfig : ScriptableObject
    {
        #region Configuration
        /// <summary>
        /// Whether to import tags from the config file
        /// </summary>
        public bool ImportTagsFromConfig = true;

        /// <summary>
        /// Whether to warn about invalid tags
        /// </summary>
        public bool WarnOnInvalidTags = true;

        /// <summary>
        /// Whether to use fast replication for tags
        /// </summary>
        public bool FastReplication = true;

        /// <summary>
        /// Characters that are not allowed in tag names
        /// </summary>
        public string InvalidTagCharacters = "\"',";
        #endregion

        #region Singleton
        private static GameplayTagConfig instance;

        /// <summary>
        /// Singleton instance of the config, loaded from Resources folder
        /// </summary>
        public static GameplayTagConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Resources.Load<GameplayTagConfig>("GameplayTagConfig");
                    if (instance == null)
                    {
                        UnityEngine.Debug.LogError("GameplayTagConfig asset not found in Resources folder!");
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Tag Definitions
        /// <summary>
        /// List of all valid gameplay tags in the system
        /// </summary>
        [SerializeField] private List<GameplayTagDefinition> gameplayTagList = new List<GameplayTagDefinition>();
        public IReadOnlyList<GameplayTagDefinition> GameplayTagList => gameplayTagList;

        /// <summary>
        /// List of tag redirects, where a tag can be automatically converted to another tag
        /// </summary>
        [SerializeField] private List<GameplayTagRedirect> gameplayTagRedirects = new List<GameplayTagRedirect>();
        public IReadOnlyList<GameplayTagRedirect> GameplayTagRedirects => gameplayTagRedirects;
        #endregion

        /// <summary>
        /// Adds a new tag to the config if it doesn't already exist
        /// </summary>
        /// <param name="tag">The tag to add</param>
        /// <param name="comment">Optional comment for the tag</param>
        public void AddTag(string tag, string comment = "")
        {
            if (string.IsNullOrEmpty(tag)) return;

            // Normalize tag name
            tag = tag.Trim().ToLower();

            // Check for invalid characters
            if (tag.IndexOfAny(InvalidTagCharacters.ToCharArray()) != -1)
            {
                if (WarnOnInvalidTags)
                {
                    UnityEngine.Debug.Log($"Tag '{tag}' contains invalid characters: {InvalidTagCharacters}");
                }
                return;
            }

            // Check if tag already exists
            if (gameplayTagList.Exists(t => t.Tag == tag))
            {
                UnityEngine.Debug.LogWarning($"Tag '{tag}' already exists in the config");
                return;
            }

            // Add parent tags if they don't exist
            var parts = tag.Split('.');
            var currentPath = "";
            for (int i = 0; i < parts.Length - 1; i++)
            {
                currentPath = i == 0 ? parts[i] : currentPath + "." + parts[i];
                if (!gameplayTagList.Exists(t => t.Tag == currentPath))
                {
                    gameplayTagList.Add(new GameplayTagDefinition { Tag = currentPath, DevComment = $"Parent tag for {tag}" });
                }
            }

            gameplayTagList.Add(new GameplayTagDefinition { Tag = tag, DevComment = comment });
            gameplayTagList = gameplayTagList.OrderBy(t => t.Tag).ToList();
        }

        /// <summary>
        /// Removes a tag from the config
        /// </summary>
        /// <param name="tag">The tag to remove</param>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            // Remove the tag and all its children
            gameplayTagList.RemoveAll(t => t.Tag == tag || t.Tag.StartsWith(tag + "."));

            // Remove any redirects that reference this tag
            gameplayTagRedirects.RemoveAll(r => r.OldTagName == tag || r.NewTagName == tag);
        }

        /// <summary>
        /// Adds a new tag redirect to the config
        /// </summary>
        /// <param name="oldTag">The tag to redirect from</param>
        /// <param name="newTag">The tag to redirect to</param>
        public void AddRedirect(string oldTag, string newTag)
        {
            if (string.IsNullOrEmpty(oldTag) || string.IsNullOrEmpty(newTag)) return;

            // Check if redirect already exists
            if (gameplayTagRedirects.Exists(r => r.OldTagName == oldTag))
            {
                UnityEngine.Debug.LogWarning($"Redirect for tag '{oldTag}' already exists");
                return;
            }

            // Check if it would create a circular reference
            if (WouldCreateCircularReference(oldTag, newTag))
            {
                UnityEngine.Debug.LogError($"Cannot add redirect from '{oldTag}' to '{newTag}' as it would create a circular reference");
                return;
            }

            gameplayTagRedirects.Add(new GameplayTagRedirect { OldTagName = oldTag, NewTagName = newTag });
            gameplayTagRedirects = gameplayTagRedirects.OrderBy(r => r.OldTagName).ToList();
        }

        /// <summary>
        /// Removes a tag redirect from the config
        /// </summary>
        /// <param name="oldTag">The tag to remove the redirect for</param>
        public void RemoveRedirect(string oldTag)
        {
            gameplayTagRedirects.RemoveAll(r => r.OldTagName == oldTag);
        }

        /// <summary>
        /// Gets the redirected tag for a given tag
        /// </summary>
        /// <param name="tag">The tag to get the redirect for</param>
        /// <returns>The redirected tag, or the original tag if no redirect exists</returns>
        public string GetRedirectedTag(string tag)
        {
            var redirect = gameplayTagRedirects.Find(r => r.OldTagName == tag);
            if (redirect == null) return tag;

            // Follow redirects to their final destination
            var visited = new HashSet<string>();
            var currentTag = redirect.NewTagName;

            while (true)
            {
                if (visited.Contains(currentTag))
                {
                    UnityEngine.Debug.LogError($"Circular reference detected in tag redirects starting from '{tag}'");
                    return tag;
                }

                visited.Add(currentTag);
                var nextRedirect = gameplayTagRedirects.Find(r => r.OldTagName == currentTag);
                if (nextRedirect == null) break;
                currentTag = nextRedirect.NewTagName;
            }

            return currentTag;
        }

        /// <summary>
        /// Checks if adding a redirect from oldTag to newTag would create a circular reference
        /// </summary>
        /// <param name="oldTag">The tag to redirect from</param>
        /// <param name="newTag">The tag to redirect to</param>
        /// <returns>True if adding the redirect would create a circular reference</returns>
        private bool WouldCreateCircularReference(string oldTag, string newTag)
        {
            var visited = new HashSet<string>();
            var currentTag = newTag;

            while (true)
            {
                if (currentTag == oldTag) return true;
                if (visited.Contains(currentTag)) return false;

                visited.Add(currentTag);
                var redirect = gameplayTagRedirects.Find(r => r.OldTagName == currentTag);
                if (redirect == null) break;
                currentTag = redirect.NewTagName;
            }

            return false;
        }

        /// <summary>
        /// Validates all tags in the config
        /// </summary>
        public void ValidateAllTags()
        {
            var invalidTags = new List<GameplayTagDefinition>();
            foreach (var tagDef in gameplayTagList)
            {
                if (tagDef.Tag.IndexOfAny(InvalidTagCharacters.ToCharArray()) != -1)
                {
                    invalidTags.Add(tagDef);
                }
            }

            if (invalidTags.Count > 0)
            {
                UnityEngine.Debug.LogWarning($"Found {invalidTags.Count} invalid tags in config");
                foreach (var tag in invalidTags)
                {
                    UnityEngine.Debug.LogWarning($"Invalid tag: {tag.Tag}");
                }
            }

            // Check for circular references in redirects
            foreach (var redirect in gameplayTagRedirects)
            {
                if (WouldCreateCircularReference(redirect.OldTagName, redirect.NewTagName))
                {
                    UnityEngine.Debug.LogError($"Circular reference detected in redirect from '{redirect.OldTagName}' to '{redirect.NewTagName}'");
                }
            }
        }

        /// <summary>
        /// Checks if a tag exists in the config
        /// </summary>
        /// <param name="tag">The tag to check</param>
        /// <returns>True if the tag exists, false otherwise</returns>
        public bool HasTag(string tag)
        {
            return gameplayTagList.Exists(t => t.Tag == tag);
        }

        /// <summary>
        /// Checks if a tag has child tags
        /// </summary>
        /// <param name="parentTag">The parent tag to check</param>
        /// <returns>True if the tag has child tags, false otherwise</returns>
        public bool HasChildTags(string parentTag)
        {
            return gameplayTagList.Exists(t => t.Tag.StartsWith(parentTag + "."));
        }

        /// <summary>
        /// Gets all child tags of a given parent tag
        /// </summary>
        /// <param name="parentTag">The parent tag to get children for</param>
        /// <returns>List of child tags</returns>
        public IEnumerable<GameplayTagDefinition> GetChildTags(string parentTag)
        {
            return gameplayTagList.Where(t => t.Tag.StartsWith(parentTag + "."));
        }

        private void OnValidate()
        {
            ValidateAllTags();
        }
    }

    /// <summary>
    /// Represents a gameplay tag definition
    /// </summary>
    [System.Serializable]
    public class GameplayTagDefinition
    {
        /// <summary>
        /// The name of the tag
        /// </summary>
        public string Tag;

        /// <summary>
        /// Optional comment for the tag
        /// </summary>
        public string DevComment;
    }

    /// <summary>
    /// Represents a redirect from one tag to another
    /// Used for tag aliasing and deprecation
    /// </summary>
    [System.Serializable]
    public class GameplayTagRedirect
    {
        /// <summary>
        /// The tag to redirect from
        /// </summary>
        public string OldTagName;

        /// <summary>
        /// The tag to redirect to
        /// </summary>
        public string NewTagName;
    }
}

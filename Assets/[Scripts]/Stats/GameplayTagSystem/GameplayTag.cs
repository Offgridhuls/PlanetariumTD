using System;
using UnityEngine;

namespace Planetarium.Stats
{
    /// <summary>
    /// Represents an immutable gameplay tag that can be used to mark and categorize game objects.
    /// Tags use a hierarchical dot notation (e.g., "Category.Subcategory.Name") and can include metadata.
    /// </summary>
    [Serializable]
    public class GameplayTag : IEquatable<GameplayTag>, IComparable<GameplayTag>
    {
        /// <summary>
        /// The full tag name in dot notation (e.g., "Enemy.Flying.Scout")
        /// </summary>
        [SerializeField] private string tagName;
        public string TagName => tagName;

        /// <summary>
        /// Optional developer comment or metadata associated with this tag
        /// </summary>
        [SerializeField] private string devComment;
        public string DevComment => devComment;

        /// <summary>
        /// Creates a new gameplay tag with the specified name and optional comment
        /// </summary>
        /// <param name="tag">Full tag name in dot notation (e.g., "Enemy.Flying")</param>
        /// <param name="comment">Optional metadata or comment about the tag's purpose</param>
        public GameplayTag(string tag, string comment = "")
        {
            tagName = tag?.ToLower() ?? string.Empty;
            devComment = comment;
        }

        /// <summary>
        /// Checks if this tag matches another tag in the hierarchy.
        /// A tag matches if it is exactly equal to the other tag or if it is a child of the other tag.
        /// Example: "Enemy.Flying" matches "Enemy", but "Enemy" does not match "Enemy.Flying"
        /// </summary>
        /// <param name="other">The tag to check against</param>
        /// <returns>True if this tag matches the hierarchy of the other tag</returns>
        public bool Matches(GameplayTag other)
        {
            if (other == null) return false;
            return tagName == other.tagName;
        }

        /// <summary>
        /// Checks if this tag is a child of another tag in the hierarchy.
        /// A tag is a child if its name starts with the parent tag's name followed by a dot.
        /// Example: "Enemy.Flying.Scout" is a child of "Enemy.Flying"
        /// </summary>
        /// <param name="parent">The parent tag to check against</param>
        /// <returns>True if this tag is a child of the parent tag</returns>
        public bool IsChildOf(GameplayTag parent)
        {
            if (parent == null) return false;
            return tagName.StartsWith(parent.tagName + ".");
        }

        /// <summary>
        /// Checks if this tag has a child tag in the hierarchy.
        /// A tag has a child if the child tag's name starts with this tag's name followed by a dot.
        /// Example: "Enemy.Flying" has a child "Enemy.Flying.Scout"
        /// </summary>
        /// <param name="child">The child tag to check against</param>
        /// <returns>True if this tag has a child tag</returns>
        public bool HasChild(GameplayTag child)
        {
            if (child == null) return false;
            return child.tagName.StartsWith(tagName + ".");
        }

        #region Equality and Hash Code
        public bool Equals(GameplayTag other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(tagName, other.tagName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GameplayTag)obj);
        }

        public override int GetHashCode()
        {
            return tagName != null ? tagName.GetHashCode() : 0;
        }

        public static bool operator ==(GameplayTag left, GameplayTag right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GameplayTag left, GameplayTag right)
        {
            return !Equals(left, right);
        }
        #endregion

        public int CompareTo(GameplayTag other)
        {
            if (other == null) return 1;
            return string.Compare(tagName, other.tagName, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return tagName;
        }
    }
}

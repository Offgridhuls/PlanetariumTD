using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Planetarium.Stats
{
    [Serializable]
    public class GameplayTagContainer
    {
        [SerializeField] private List<GameplayTag> tags = new List<GameplayTag>();

        public IReadOnlyList<GameplayTag> Tags => tags;

        public bool AddTag(GameplayTag tag)
        {
            if (tag != null && !HasExactTag(tag))
            {
                tags.Add(tag);
                return true;
            }
            return false;
        }

        public bool RemoveTag(GameplayTag tag)
        {
            if (tag != null)
            {
                int count = tags.RemoveAll(t => t.Matches(tag));
                return count > 0;
            }
            return false;
        }

        public bool HasExactTag(GameplayTag tag)
        {
            return tag != null && tags.Any(t => t.Matches(tag));
        }

        public bool HasTagOrChild(GameplayTag parent)
        {
            if (parent == null) return false;
            return tags.Any(t => t.Matches(parent) || t.IsChildOf(parent));
        }

        public bool HasTagOrParent(GameplayTag child)
        {
            if (child == null) return false;
            return tags.Any(t => t.Matches(child) || child.IsChildOf(t));
        }

        public bool HasAnyTag(GameplayTagContainer container)
        {
            if (container == null) return false;
            return container.Tags.Any(tag => HasExactTag(tag));
        }

        public bool HasAllTags(GameplayTagContainer container)
        {
            if (container == null) return false;
            return container.Tags.All(tag => HasExactTag(tag));
        }

        public bool HasAnyTagInHierarchy(GameplayTagContainer container)
        {
            if (container == null) return false;
            return container.Tags.Any(tag => HasTagOrChild(tag) || HasTagOrParent(tag));
        }

        public bool MatchesQuery(GameplayTagQuery query)
        {
            return query?.Matches(this) ?? false;
        }

        public bool MatchesAnyQuery(params GameplayTagQuery[] queries)
        {
            return queries?.Any(q => q.Matches(this)) ?? false;
        }

        public bool MatchesAllQueries(params GameplayTagQuery[] queries)
        {
            return queries?.All(q => q.Matches(this)) ?? false;
        }

        public IEnumerable<GameplayTag> GetTagsInHierarchy(GameplayTag root)
        {
            if (root == null) return Enumerable.Empty<GameplayTag>();
            return tags.Where(t => t.Matches(root) || t.IsChildOf(root));
        }

        public void GetTags(List<GameplayTag> outTags)
        {
            outTags.Clear();
            outTags.AddRange(tags);
        }

        public void AppendTags(GameplayTagContainer other)
        {
            if (other == null) return;
            foreach (var tag in other.Tags)
            {
                AddTag(tag);
            }
        }

        public void RemoveTags(GameplayTagContainer other)
        {
            if (other == null) return;
            foreach (var tag in other.Tags)
            {
                RemoveTag(tag);
            }
        }

        public void RemoveTagsInHierarchy(GameplayTag root)
        {
            if (root == null) return;
            tags.RemoveAll(t => t.Matches(root) || t.IsChildOf(root));
        }

        public void Clear()
        {
            tags.Clear();
        }
    }
}

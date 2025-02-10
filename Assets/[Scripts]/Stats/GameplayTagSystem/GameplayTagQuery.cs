using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Planetarium.Stats
{
    [Serializable]
    public class GameplayTagQuery
    {
        public enum MatchType
        {
            Exact,          // Tag must match exactly
            Partial,        // Tag can be a child of the query tag
            Any,            // Any of the tags must match
            All,           // All of the tags must match
            None           // None of the tags should match
        }

        [SerializeField] private List<GameplayTag> tags = new List<GameplayTag>();
        [SerializeField] private MatchType matchType = MatchType.Exact;

        public IReadOnlyList<GameplayTag> Tags => tags;
        public MatchType Type => matchType;

        public GameplayTagQuery(MatchType type = MatchType.Exact)
        {
            matchType = type;
        }

        public void AddTag(GameplayTag tag)
        {
            if (tag != null && !tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        public void RemoveTag(GameplayTag tag)
        {
            tags.Remove(tag);
        }

        public bool Matches(GameplayTagContainer container)
        {
            if (container == null || tags.Count == 0) return false;

            switch (matchType)
            {
                case MatchType.Exact:
                    return tags.All(t => container.HasExactTag(t));

                case MatchType.Partial:
                    return tags.All(t => container.HasTagOrChild(t));

                case MatchType.Any:
                    return tags.Any(t => container.HasExactTag(t));

                case MatchType.All:
                    return tags.All(t => container.HasExactTag(t));

                case MatchType.None:
                    return !tags.Any(t => container.HasExactTag(t));

                default:
                    return false;
            }
        }

        public bool Matches(GameplayTag tag)
        {
            if (tag == null || tags.Count == 0) return false;

            switch (matchType)
            {
                case MatchType.Exact:
                    return tags.Any(t => t.Matches(tag));

                case MatchType.Partial:
                    return tags.Any(t => tag.IsChildOf(t));

                case MatchType.Any:
                    return tags.Any(t => t.Matches(tag));

                case MatchType.All:
                    return tags.All(t => t.Matches(tag));

                case MatchType.None:
                    return !tags.Any(t => t.Matches(tag));

                default:
                    return false;
            }
        }

        public static GameplayTagQuery CreateExactQuery(params GameplayTag[] tags)
        {
            var query = new GameplayTagQuery(MatchType.Exact);
            foreach (var tag in tags)
            {
                query.AddTag(tag);
            }
            return query;
        }

        public static GameplayTagQuery CreatePartialQuery(params GameplayTag[] tags)
        {
            var query = new GameplayTagQuery(MatchType.Partial);
            foreach (var tag in tags)
            {
                query.AddTag(tag);
            }
            return query;
        }

        public static GameplayTagQuery CreateAnyQuery(params GameplayTag[] tags)
        {
            var query = new GameplayTagQuery(MatchType.Any);
            foreach (var tag in tags)
            {
                query.AddTag(tag);
            }
            return query;
        }

        public static GameplayTagQuery CreateAllQuery(params GameplayTag[] tags)
        {
            var query = new GameplayTagQuery(MatchType.All);
            foreach (var tag in tags)
            {
                query.AddTag(tag);
            }
            return query;
        }

        public static GameplayTagQuery CreateNoneQuery(params GameplayTag[] tags)
        {
            var query = new GameplayTagQuery(MatchType.None);
            foreach (var tag in tags)
            {
                query.AddTag(tag);
            }
            return query;
        }
    }
}

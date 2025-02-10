using UnityEngine;
using System;
using System.Collections.Generic;
using Planetarium.Core.Messages;
using Planetarium.Stats.Messages;

namespace Planetarium.Stats
{
    public class TaggedStatManager : SceneService
    {
        private static TaggedStatManager instance;
        public static TaggedStatManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<TaggedStatManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("TaggedStatManager");
                        instance = go.AddComponent<TaggedStatManager>();
                    }
                }
                return instance;
            }
        }

        [SerializeField] private List<TaggedStat> registeredStats = new List<TaggedStat>();
        private Dictionary<GameplayTag, object> statValues = new Dictionary<GameplayTag, object>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            instance = this;
            InitializeStats();
        }

        private void InitializeStats()
        {
            foreach (var stat in registeredStats)
            {
                if (!statValues.ContainsKey(stat.StatTag))
                {
                    statValues[stat.StatTag] = stat.GetDefaultValue();
                }
            }
        }

        public void RegisterStat(TaggedStat stat)
        {
            if (!registeredStats.Contains(stat))
            {
                registeredStats.Add(stat);
                if (!statValues.ContainsKey(stat.StatTag))
                {
                    statValues[stat.StatTag] = stat.GetDefaultValue();
                }
            }
        }

        public T GetValue<T>(GameplayTag tag)
        {
            if (statValues.TryGetValue(tag, out object value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default;
        }

        public void SetValue<T>(GameplayTag tag, T value)
        {
            var oldValue = statValues.ContainsKey(tag) ? statValues[tag] : null;
            statValues[tag] = value;
            
            var message = new StatChangedMessage(tag, oldValue, value);
            MessageBus.Instance.Publish(message);
        }

        public void ModifyValue<T>(GameplayTag tag, Func<T, T> modifier)
        {
            var currentValue = GetValue<T>(tag);
            SetValue(tag, modifier(currentValue));
        }

        public void AddValue(GameplayTag tag, int amount) => ModifyValue<int>(tag, x => x + amount);
        public void AddValue(GameplayTag tag, float amount) => ModifyValue<float>(tag, x => x + amount);

        public TaggedStat GetStatDefinition(GameplayTag tag)
        {
            return registeredStats.Find(s => s.StatTag == tag);
        }

        public string GetFormattedValue(GameplayTag tag)
        {
            var stat = GetStatDefinition(tag);
            if (stat == null) return string.Empty;

            var value = statValues.TryGetValue(tag, out object val) ? val : stat.GetDefaultValue();
            return stat.FormatValue(value);
        }
    }
}

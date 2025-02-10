using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    public class StatManager : SceneService
    {
        private static StatManager instance;
        public static StatManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StatManager>();
                    if (instance == null)
                    {
                        Debug.LogWarning("Creating StatManager instance");
                        var go = new GameObject("StatManager");
                        instance = go.AddComponent<StatManager>();
                    }
                }
                return instance;
            }
        }

        [SerializeField] public StatDatabase database;
        
        private Dictionary<StatBase, object> statValues = new Dictionary<StatBase, object>();
        private Dictionary<StatBase, List<Action<object>>> callbacks = new Dictionary<StatBase, List<Action<object>>>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            instance = this;
            Debug.Log("StatManager initialized");

            database.Initialize();
            InitializeStats();
        }

        private void InitializeStats()
        {
            foreach (var stat in database.stats)
            {
                statValues[stat] = stat.GetDefaultValue();
                callbacks[stat] = new List<Action<object>>();
            }
        }

        #region Generic Access Methods
        public T GetOrCreateStat<T>(string statId) where T : StatBase
        {
            var stat = database.GetStat(statId);
            if (stat == null)
            {
                // Create a new stat of type T
                stat = ScriptableObject.CreateInstance<T>();
                stat.name = statId;
                database.AddStat(stat);
                statValues[stat] = stat.GetDefaultValue();
                callbacks[stat] = new List<Action<object>>();
            }
            return (T)stat;
        }

        public T GetValue<T>(string statId)
        {
            var stat = database.GetStat(statId);
            if (stat == null || !statValues.ContainsKey(stat)) return default;
            return (T)statValues[stat];
        }

        public void SetValue<T>(string statId, T value)
        {
            var stat = database.GetStat(statId);
            if (stat == null) return;
            
            statValues[stat] = value;
            NotifyCallbacks(stat, value);
        }

        public void ModifyValue<T>(string statId, Func<T, T> modifier)
        {
            var currentValue = GetValue<T>(statId);
            SetValue(statId, modifier(currentValue));
        }
        #endregion

        #region Specialized Access Methods
        public int GetInt(string statId) => GetValue<int>(statId);
        public void SetInt(string statId, int value) => SetValue(statId, value);
        public void AddInt(string statId, int amount) => ModifyValue<int>(statId, x => x + amount);

        public float GetFloat(string statId) => GetValue<float>(statId);
        public void SetFloat(string statId, float value) => SetValue(statId, value);
        public void AddFloat(string statId, float amount) => ModifyValue<float>(statId, x => x + amount);

        public string GetString(string statId) => GetValue<string>(statId);
        public void SetString(string statId, string value) => SetValue(statId, value);

        public KillStat.KillData GetKillData(string statId) => GetValue<KillStat.KillData>(statId);
        public void SetKillData(string statId, KillStat.KillData value) => SetValue(statId, value);
        public void UpdateKills(string statId, int kills, float damage, float accuracy)
        {
            ModifyValue<KillStat.KillData>(statId, data => new KillStat.KillData
            {
                kills = data.kills + kills,
                damageDealt = data.damageDealt + damage,
                accuracy = (data.accuracy + accuracy) / 2 // Average accuracy
            });
        }
        #endregion

        #region Callback Management
        public void RegisterCallback<T>(string statId, Action<T> callback)
        {
            var stat = database.GetStat(statId);
            if (stat == null) return;

            if (!callbacks.ContainsKey(stat))
            {
                callbacks[stat] = new List<Action<object>>();
            }

            void WrappedCallback(object value) => callback((T)value);
            callbacks[stat].Add(WrappedCallback);

            // Initial callback
            if (statValues.ContainsKey(stat))
            {
                callback((T)statValues[stat]);
            }
        }

        public void UnregisterCallback<T>(string statId, Action<T> callback)
        {
            var stat = database.GetStat(statId);
            if (stat == null || !callbacks.ContainsKey(stat)) return;

            // Note: This is a simplified version. In production, you might want to store
            // the wrapped callback reference to properly remove it.
            callbacks.Remove(stat);
        }

        private void NotifyCallbacks(StatBase stat, object value)
        {
            if (!callbacks.ContainsKey(stat)) return;
            
            foreach (var callback in callbacks[stat])
            {
                callback(value);
            }
        }
        #endregion

        public string GetFormattedValue(string statId)
        {
            var stat = database.GetStat(statId);
            if (stat == null || !statValues.ContainsKey(stat)) return string.Empty;
            
            return stat.FormatValue(statValues[stat]);
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            instance = null;
            statValues.Clear();
            callbacks.Clear();
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    [CreateAssetMenu(fileName = "StatDatabase", menuName = "Planetarium/Stats/Stat Database")]
    public class StatDatabase : ScriptableObject
    {
        public List<StatBase> stats = new List<StatBase>();
        
        private Dictionary<string, StatBase> statLookup;

        public void Initialize()
        {
            // Initialize any required setup
        }

        public StatBase GetStat(string id)
        {
            return stats.Find(s => s.name == id);
        }

        public T GetStat<T>(string id) where T : StatBase
        {
            return GetStat(id) as T;
        }

        public void AddStat(StatBase stat)
        {
            if (!stats.Contains(stat))
            {
                stats.Add(stat);
            }
        }
    }
}

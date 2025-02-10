using UnityEngine;

namespace Planetarium.Stats
{
    [CreateAssetMenu(fileName = "New Stat", menuName = "Planetarium/Stats/Stat Definition")]
    public class StatDefinition : ScriptableObject
    {
        [Header("Display")]
        public string displayName;
        public string format = "{0}"; // e.g. "Wave {0}" or "{0} enemies"
        
        [Header("Type")]
        public StatType type;
        public float initialValue;
        
        public enum StatType
        {
            Integer,
            Float,
            Time
        }
    }
}

using UnityEngine;

namespace Planetarium.Stats
{
    public abstract class StatBase : ScriptableObject
    {
        [Header("Display")]
        public string displayName;
        public string description;
        public string format = "{0}";
        
        public abstract string FormatValue(object value);
        public abstract object GetDefaultValue();
    }
}

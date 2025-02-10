using UnityEngine;
using System;

namespace Planetarium.Stats
{
    [CreateAssetMenu(fileName = "NewTaggedStat", menuName = "Planetarium/Stats/TaggedStat")]
    public class TaggedStat : ScriptableObject
    {
        [SerializeField] private GameplayTag statTag;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private StatValueType valueType;
        [SerializeField] private string format = "{0}";
        [SerializeField] private string defaultValue;

        public GameplayTag StatTag => statTag;
        public string DisplayName => displayName;
        public string Description => description;
        public StatValueType ValueType => valueType;
        public string Format => format;

        public object GetDefaultValue()
        {
            try
            {
                switch (valueType)
                {
                    case StatValueType.Integer:
                        return string.IsNullOrEmpty(defaultValue) ? 0 : int.Parse(defaultValue);
                    case StatValueType.Float:
                        return string.IsNullOrEmpty(defaultValue) ? 0f : float.Parse(defaultValue);
                    case StatValueType.String:
                        return defaultValue ?? string.Empty;
                    case StatValueType.Boolean:
                        return string.IsNullOrEmpty(defaultValue) ? false : bool.Parse(defaultValue);
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                //($"Error parsing default value for stat {name}: {e.Message}");
                return GetDefaultValueForType(valueType);
            }
        }

        private object GetDefaultValueForType(StatValueType type)
        {
            switch (type)
            {
                case StatValueType.Integer: return 0;
                case StatValueType.Float: return 0f;
                case StatValueType.String: return string.Empty;
                case StatValueType.Boolean: return false;
                default: return null;
            }
        }

        public string FormatValue(object value)
        {
            return string.Format(format, value);
        }
    }

    public enum StatValueType
    {
        Integer,
        Float,
        String,
        Boolean
    }
}

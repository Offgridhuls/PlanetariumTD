using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    [CreateAssetMenu(fileName = "New Int Stat", menuName = "Planetarium/Stats/Int Stat")]
    public class IntStat : StatBase
    {
        public int defaultValue;
        public override string FormatValue(object value) => string.Format(format, (int)value);
        public override object GetDefaultValue() => defaultValue;
    }

    [CreateAssetMenu(fileName = "New Float Stat", menuName = "Planetarium/Stats/Float Stat")]
    public class FloatStat : StatBase
    {
        public float defaultValue;
        public override string FormatValue(object value) => string.Format(format, (float)value);
        public override object GetDefaultValue() => defaultValue;
    }

    [CreateAssetMenu(fileName = "New Time Stat", menuName = "Planetarium/Stats/Time Stat")]
    public class TimeStat : StatBase
    {
        public float defaultValue;
        public override object GetDefaultValue() => defaultValue;
        public override string FormatValue(object value)
        {
            float time = (float)value;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format(format, $"{minutes:00}:{seconds:00}");
        }
    }

    [CreateAssetMenu(fileName = "New String Stat", menuName = "Planetarium/Stats/String Stat")]
    public class StringStat : StatBase
    {
        public string defaultValue;
        public override string FormatValue(object value) => string.Format(format, value);
        public override object GetDefaultValue() => defaultValue;
    }

    // Example of a custom stat type
    [CreateAssetMenu(fileName = "New Kill Stat", menuName = "Planetarium/Stats/Kill Stat")]
    public class KillStat : StatBase
    {
        [System.Serializable]
        public struct KillData
        {
            public int kills;
            public float damageDealt;
            public float accuracy;
        }

        public KillData defaultValue;
        
        public override object GetDefaultValue() => defaultValue;
        
        public override string FormatValue(object value)
        {
            var data = (KillData)value;
            return string.Format(format, data.kills, data.damageDealt, data.accuracy);
        }
    }
}

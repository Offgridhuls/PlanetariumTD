using Planetarium.Core.Messages;
using UnityEngine;

namespace Planetarium.Stats.Messages
{
    public class StatChangedMessage : IMessage
    {
        public GameplayTag StatTag { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public float Timestamp { get; }

        public StatChangedMessage(GameplayTag statTag, object oldValue, object newValue)
        {
            StatTag = statTag;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }
}

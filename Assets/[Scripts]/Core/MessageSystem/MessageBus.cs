using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium.Core.Messages
{
    public class MessageBus : SceneService
    {
        private static MessageBus instance;
        public static MessageBus Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("MessageBus");
                    instance = go.AddComponent<MessageBus>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<Type, List<object>> listeners = new Dictionary<Type, List<object>>();
        
        
        
        protected override void OnInitialize()
        {
    
   
        }

        protected override void OnTick()
        {
            
        }

        public void Subscribe<T>(Action<T> listener) where T : IMessage
        {
            var type = typeof(T);
            if (!listeners.ContainsKey(type))
            {
                listeners[type] = new List<object>();
            }
            listeners[type].Add(listener);
        }

        public void Unsubscribe<T>(Action<T> listener) where T : IMessage
        {
            var type = typeof(T);
            if (listeners.ContainsKey(type))
            {
                listeners[type].Remove(listener);
            }
        }

        public void Publish<T>(T message) where T : IMessage
        {
            var type = typeof(T);
            if (!listeners.ContainsKey(type)) return;

            foreach (var listener in listeners[type].ToArray())
            {
                try
                {
                    ((Action<T>)listener).Invoke(message);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error publishing message of type {type}: {e}");
                }
            }
        }
    }
}

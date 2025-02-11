using System;
using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.SaveSystem
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<TValue> values = new List<TValue>();

        public SerializableDictionary() { }

        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            FromDictionary(dict);
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            var dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i < values.Count)
                {
                    dict.Add(keys[i], values[i]);
                }
            }
            return dict;
        }

        public void FromDictionary(Dictionary<TKey, TValue> dict)
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
    }

    [Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int> 
    {
        public StringIntDictionary() : base() { }
        public StringIntDictionary(Dictionary<string, int> dict) : base(dict) { }
    }

    [Serializable]
    public class StringBoolDictionary : SerializableDictionary<string, bool> 
    {
        public StringBoolDictionary() : base() { }
        public StringBoolDictionary(Dictionary<string, bool> dict) : base(dict) { }
    }

    public static class JsonHelper
    {
        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }

        public static string EncryptDecrypt(string data, string key)
        {
            char[] result = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (char)(data[i] ^ key[i % key.Length]);
            }
            return new string(result);
        }
    }
}

﻿﻿
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Extensions.Serialize
{
    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, ReadOnly]
        private List<TKey> keys = new List<TKey>();
        [SerializeField, ReadOnly]
        private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys = new List<TKey>(this.Count);
            values = new List<TValue>(this.Count);

            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            for (int i = 0; i != Mathf.Min(keys.Count, values.Count); i++)
            {
                this.Add(keys[i], values[i]);
            }
        }
    }
}

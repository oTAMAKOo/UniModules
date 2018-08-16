﻿
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;

namespace Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue result;
            return dictionary.TryGetValue(key, out result) ? result : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey1, TKey2, TValue>(this IDictionary<UniRx.Tuple<TKey1, TKey2>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2), out value)
                ? value
                : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey1, TKey2, TKey3, TValue>(this IDictionary<Tuple<TKey1, TKey2, TKey3>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TKey3 tKey3, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2, tKey3), out value)
                ? value
                : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey1, TKey2, TKey3, TKey4, TValue>(this IDictionary<Tuple<TKey1, TKey2, TKey3, TKey4>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TKey3 tKey3, TKey4 tKey4, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2, tKey3, tKey4), out value)
                ? value
                : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(this IDictionary<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TKey3 tKey3, TKey4 tKey4, TKey5 tKey5, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2, tKey3, tKey4, tKey5), out value)
                ? value
                : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey1, TKey2, TKey3, TKey4, TKey5, TKey6, TValue>(this IDictionary<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5, TKey6>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TKey3 tKey3, TKey4 tKey4, TKey5 tKey5, TKey6 tKey6, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2, tKey3, tKey4, tKey5, tKey6), out value)
                ? value
                : defaultValue;
        }


        public static TValue GetValueOrDefault<TKey1, TKey2, TKey3, TKey4, TKey5, TKey6, TKey7, TValue>(this IDictionary<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5, TKey6, TKey7>, TValue> dictionary, TKey1 tKey1, TKey2 tKey2, TKey3 tKey3, TKey4 tKey4, TKey5 tKey5, TKey6 tKey6, TKey7 tKey7, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(UniRx.Tuple.Create(tKey1, tKey2, tKey3, tKey4, tKey5, tKey6, tKey7), out value)
                ? value
                : defaultValue;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = valueFactory(key);
                dictionary.Add(key, value);
            }

            return value;
        }
    }
}
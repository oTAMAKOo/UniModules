
using System;
using System.Collections.Generic;

namespace Extensions
{
    public static partial class DictionaryExtensions
    {
		#if !UNITY_2021_2_OR_NEWER

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue result;

            return dictionary.TryGetValue(key, out result) ? result : defaultValue;
        }

		#endif

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

        /// <summary> デフォルト値か </summary>
        public static bool IsDefault<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair)
        {
            return keyValuePair.Equals(default(KeyValuePair<TKey, TValue>));
        }
    }
}

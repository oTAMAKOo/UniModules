using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.Master
{
    public sealed class CacheData<TKey, TValue> : ICacheData where TKey : struct
    {
        //----- params -----

        //----- field -----

        private Dictionary<TKey, TValue> cache = null;

        private Func<TKey, TValue> function = null;

        //----- property -----

        //----- method -----

        public CacheData(Func<TKey, TValue> function)
        {
            CacheDataManager.Instance.Add(this);

            this.function = function;

            cache = new Dictionary<TKey, TValue>();
        }

        ~CacheData()
        {
            CacheDataManager.Instance.Remove(this);
        }

        public TValue Get(TKey key, TValue defaultValue = default)
        {
            if (function == null) { return defaultValue; }

            if (!cache.ContainsKey(key))
            {
                var value = function.Invoke(key);

                cache[key] = value != null ? value : defaultValue;
            }

            return cache[key];
        }

        public void Clear()
        {
            if (cache != null)
            {
                cache.Clear();
            }
        }
    }
}

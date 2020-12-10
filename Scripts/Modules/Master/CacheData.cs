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

            var value = cache.GetValueOrDefault(key, defaultValue);

            if (value == null)
            {
                cache[key] = function.Invoke(key);
            }

            return value;
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

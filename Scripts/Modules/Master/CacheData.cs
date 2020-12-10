using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.Master
{
    public sealed class CacheData<Tkey, TValue> where Tkey : struct
    {
        //----- params -----

        //----- field -----

        private Dictionary<Tkey, TValue> cache = null;

        private Func<Tkey, TValue> function = null;

        //----- property -----

        //----- method -----

        public CacheData(Func<Tkey, TValue> function)
        {
            this.function = function;

            cache = new Dictionary<Tkey, TValue>();
        }

        public TValue Get(Tkey key, TValue defaultValue = default)
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

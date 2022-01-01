
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.Master
{
    public sealed class Reference : Singleton<Reference>
    {
        //----- params -----

        private interface ICacheContainer
        {
            public void Clear();
        }

        private sealed class CacheContainer<TKey, TValue> : ICacheContainer
        {
            public Dictionary<TKey, TValue> elements = null;
            
            public void Clear()
            {
                elements.Clear();
            }
        }

        //----- field -----

        private Dictionary<Tuple<Type, string>, ICacheContainer> referenceCache = null;

        //----- property -----

        //----- method -----

        private Reference()
        {
            referenceCache = new Dictionary<Tuple<Type, string>, ICacheContainer>();
        }

        public static TResult Get<TRecord, TValue, TResult>(TRecord record, string keyName, Func<TRecord, TValue> keySelector, Func<TValue, TResult> valueSelector)
        {
            var cahce = Instance.referenceCache;

            var cacheKey = Tuple.Create(typeof(TRecord), keyName);

            var container = cahce.GetValueOrDefault(cacheKey) as CacheContainer<TValue, TResult>;

            if (container == null)
            {
                container = new CacheContainer<TValue, TResult>()
                {
                    elements = new Dictionary<TValue, TResult>(),
                };

                cahce[cacheKey] = container;
            }

            var key = keySelector.Invoke(record);

            var value = container.elements.GetValueOrDefault(key);

            if (value == null)
            {
                value = valueSelector.Invoke(key);

                container.elements[key] = value;
            }

            return value;
        }

        public static void Clear()
        {
            var cahce = Instance.referenceCache;

            foreach (var item in cahce.Values)
            {
                if (item == null){ continue; }
                
                item.Clear();
            }

            cahce.Clear();
        }
    }
}
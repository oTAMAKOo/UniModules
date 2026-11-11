
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

        public static TValue Get<TRecord, TKey, TValue>(TRecord record, string keyName, Func<TRecord, TKey> keySelector, Func<TKey, TValue> valueSelector)
        {
            var cahce = Instance.referenceCache;

            var cacheKey = Tuple.Create(typeof(TRecord), keyName);

            var container = cahce.GetValueOrDefault(cacheKey) as CacheContainer<TKey, TValue>;

            if (container == null)
            {
                container = new CacheContainer<TKey, TValue>()
                {
                    elements = new Dictionary<TKey, TValue>(),
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

        public static void Remove<TRecord, TKey, TValue>(TRecord record, string keyName, Func<TRecord, TKey> keySelector)
        {
            var cahce = Instance.referenceCache;

            var cacheKey = Tuple.Create(typeof(TRecord), keyName);

            var container = cahce.GetValueOrDefault(cacheKey) as CacheContainer<TKey, TValue>;

            if (container == null){ return; }

            var key = keySelector.Invoke(record);
            
            if (!container.elements.ContainsKey(key)){ return; }

            container.elements.Remove(key);
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
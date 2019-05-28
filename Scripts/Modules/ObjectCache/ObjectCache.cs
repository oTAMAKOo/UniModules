﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.ObjectCache
{
    /// <summary>
    /// オブジェクトをメモリ上にキャッシュ.
    /// </summary>
    public class ObjectCache<T> : IDisposable where T : class
    {
        //----- params -----

        private class Reference
        {
            public int referenceCount = 0;
            public ObjectCache<T> cacheInstance = null;
        }

        //----- field -----

        private string referenceName = null;
        private Dictionary<string, T> cache = null;

        private static Dictionary<string, Reference> cacheReference = null;

        //----- property -----

        //----- method -----

        public ObjectCache(string referenceName = null)
        {
            this.referenceName = referenceName;

            if (cacheReference == null)
            {
                cacheReference = new Dictionary<string, Reference>();
            }

            if (!string.IsNullOrEmpty(referenceName))
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference == null)
                {
                    reference = new Reference()
                    {
                        referenceCount = 0,
                        cacheInstance = this,
                    };

                    cacheReference.Add(referenceName, reference);
                }
                else
                {
                    reference.referenceCount++;
                }
            }
        }

        ~ObjectCache()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(referenceName))
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference != null)
                {
                    reference.referenceCount--;

                    if (reference.referenceCount <= 0)
                    {
                        cacheReference.Remove(referenceName);
                    }
                }
            }

            GC.SuppressFinalize(this);
        }

        public void Add(string key, T asset)
        {
            if (asset == null) { return; }

            var instance = GetInstance();

            if (instance.cache == null)
            {
                instance.cache = new Dictionary<string, T>();
            }
            
            if (!instance.cache.ContainsKey(key))
            {
                instance.cache.Add(key, asset);
            }
        }

        public void Remove(string key)
        {
            var instance = GetInstance();

            if (!instance.cache.ContainsKey(key))
            {
                instance.cache.Remove(key);
            }

            if (instance.cache.IsEmpty())
            {
                instance.cache = null;
            }
        }

        public void Clear()
        {
            var instance = GetInstance();

            if (instance.cache == null) { return; }

            instance.cache.Clear();
            instance.cache = null;
        }

        public T Get(string key)
        {
            if (cache == null) { return null; }

            return cache.GetValueOrDefault(key);
        }

        public bool HasCache(string key)
        {
            if (cache == null) { return false; }

            return cache.ContainsKey(key);
        }

        private ObjectCache<T> GetInstance()
        {
            ObjectCache<T> cacheInstance = null;

            if (string.IsNullOrEmpty(referenceName))
            {
                cacheInstance = this;
            }
            else
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                cacheInstance = reference.cacheInstance;
            }

            return cacheInstance;
        }
    }
}

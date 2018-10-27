﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.ObjectCache
{
    public interface IObjectCache
    {
        // 参照数を増やす.
        void AddReference();
        // 参照数を減らす.
        void ReleaseReference();
    }

    /// <summary>
    /// オブジェクトをメモリ上にキャッシュ.
    /// </summary>
    public class ObjectCache<T> : IObjectCache where T : UnityEngine.Object
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, T> cache = null;
        private int referenceCount = 0;

        //----- property -----

        //----- method -----

        public void Add(string key, T asset)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, T>();
            }

            if (!cache.ContainsKey(key))
            {
                cache.Add(key, asset);
            }
        }

        public void Remove(string key)
        {
            if (!cache.ContainsKey(key))
            {
                cache.Remove(key);
            }

            if (cache.IsEmpty())
            {
                cache = null;
            }
        }

        public void Clear()
        {
            if (cache == null) { return; }

            cache.Clear();
            cache = null;
        }

        public T Get(string key)
        {
            if (cache == null) { return null; }

            return cache.GetValueOrDefault(key);
        }

        public void AddReference()
        {
            referenceCount++;
        }

        public void ReleaseReference()
        {
            referenceCount--;

            if (referenceCount <= 0)
            {
                Clear();
                referenceCount = 0;
            }
        }
    }
}

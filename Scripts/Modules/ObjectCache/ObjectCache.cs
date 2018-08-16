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

        public void Add(string assetName, T asset)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, T>();
            }

            if (!cache.ContainsKey(assetName))
            {
                cache.Add(assetName, asset);
            }
        }

        public void Remove(string assetName)
        {
            if (!cache.ContainsKey(assetName))
            {
                cache.Remove(assetName);
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

        public T Get(string assetName)
        {
            if (cache == null) { return null; }

            return cache.GetValueOrDefault(assetName);
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
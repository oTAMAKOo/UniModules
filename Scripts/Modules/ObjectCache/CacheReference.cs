
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.ObjectCache
{
    /// <summary>
    /// キャッシュの参照カウントを自動制御して参照がなくなった際に解放するようにする為のサポートクラス.
    /// </summary>
    public class CacheReference : IDisposable
    {
        //----- params -----

        //----- field -----

        private List<IObjectCache> objectCaches = null;

        //----- property -----

        //----- method -----

        public CacheReference()
        {
            objectCaches = new List<IObjectCache>();
        }

        public CacheReference(IObjectCache cache) : this()
        {
            AddReference(cache);
        }

        public CacheReference(IObjectCache[] caches) : this()
        {
            caches.ForEach(x => AddReference(x));
        }

        public void AddReference(IObjectCache cache)
        {
            if (cache != null)
            {
                cache.AddReference();
            }

            objectCaches.Add(cache);
        }

        ~CacheReference()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (objectCaches.Any())
            {
                foreach (var objectCache in objectCaches)
                {
                    objectCache.ReleaseReference();
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Cache
{
    /// <summary>
    /// オブジェクトをメモリ上にキャッシュ.
    /// </summary>
    public sealed class Cache<T> : IDisposable where T : class
    {
        //----- params -----

        private sealed class Reference
        {
            public int referenceCount = 0;
            public Cache<T> cacheInstance = new Cache<T>();
        }

        //----- field -----

        private string referenceName = null;
        private Dictionary<string, T> cache = null;
        private bool disposed = false;

        private static Dictionary<string, Reference> cacheReference = null;

        //----- property -----

        public IReadOnlyList<string> Keys
        {
            get { return cache != null ? cache.Keys.ToArray() : new string[0]; }
        }

		public IReadOnlyList<T> Values
		{
			get { return cache != null ? cache.Values.ToArray() : new T[0]; }
		}

        //----- method -----

        public Cache(string referenceName = null)
        {
            this.referenceName = referenceName;

            if (cacheReference == null)
            {
                cacheReference = new Dictionary<string, Reference>();
            }

            if (!string.IsNullOrEmpty(referenceName))
            {
                lock (cacheReference)
                {
                    var reference = cacheReference.GetValueOrDefault(referenceName);

                    if (reference == null)
                    {
                        reference = CreateNewReference(referenceName);
                    }
                    else
                    {
                        lock (reference)
                        {
                            reference.referenceCount++;
                        }
                    }
                }
            }
        }

        ~Cache()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) { return; }

            if (!string.IsNullOrEmpty(referenceName))
            {
                lock (cacheReference)
                {
                    var reference = cacheReference.GetValueOrDefault(referenceName);

                    if (reference != null)
                    {
                        lock (reference)
                        {
                            reference.referenceCount--;

                            if (reference.referenceCount <= 0)
                            {
                                cacheReference.Remove(referenceName);
                            }
                        }
                    }
                }
            }

            disposed = true;

            GC.SuppressFinalize(this);
        }

		private Reference CreateNewReference(string referenceName)
		{
			lock (cacheReference)
			{
				cacheReference[referenceName] = new Reference();
			}

			return cacheReference[referenceName];
		}

        public void Add(string key, T asset)
        {
            if (asset == null) { return; }

            if (string.IsNullOrEmpty(key)) { return; }

            var instance = GetInstance();

            if (instance.cache == null)
            {
                instance.cache = new Dictionary<string, T>();
            }

            instance.cache[key] = asset;
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) { return; }

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
            if (string.IsNullOrEmpty(key)) { return null; }

            var instance = GetInstance();

            if (instance.cache == null) { return null; }

            return instance.cache.GetValueOrDefault(key);
        }

        public bool HasCache(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }

            var instance = GetInstance();

            if (instance.cache == null) { return false; }

            return instance.Keys.Contains(key);
        }

        private Cache<T> GetInstance()
        {
            Cache<T> cacheInstance = null;

            if (string.IsNullOrEmpty(referenceName))
            {
                cacheInstance = this;
            }
            else
            {
                lock (cacheReference)
                {
                    var reference = cacheReference.GetValueOrDefault(referenceName);

                    if (reference == null)
                    {
                        reference = CreateNewReference(referenceName);
                    }

                    cacheInstance = reference.cacheInstance;
                }
            }

            return cacheInstance;
        }
    }
}

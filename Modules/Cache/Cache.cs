
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Extensions;

namespace Modules.Cache
{
    /// <summary> オブジェクトをメモリ上にキャッシュ. </summary>
    /// <remarks>
    /// referenceName を指定すると同名の Cache 間でキャッシュを共有する.
    /// 共有モードでは参加者を WeakReference で管理し、最終参照者の Dispose 時に実クリアする.
    /// Clear() は共有相手が生存中の場合 no-op となり、戻り値で実クリアの有無を判定できる.
    /// 公開メソッドはスレッドセーフ. ファイナライザは持たないため明示的 Dispose を推奨する.
    /// </remarks>
    public sealed class Cache<T> : IDisposable where T : class
    {
        //----- params -----

        private sealed class Reference
        {
            public readonly object syncRoot = new object();
            public Dictionary<string, T> cache = null;
            public List<WeakReference<Cache<T>>> participants = new List<WeakReference<Cache<T>>>();
        }

        //----- field -----

        private string referenceName = null;
        private Dictionary<string, T> localCache = null;
        private readonly object localSyncRoot = new object();
        private int disposedFlag = 0;

        private static readonly Dictionary<string, Reference> cacheReference = new Dictionary<string, Reference>();

        //----- property -----

        public IReadOnlyList<string> Keys
        {
            get
            {
                if (IsDisposed){ return new string[0]; }

                if (string.IsNullOrEmpty(referenceName))
                {
                    lock (localSyncRoot)
                    {
                        return localCache != null ? localCache.Keys.ToArray() : new string[0];
                    }
                }

                var reference = GetOrCreateReference();

                lock (reference.syncRoot)
                {
                    return reference.cache != null ? reference.cache.Keys.ToArray() : new string[0];
                }
            }
        }

        public IReadOnlyList<T> Values
        {
            get
            {
                if (IsDisposed){ return new T[0]; }

                if (string.IsNullOrEmpty(referenceName))
                {
                    lock (localSyncRoot)
                    {
                        return localCache != null ? localCache.Values.ToArray() : new T[0];
                    }
                }

                var reference = GetOrCreateReference();

                lock (reference.syncRoot)
                {
                    return reference.cache != null ? reference.cache.Values.ToArray() : new T[0];
                }
            }
        }

        private bool IsDisposed
        {
            get { return Volatile.Read(ref disposedFlag) != 0; }
        }

        //----- method -----

        public Cache(string referenceName = null)
        {
            this.referenceName = referenceName;

            if (string.IsNullOrEmpty(referenceName)){ return; }

            lock (cacheReference)
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference == null)
                {
                    reference = new Reference();
                    cacheReference[referenceName] = reference;
                }

                lock (reference.syncRoot)
                {
                    PruneDeadParticipants(reference);
                    reference.participants.Add(new WeakReference<Cache<T>>(this));
                }
            }
        }

        public void Dispose()
        {
            // 多重 Dispose を防止しつつアトミックにフラグを立てる.
            if (Interlocked.Exchange(ref disposedFlag, 1) != 0){ return; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null){ return; }

                    localCache.Clear();
                    localCache = null;
                }

                return;
            }

            lock (cacheReference)
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference == null){ return; }

                lock (reference.syncRoot)
                {
                    RemoveSelfFromParticipants(reference);
                    PruneDeadParticipants(reference);

                    if (!reference.participants.IsEmpty()){ return; }

                    if (reference.cache != null)
                    {
                        reference.cache.Clear();
                        reference.cache = null;
                    }

                    cacheReference.Remove(referenceName);
                }
            }
        }

        public void Add(string key, T asset)
        {
            if (IsDisposed){ return; }

            if (asset == null){ return; }

            if (string.IsNullOrEmpty(key)){ return; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null)
                    {
                        localCache = new Dictionary<string, T>();
                    }

                    localCache[key] = asset;
                }

                return;
            }

            var reference = GetOrCreateReference();

            lock (reference.syncRoot)
            {
                if (reference.cache == null)
                {
                    reference.cache = new Dictionary<string, T>();
                }

                reference.cache[key] = asset;
            }
        }

        public void Remove(string key)
        {
            if (IsDisposed){ return; }

            if (string.IsNullOrEmpty(key)){ return; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null){ return; }

                    localCache.Remove(key);

                    if (localCache.IsEmpty())
                    {
                        localCache = null;
                    }
                }

                return;
            }

            var reference = GetOrCreateReference();

            lock (reference.syncRoot)
            {
                if (reference.cache == null){ return; }

                reference.cache.Remove(key);

                if (reference.cache.IsEmpty())
                {
                    reference.cache = null;
                }
            }
        }

        /// <summary> キャッシュをクリア. 共有モードでは自身が最終参照者の時のみ実クリアし、戻り値で実クリアの有無を返す. </summary>
        public bool Clear()
        {
            if (IsDisposed){ return false; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null){ return false; }

                    localCache.Clear();
                    localCache = null;
                }

                return true;
            }

            lock (cacheReference)
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference == null){ return false; }

                lock (reference.syncRoot)
                {
                    PruneDeadParticipants(reference);

                    // 最終参照者でなければ実クリアしない.
                    if (!IsLastLivingParticipant(reference)){ return false; }

                    if (reference.cache == null){ return false; }

                    reference.cache.Clear();
                    reference.cache = null;

                    return true;
                }
            }
        }

        public T Get(string key)
        {
            if (IsDisposed){ return null; }

            if (string.IsNullOrEmpty(key)){ return null; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null){ return null; }

                    return localCache.GetValueOrDefault(key);
                }
            }

            var reference = GetOrCreateReference();

            lock (reference.syncRoot)
            {
                if (reference.cache == null){ return null; }

                return reference.cache.GetValueOrDefault(key);
            }
        }

        public bool HasCache(string key)
        {
            if (IsDisposed){ return false; }

            if (string.IsNullOrEmpty(key)){ return false; }

            if (string.IsNullOrEmpty(referenceName))
            {
                lock (localSyncRoot)
                {
                    if (localCache == null){ return false; }

                    return localCache.ContainsKey(key);
                }
            }

            var reference = GetOrCreateReference();

            lock (reference.syncRoot)
            {
                if (reference.cache == null){ return false; }

                return reference.cache.ContainsKey(key);
            }
        }

        private Reference GetOrCreateReference()
        {
            lock (cacheReference)
            {
                var reference = cacheReference.GetValueOrDefault(referenceName);

                if (reference == null)
                {
                    reference = new Reference();
                    cacheReference[referenceName] = reference;

                    lock (reference.syncRoot)
                    {
                        reference.participants.Add(new WeakReference<Cache<T>>(this));
                    }

                    return reference;
                }

                lock (reference.syncRoot)
                {
                    PruneDeadParticipants(reference);

                    if (!IsSelfInParticipants(reference))
                    {
                        reference.participants.Add(new WeakReference<Cache<T>>(this));
                    }
                }

                return reference;
            }
        }

        private bool IsSelfInParticipants(Reference reference)
        {
            foreach (var weak in reference.participants)
            {
                if (!weak.TryGetTarget(out var target)){ continue; }

                if (ReferenceEquals(target, this)){ return true; }
            }

            return false;
        }

        private void RemoveSelfFromParticipants(Reference reference)
        {
            for (var i = reference.participants.Count - 1; i >= 0; i--)
            {
                var weak = reference.participants[i];

                if (!weak.TryGetTarget(out var target)){ continue; }

                if (!ReferenceEquals(target, this)){ continue; }

                reference.participants.RemoveAt(i);

                return;
            }
        }

        private bool IsLastLivingParticipant(Reference reference)
        {
            var liveCount = 0;

            foreach (var weak in reference.participants)
            {
                if (!weak.TryGetTarget(out var target)){ continue; }

                if (target.IsDisposed){ continue; }

                liveCount++;

                if (liveCount > 1){ return false; }
            }

            return liveCount <= 1;
        }

        private static void PruneDeadParticipants(Reference reference)
        {
            for (var i = reference.participants.Count - 1; i >= 0; i--)
            {
                var weak = reference.participants[i];

                if (weak.TryGetTarget(out var target) && !target.IsDisposed){ continue; }

                reference.participants.RemoveAt(i);
            }
        }
    }
}

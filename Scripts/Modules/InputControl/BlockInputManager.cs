
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.LogHandler;

namespace Modules.InputControl
{
    public sealed partial class BlockInputManager : Singleton<BlockInputManager>
	{
        //----- params -----

        //----- field -----

        private ulong nextBlockingId = 0;

        private HashSet<ulong> blockingIds = new HashSet<ulong>();
        private Subject<bool> onUpdateStatus = null;

        //----- property -----

        public bool IsBlocking { get { return blockingIds.Any(); } }

        //----- method -----
        
        protected override void OnCreate()
        {
            // Exception”­¶Žž‚É‹­§‰ðœ.
            ApplicationLogHandler.Instance.OnReceivedExceptionAsObservable()
                .Subscribe(_ => ForceUnlock())
                .AddTo(Disposable);
        }

        public ulong GetNextBlockingId()
        {
            nextBlockingId++;

            return nextBlockingId;
        }

        public void Lock(ulong blockingId)
        {
            var isBlocking = IsBlocking;

            blockingIds.Add(blockingId);

            #if UNITY_EDITOR

            AddTracker(blockingId);

            #endif

            if (isBlocking != IsBlocking && onUpdateStatus != null)
            {
                onUpdateStatus.OnNext(IsBlocking);
            }
        }

        public void Unlock(ulong blockingId)
        {
            if (!IsBlocking) { return; }

            var isBlocked = IsBlocking;

            blockingIds.Remove(blockingId);

            #if UNITY_EDITOR

            RemoveTracker(blockingId);

            #endif

            if (isBlocked != IsBlocking && onUpdateStatus != null)
            {
                onUpdateStatus.OnNext(IsBlocking);
            }
        }

        public void ForceUnlock()
        {
            if (!IsBlocking) { return; }

            var isBlocked = IsBlocking;

            blockingIds.Clear();
            
            #if UNITY_EDITOR

            ClearTracker();

            #endif

            if (isBlocked != IsBlocking && onUpdateStatus != null)
            {
                onUpdateStatus.OnNext(IsBlocking);
            }
        }

        public IObservable<bool> OnUpdateStatusAsObservable()
        {
            return onUpdateStatus ?? (onUpdateStatus = new Subject<bool>());
        }
    }
}
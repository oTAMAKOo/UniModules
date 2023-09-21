
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using Modules.Devkit.LogHandler;

namespace Modules.InputControl
{
    public sealed class BlockInputManager : Singleton<BlockInputManager>
	{
        //----- params -----

		//----- field -----

        private ulong nextBlockingId = 0;

        private HashSet<ulong> blockingIds = new HashSet<ulong>();
        private Subject<bool> onUpdateStatus = null;

		private Dictionary<ulong, string> trackInputBlock = null;

		//----- property -----

		/// <summary> 入力制限中か </summary>
        public bool IsBlocking { get { return blockingIds.Any(); } }

        //----- method -----
        
        protected override void OnCreate()
        {
			trackInputBlock = new Dictionary<ulong, string>();

            // Exception発生時に強制解除.
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

			AddTracker(blockingId);

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

			RemoveTracker(blockingId);

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

			ClearTracker();

            if (isBlocked != IsBlocking && onUpdateStatus != null)
            {
                onUpdateStatus.OnNext(IsBlocking);
            }
        }

		#region Tracking

		private void AddTracker(ulong blockingId)
        {
			if (!Debug.isDebugBuild) { return; }

			// 実際の呼び出し元開始行数.
            const int StackTraceStartLine = 4;
            
            var stackTrace = StackTraceUtility.ExtractStackTrace();

            stackTrace = stackTrace.FixLineEnd();

            var lines = stackTrace.Split('\n').ToList();

            var builder = new StringBuilder();

            for (var i = 0; i < lines.Count; i++)
            {
                if (i < StackTraceStartLine){ continue; }

                builder.AppendLine(lines[i]);
            }

            stackTrace = builder.ToString().FixLineEnd().Trim();

            trackInputBlock[blockingId] = stackTrace;
        }

        private void RemoveTracker(ulong blockingId)
        {
			if (!Debug.isDebugBuild) { return; }

            if (trackInputBlock == null){ return; }

            if (trackInputBlock.ContainsKey(blockingId))
            {
                trackInputBlock.Remove(blockingId);
            }
        }

        private void ClearTracker()
        {
			if (!Debug.isDebugBuild) { return; }

            if (trackInputBlock == null){ return; }

            trackInputBlock.Clear();
        }

        public IReadOnlyDictionary<ulong, string> GetTrackContents()
        {
            return trackInputBlock;
        }

		#endregion

		public IObservable<bool> OnUpdateStatusAsObservable()
        {
            return onUpdateStatus ?? (onUpdateStatus = new Subject<bool>());
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.TimeUtil
{
    /// <summary> 定期通知の発火理由 </summary>
    public enum TickReason
    {
        /// <summary> 開始直後の初回通知 </summary>
        Initial,
        /// <summary> 間隔の境界を跨いだときの通知 </summary>
        Periodic,
        /// <summary> 一時停止を解除したときの通知 </summary>
        Resume,
    }

    /// <summary> 定期通知の内容 </summary>
    public readonly struct TickInfo
    {
        /// <summary> 通知の間隔(秒) </summary>
        public int IntervalSeconds { get; }

        /// <summary> 今回の時刻(秒) </summary>
        public long Time { get; }

        /// <summary> 発火理由 </summary>
        public TickReason Reason { get; }

        public TickInfo(int intervalSeconds, long time, TickReason reason)
        {
            IntervalSeconds = intervalSeconds;
            Time = time;
            Reason = reason;
        }
    }

    /// <summary>
    /// 時刻の境界ごとに順番どおり通知する定期通知の基底.
    /// <para> 間隔 N 秒の購読は、時刻が N の倍数を跨いだフレームで 1 回通知される (1 フレームで複数の境界を跨いでも 1 回). </para>
    /// <para> 購読先は order の小さい順、同じ order は購読順に呼ばれる. 1 件の購読先の例外は他の購読先へ波及しない. </para>
    /// <para> StartTick で全購読先へ初回通知を出してから時刻の判定を始める. StartTick より後に購読した購読先には初回通知は届かない. </para>
    /// <para> 停止 (StopTick / Refresh) しても購読は保持する. 購読の解除は購読側の Dispose で行う. </para>
    /// </summary>
    public abstract class PeriodicTickManager<TInstance> : Singleton<TInstance> where TInstance : PeriodicTickManager<TInstance>
    {
        //----- params -----

        /// <summary> 順番を省略した購読の順番 (順番を指定した購読より後に呼ぶ) </summary>
        public const int DefaultObserverOrder = int.MaxValue;

        private const string ConsoleEventName = "PeriodicTick";

        private static readonly Color ConsoleEventColor = new Color(0.6f, 0.8f, 1f);

        private sealed class Entry
        {
            public int IntervalSeconds { get; set; }
            public int Order { get; set; }
            public long Sequence { get; set; }
            public Action<TickInfo> Action { get; set; }
            public string Label { get; set; }
            public bool IsDisposed { get; set; }
        }

        //----- field -----

        private List<Entry> entries = new List<Entry>();

        private Entry[] snapshot = null;

        private long sequence = 0;

        private long lastTime = 0;

        private bool initialPending = false;

        private bool dispatching = false;

        private IDisposable frameDisposable = null;

        //----- property -----

        /// <summary> 稼働中か (StartTick から StopTick まで) </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary> 一時停止中か (SuspendTick から ResumeTick まで) </summary>
        public bool IsSuspended { get; private set; } = false;

        /// <summary> 時刻を判定するフレーム </summary>
        protected virtual FrameProvider TickFrameProvider { get { return UnityFrameProvider.PreLateUpdate; } }

        //----- method -----

        /// <summary> 現在時刻(秒)を返す. 0 以下は時刻未確定として通知しない </summary>
        protected abstract long GetCurrentTime();

        /// <summary> 1 回の通知処理全体を囲むスコープを生成 (null なら囲まない) </summary>
        protected virtual IDisposable CreateDispatchScope() { return null; }

        /// <summary> 開始時処理 (初回通知より前に呼ばれる) </summary>
        protected virtual void OnStartTick() { }

        /// <summary> 停止時処理 </summary>
        protected virtual void OnStopTick() { }

        /// <summary> 定期通知を購読. order の小さい順、同じ order は購読順に通知する </summary>
        public Observable<TickInfo> OnTickAsObservable(int intervalSeconds, int order = DefaultObserverOrder)
        {
            return OnTickAsObservable(intervalSeconds, order, null);
        }

        /// <summary> 定期通知を購読 (label は例外ログの識別用) </summary>
        protected Observable<TickInfo> OnTickAsObservable(int intervalSeconds, int order, string label)
        {
            if (intervalSeconds <= 0){ throw new ArgumentOutOfRangeException(nameof(intervalSeconds)); }

            return Observable.Create<TickInfo>(observer => AddEntry(intervalSeconds, order, x => observer.OnNext(x), label));
        }

        /// <summary> 全購読先へ初回通知を発火してから時刻の判定を開始 (時刻未確定なら確定したフレームで初回通知). 稼働中は何もしない </summary>
        public void StartTick()
        {
            if (IsRunning){ return; }

            IsRunning = true;
            IsSuspended = false;

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Start");

            OnStartTick();

            var now = GetCurrentTime();

            if (0 < now)
            {
                lastTime = now;

                DispatchAll(now, TickReason.Initial);
            }
            else
            {
                initialPending = true;
            }

            // 初回通知の購読先が停止した場合は時刻の判定を始めない.
            if (!IsRunning){ return; }

            frameDisposable = Observable.EveryUpdate(TickFrameProvider)
                .Subscribe(_ => OnFrame());
        }

        /// <summary> 時刻の判定を停止. 購読は保持する </summary>
        public void StopTick()
        {
            if (!IsRunning){ return; }

            IsRunning = false;
            IsSuspended = false;
            initialPending = false;

            if (frameDisposable != null)
            {
                frameDisposable.Dispose();
                frameDisposable = null;
            }

            OnStopTick();

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Stop");
        }

        /// <summary> 時刻の判定を一時停止 </summary>
        public void SuspendTick()
        {
            if (!IsRunning || IsSuspended){ return; }

            IsSuspended = true;

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Suspend");
        }

        /// <summary> 一時停止を解除し全購読先へ再開通知を発火. 一時停止中でなければ何もしない </summary>
        public void ResumeTick()
        {
            if (!IsRunning || !IsSuspended){ return; }

            IsSuspended = false;

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Resume");

            // 初回通知が済んでいなければ時刻確定時の初回通知に任せる.
            if (initialPending){ return; }

            var now = GetCurrentTime();

            if (now <= 0){ return; }

            lastTime = now;

            DispatchAll(now, TickReason.Resume);
        }

        protected override void OnRefresh()
        {
            StopTick();
        }

        protected override void OnRelease()
        {
            StopTick();
        }

        protected override void OnDispose()
        {
            StopTick();
        }

        private IDisposable AddEntry(int intervalSeconds, int order, Action<TickInfo> action, string label)
        {
            var entry = new Entry()
            {
                IntervalSeconds = intervalSeconds,
                Order = order,
                Sequence = sequence++,
                Action = action,
                Label = label,
            };

            entries.Add(entry);

            snapshot = null;

            return R3.Disposable.Create(() =>
                {
                    entry.IsDisposed = true;

                    entries.Remove(entry);

                    snapshot = null;
                });
        }

        private void OnFrame()
        {
            if (IsSuspended){ return; }

            var now = GetCurrentTime();

            if (now <= 0){ return; }

            if (initialPending)
            {
                initialPending = false;

                lastTime = now;

                DispatchAll(now, TickReason.Initial);

                return;
            }

            // 秒が変わっていない.
            if (now == lastTime){ return; }

            // 時刻が戻った場合は通知せず基準だけ合わせる.
            if (now < lastTime)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Time rewound. ({lastTime} -> {now})", LogType.Warning);

                lastTime = now;

                return;
            }

            var previous = lastTime;

            lastTime = now;

            Dispatch(now, TickReason.Periodic, x => (now / x) != (previous / x));
        }

        private void DispatchAll(long now, TickReason reason)
        {
            Dispatch(now, reason, x => true);
        }

        private void Dispatch(long now, TickReason reason, Func<int, bool> isDue)
        {
            if (dispatching)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Dispatch reentered.", LogType.Warning);

                return;
            }

            if (snapshot == null)
            {
                snapshot = entries.OrderBy(x => x.Order).ThenBy(x => x.Sequence).ToArray();
            }

            var targets = snapshot;

            dispatching = true;

            try
            {
                using (CreateDispatchScope())
                {
                    foreach (var entry in targets)
                    {
                        // 通知中に停止された場合は残りを呼ばない.
                        if (!IsRunning){ break; }

                        if (entry.IsDisposed){ continue; }

                        if (!isDue(entry.IntervalSeconds)){ continue; }

                        var info = new TickInfo(entry.IntervalSeconds, now, reason);

                        // 1 件の例外で他の購読先の呼び出しを止めない.
                        try
                        {
                            entry.Action(info);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Periodic tick handler failed. ({entry.Label})");
                            Debug.LogException(e);
                        }
                    }
                }
            }
            finally
            {
                dispatching = false;
            }
        }
    }
}


using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UniRxExtension
{
    public sealed class ObservableQueue
    {
        //----- params -----

        private class QueueItem
        {
            public IObservable<Unit> Observable { get; private set; }
            public Action OnStart { get; private set; }
            public Action OnComplete { get; private set; }
            public Action<Exception> OnError { get; private set; }

            public QueueItem(IObservable<Unit> observable, Action onStart, Action onComplete, Action<Exception> onError)
            {
                Observable = observable;
                OnStart = onStart;
                OnComplete = onComplete;
                OnError = onError;
            }
        }

        //----- field -----

        // 同時実行数.
        private int maxParallelizationCount = 0;
        // 実行待ちキュー.
        private Queue<QueueItem> processingQueue = null;
        // 実行中リスト.
        private List<QueueItem> runningList = null;

        // 完了イベント.
        private Subject<Unit> onComplete = null;

        //----- property -----

        //----- method -----

        public ObservableQueue(int maxParallelizationCount = 3)
        {
            this.maxParallelizationCount = maxParallelizationCount;

            processingQueue = new Queue<QueueItem>();
            runningList = new List<QueueItem>();
        }

        public void Add(IObservable<Unit> observable, Action onStart = null, Action onFinish = null, Action<Exception> onError = null)
        {
            processingQueue.Enqueue(new QueueItem(observable, onStart, onFinish, onError));
        }

        public IObservable<Unit> Start()
        {
            var observers = processingQueue.Select(x => Observable.FromMicroCoroutine(() => ExecProcess(x))).ToArray();

            return observers.WhenAll()                
                .Do(_ =>
                    {
                        if (onComplete != null)
                        {
                            onComplete.OnNext(Unit.Default);
                        }
                    })
                .Finally(() => Clear())
                .AsUnitObservable();
        }

        public void Clear()
        {
            runningList.Clear();
            processingQueue.Clear();
        }

        private IEnumerator ExecProcess(QueueItem queueItem)
        {
            var waitYield = WaitQueueProcess(queueItem).ToYieldInstruction();

            while (!waitYield.IsDone)
            {
                yield return null;
            }

            if (queueItem.OnStart != null)
            {
                queueItem.OnStart();
            }

            runningList.Add(queueItem);

            var itemYield = queueItem.Observable.ToYieldInstruction();

            while (!itemYield.IsDone)
            {
                yield return null;
            }

            if (itemYield.HasError)
            {
                if (queueItem.OnError != null)
                {
                    queueItem.OnError(itemYield.Error);
                }
            }
            else
            {
                if (queueItem.OnComplete != null)
                {
                    queueItem.OnComplete();
                }
            }

            runningList.Remove(queueItem);
        }

        private IEnumerator WaitQueueProcess(QueueItem queueItem)
        {
            while (true)
            {
                // キューが空になっていた場合はキャンセル扱い.
                if (processingQueue.IsEmpty()) { break; }

                // 同時実行数以下 & キューの先頭が自身の場合待ち終了.
                if (runningList.Count < maxParallelizationCount && processingQueue.Peek() == queueItem)
                {
                    processingQueue.Dequeue();
                    break;
                }

                yield return null;
            }
        }

        public IObservable<Unit> OnCompleteAsObservable()
        {
            return onComplete ?? (onComplete = new Subject<Unit>());
        }
    }
}

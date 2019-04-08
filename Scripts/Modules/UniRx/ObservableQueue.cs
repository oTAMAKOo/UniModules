
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UniRxExtension
{
    public class ObservableQueue<T>
    {
        //----- params -----

        private class QueueItem
        {
            public IObservable<T> Observable { get; private set; }

            public QueueItem(IObservable<T> observable)
            {
                Observable = observable;
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
        private Subject<T> onComplete = null;

        //----- property -----

        //----- method -----

        public ObservableQueue(int maxParallelizationCount = 3)
        {
            this.maxParallelizationCount = maxParallelizationCount;

            processingQueue = new Queue<QueueItem>();
            runningList = new List<QueueItem>();
        }

        public void Add(IObservable<T> observable)
        {
            processingQueue.Enqueue(new QueueItem(observable));
        }

        public IObservable<Unit> Start()
        {
            var observables = processingQueue.Select(x => Observable.FromMicroCoroutine(() => ExecProcess(x))).ToArray();

            return observables.WhenAll()
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

            runningList.Add(queueItem);

            var itemYield = queueItem.Observable.ToYieldInstruction();

            while (!itemYield.IsDone)
            {
                yield return null;
            }

            if (itemYield.HasResult)
            {
                if (onComplete != null)
                {
                    onComplete.OnNext(itemYield.Result);
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

        public IObservable<T> OnCompleteAsObservable()
        {
            return onComplete ?? (onComplete = new Subject<T>());
        }
    }
}


using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Modules.SceneManagement
{
    public sealed class WaitHandler : IDisposable
    {
        //----- params -----

        //----- field -----

        public int? identifier = null;
        private Subject<Unit> onDispose = null;

        //----- property -----

        public int Identifier
        {
            get { return (int)(identifier ?? (identifier = GetHashCode())); }
        }

        //----- method -----

        ~WaitHandler()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (onDispose != null)
            {
                onDispose.OnNext(Unit.Default);
            }

            GC.SuppressFinalize(this);
        }

        public IObservable<Unit> OnDisposeAsObservable()
        {
            return onDispose ?? (onDispose = new Subject<Unit>());
        }
    }

    public partial class SceneManagement<T>
    {
        //----- params -----

        //----- field -----

        private HashSet<int> waitHandlerIds = null;

        //----- property -----

        //----- method -----

        /// <summary>
        /// 遷移中に外部処理の待機開始.
        /// </summary>
        public WaitHandler BeginWait()
        {
            var waitHandler = new WaitHandler();

            waitHandler.OnDisposeAsObservable()
                .Subscribe(_ => FinishWait(waitHandler))
                .AddTo(Disposable);

            waitHandlerIds.Add(waitHandler.Identifier);

            return waitHandler;
        }

        /// <summary>
        /// <see cref="BeginWait"/>から取得された<see cref="WaitHandler"/>で待ち状態を解除.
        /// </summary>
        public void FinishWait(WaitHandler waitHandler)
        {
            if (waitHandler == null) { return; }

            if (!waitHandlerIds.Contains(waitHandler.Identifier)) { return; }

            waitHandlerIds.Remove(waitHandler.Identifier);
        }

        /// <summary> 全ての外部の処理待ちをキャンセル </summary>
        public void CancelAllTransitionWait()
        {
            waitHandlerIds.Clear();
        }

        /// <summary> 外部の処理待ち </summary>
        private async UniTask TransitionWait()
        {
            await UniTask.WaitWhile(() => waitHandlerIds.Any());
        }
    }
}

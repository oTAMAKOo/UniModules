
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;

namespace Modules.SceneManagement
{
    public class WaitEntity : IDisposable
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

        ~WaitEntity()
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

        private HashSet<int> waitEntityIds = null;

        //----- property -----

        //----- method -----


        /// <summary>
        /// 遷移中に外部処理の待機開始.
        /// </summary>
        public WaitEntity BeginWait()
        {
            var entity = new WaitEntity();

            entity.OnDisposeAsObservable()
                .Subscribe(_ => FinishWait(entity))
                .AddTo(Disposable);

            waitEntityIds.Add(entity.Identifier);

            return entity;
        }

        /// <summary>
        /// <see cref="BeginWait"/>から取得された<see cref="WaitEntity"/>で待ち状態を解除.
        /// </summary>
        public void FinishWait(WaitEntity entity)
        {
            if (entity == null) { return; }

            if (!waitEntityIds.Contains(entity.Identifier)) { return; }

            waitEntityIds.Remove(entity.Identifier);
        }

        /// <summary> 全ての外部の処理待ちをキャンセル </summary>
        public void CancelAllTransitionWait()
        {
            waitEntityIds.Clear();
        }

        /// <summary> 外部の処理待ち </summary>
        private IEnumerator TransitionWait()
        {
            while (waitEntityIds.Any())
            {
                yield return null;
            }
        }
    }
}

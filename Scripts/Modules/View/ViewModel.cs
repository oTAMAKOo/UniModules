
using System;
using Extensions;
using UniRx;

namespace Modules.View
{
    public abstract class ViewModel : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        private Subject<Unit> onDispose = null;

        //----- property -----

        //----- method -----

        protected override void OnDispose()
        {
            if (onDispose != null)
            {
                onDispose.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnDisposeAsObservable()
        {
            return onDispose ??= new Subject<Unit>();
        }
    }
}

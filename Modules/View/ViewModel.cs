
using System;
using Extensions;
using R3;

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

        public Observable<Unit> OnDisposeAsObservable()
        {
            return onDispose ??= new Subject<Unit>();
        }
    }
}

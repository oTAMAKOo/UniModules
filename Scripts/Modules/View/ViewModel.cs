
using System;
using UniRx;
using Extensions;

namespace Modules.View
{
    public abstract class ViewModel : IDisposable
    {
        //----- params -----

        //----- field -----

        private LifetimeDisposable lifetimeDisposable = null;

        //----- property -----

        protected CompositeDisposable Disposable
        {
            get { return lifetimeDisposable != null ? lifetimeDisposable.Disposable : null; }
        }

        public bool IsDisposed { get; private set; }

        //----- method -----

        protected ViewModel()
        {
            lifetimeDisposable = new LifetimeDisposable();

            IsDisposed = false;
        }

        ~ViewModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;

            if (lifetimeDisposable != null)
            {
                lifetimeDisposable.Dispose();
                lifetimeDisposable = null;
            }

            OnDispose();

            GC.SuppressFinalize(this);
        }

        protected virtual void OnDispose() { }
    }
}

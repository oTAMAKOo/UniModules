
using System;
using System.Linq;
using System.Collections.Generic;
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
            get { return lifetimeDisposable.Disposable; }
        }

        public bool Disposed { get; private set; }

        //----- method -----

        protected ViewModel()
        {
            lifetimeDisposable = new LifetimeDisposable();
        }

        ~ViewModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            Disposed = true;

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

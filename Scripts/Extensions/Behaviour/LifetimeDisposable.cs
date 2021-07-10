﻿﻿
using System;
using UniRx;

namespace Extensions
{
    public class LifetimeDisposable : IDisposable
    {
        //----- params -----

        //----- field -----

        private CompositeDisposable disposables = null;

        //----- property -----

        public bool IsDisposed { get; private set; }

        public CompositeDisposable Disposable
        {
            get { return disposables ?? (disposables = new CompositeDisposable()); }
        }

        //----- method -----

        public LifetimeDisposable()
        {
            IsDisposed = false;
        }

        ~LifetimeDisposable()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;

            OnDispose();

            if (disposables != null)
            {
                disposables.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        protected virtual void OnDispose(){ }
    }
}

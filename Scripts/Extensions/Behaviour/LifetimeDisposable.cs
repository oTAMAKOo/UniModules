﻿﻿
using System;
using UniRx;

namespace Extensions
{
    public class LifetimeDisposable : IDisposable
    {
        //----- params -----

        //----- field -----

        private CompositeDisposable disposables;

        //----- property -----

        public CompositeDisposable Disposable
        {
            get { return disposables ?? (disposables = new CompositeDisposable()); }
        }

        //----- method -----

        ~LifetimeDisposable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposables != null)
            {
                disposables.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
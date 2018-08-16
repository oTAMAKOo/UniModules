
using System;
using UniRx;

namespace Modules.UniRxExtension
{
    public class YieldCancell : IDisposable
    {
        //----- params -----

        //----- field -----

        private BooleanDisposable cancelDisposable = null;
        private CancellationToken cancellationToken;

        //----- property -----

        public CancellationToken Token { get { return cancellationToken; } }

        public bool IsCancelled { get { return cancellationToken.IsCancellationRequested; } }

        //----- method -----

        public YieldCancell()
        {
            cancelDisposable = new BooleanDisposable();
            cancellationToken = new CancellationToken(cancelDisposable);
        }

        ~YieldCancell()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (cancelDisposable != null)
            {
                cancelDisposable.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}

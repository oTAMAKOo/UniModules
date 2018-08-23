
using System;
using System.Threading;
using UniRx;

namespace Modules.UniRxExtension
{
    public class YieldCancell : IDisposable
    {
        //----- params -----

        //----- field -----
        
        private CancellationTokenSource cancellationToken = null;

        //----- property -----

        public CancellationToken Token { get { return cancellationToken.Token; } }

        public bool IsCancelled { get { return cancellationToken.IsCancellationRequested; } }

        //----- method -----

        public YieldCancell()
        {
            cancellationToken = new CancellationTokenSource();
        }

        ~YieldCancell()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
                cancellationToken = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}

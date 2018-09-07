
using System;
using System.Threading;
using UniRx;

namespace Modules.UniRxExtension
{
    #if !(NETFX_CORE || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA_10_0)

    public class YieldCancell : IDisposable
    {
        //----- params -----

        //----- field -----

        private BooleanDisposable cancelDisposable = null;
        private CancellationToken cancellationToken;

        //----- property -----

        public bool IsCancelled { get { return cancellationToken.IsCancellationRequested; } }

        public CancellationToken Token { get { return cancellationToken; } }

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

    #else

    public class YieldCancell : IDisposable
    {
        //----- params -----

        //----- field -----

        private CancellationTokenSource cancellationToken = null;

        //----- property -----

        public bool IsCancelled { get { return cancellationToken.IsCancellationRequested; } }

        public CancellationToken Token { get { return cancellationToken.Token; } }

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

    #endif

}


using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.R3Extension
{
    public class AsyncHandler
    {
        //----- params -----

        //----- field -----

        private int count = 0;

        private UniTaskCompletionSource completionSource = null;

        //----- property -----

        public bool IsComplete { get { return count <= 0; } }

        //----- method -----

        public void Begin()
        {
            Interlocked.Increment(ref count);
        }

        public void End()
        {
            if (Interlocked.Decrement(ref count) <= 0)
            {
                completionSource?.TrySetResult();
            }
        }

        public async UniTask Wait()
        {
            if (IsComplete) { return; }

            completionSource = new UniTaskCompletionSource();

            await completionSource.Task;

            completionSource = null;
        }
    }
}

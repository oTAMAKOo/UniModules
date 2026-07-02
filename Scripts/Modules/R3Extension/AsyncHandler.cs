
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
                var source = completionSource;

                completionSource = null;

                if (source != null)
                {
                    source.TrySetResult();
                }
            }
        }

        public UniTask Wait()
        {
            if (IsComplete) { return UniTask.CompletedTask; }

            // UniTaskCompletionSource(非プール版)は複数awaiter対応のため、待機中のsourceは共有する.
            if (completionSource == null)
            {
                completionSource = new UniTaskCompletionSource();
            }

            return completionSource.Task;
        }
    }
}

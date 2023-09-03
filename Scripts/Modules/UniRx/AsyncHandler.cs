
using Cysharp.Threading.Tasks;

namespace Modules.UniRxExtension
{
    public class AsyncHandler
    {
        //----- params -----

        //----- field -----

        private int count = 0;

        //----- property -----

        public bool IsComplete { get { return count <= 0; } }

        //----- method -----

        public void Begin()
        {
            count++;
        }

        public void End()
        {
            count--;
        }

        public async UniTask Wait()
        {
            await UniTask.WaitUntil(() => IsComplete);
        }
    }
}
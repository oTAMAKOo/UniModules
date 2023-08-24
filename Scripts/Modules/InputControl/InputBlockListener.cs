
using UnityEngine;
using UniRx;

namespace Modules.InputControl
{
    public abstract class InputBlockListener : MonoBehaviour
    {
        //----- params -----
        
        //----- field -----
        
        //----- property -----

        protected abstract InputBlockType BlockType { get; }

        //----- method -----

        void Start()
        {
            var blockInputManager = BlockInputManager.Instance;

            if (blockInputManager.BlockType == BlockType)
            {
                blockInputManager.OnUpdateStatusAsObservable()
                    .Subscribe(x => UpdateInputBlock(x))
                    .AddTo(this);

                UpdateInputBlock(blockInputManager.IsBlocking);
            }
        }

        protected abstract void UpdateInputBlock(bool isBlock);
    }
}
﻿﻿
using UnityEngine;
using UniRx;

namespace Modules.InputControl
{
    public abstract class InputBlockListener : MonoBehaviour
    {
        //----- params -----
        
        //----- field -----
        
        //----- property -----

        //----- method -----

        void Start()
        {
            var blockInputManager = BlockInputManager.Instance;

            blockInputManager.OnUpdateStatusAsObservable()
                .Subscribe(x => UpdateInputBlock(x))
                .AddTo(this);

            if (blockInputManager.IsBlocking)
            {
                UpdateInputBlock(blockInputManager.IsBlocking);
            }
        }

        protected abstract void UpdateInputBlock(bool isBlock);
    }
}
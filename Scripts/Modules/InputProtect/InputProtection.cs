﻿﻿
using UnityEngine;
using UniRx;

namespace Modules.InputProtection
{
    public abstract class InputProtection : MonoBehaviour
    {
        //----- params -----
        
        //----- field -----
        
        //----- property -----

        //----- method -----

        void Start()
        {
            var inputProtectManager = InputProtectManager.Instance;

            inputProtectManager.OnUpdateProtectAsObservable().Subscribe(x => UpdateProtect(x)).AddTo(this);

            if (inputProtectManager.IsProtect)
            {
                UpdateProtect(inputProtectManager.IsProtect);
            }
        }

        protected abstract void UpdateProtect(bool isProtect);
    }
}
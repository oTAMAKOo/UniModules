﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

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
            InputProtect.OnUpdateProtectAsObservable().Subscribe(x => UpdateProtect(x)).AddTo(this);

            if (InputProtect.IsProtect)
            {
                UpdateProtect(InputProtect.IsProtect);
            }
        }

        protected abstract void UpdateProtect(bool isProtect);
    }
}
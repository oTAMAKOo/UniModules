﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(InputField))]
    public abstract class UIInputField : UIComponent<InputField>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public InputField InputField { get { return component; } }

        //----- method -----
    }
}

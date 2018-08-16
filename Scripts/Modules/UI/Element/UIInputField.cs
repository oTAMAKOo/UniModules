﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(InputField))]
    public abstract class UIInputField : UIElement<InputField>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public InputField InputField { get { return component; } }

        //----- method -----

        public override void Modify()
        {

        }
    }
}
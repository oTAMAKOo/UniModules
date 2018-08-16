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
    [RequireComponent(typeof(Toggle))]
    public abstract class UIToggle : UIElement<Toggle>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Toggle Toggle { get { return component; } }

        public bool isOn
        {
            get { return component.isOn; }
            set { component.isOn = value; }
        }

        //----- method -----

        public override void Modify()
        {

        }
    }
}
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
    [RequireComponent(typeof(Dropdown))]
    public abstract class UIDropdown : UIElement<Dropdown>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Dropdown Dropdown { get { return component; } }

        //----- method -----

        public override void Modify()
        {

        }
    }
}
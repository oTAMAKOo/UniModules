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
    [RequireComponent(typeof(Scrollbar))]
    public abstract class UIScrollbar : UIElement<Scrollbar>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Scrollbar Scrollbar { get { return component; } }

        //----- method -----

        public override void Modify()
        {
            
        }
    }
}
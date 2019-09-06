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
    [ExecuteInEditMode]
    [RequireComponent(typeof(Scrollbar))]
    public abstract class UIScrollbar : UIComponent<Scrollbar>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Scrollbar Scrollbar { get { return component; } }

        //----- method -----
    }
}

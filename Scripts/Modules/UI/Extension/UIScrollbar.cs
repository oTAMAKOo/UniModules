﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
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

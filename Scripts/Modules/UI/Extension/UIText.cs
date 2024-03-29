﻿
using UnityEngine;
using TMPro;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class UIText : UIComponent<TextMeshProUGUI>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public TextMeshProUGUI Text { get { return component; } }

        public string text
        {
            get { return component.text; }
            set { component.SetText(value); }
        }

        //----- method -----
    }
}

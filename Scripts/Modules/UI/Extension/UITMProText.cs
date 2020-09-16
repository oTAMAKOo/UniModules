
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using TMPro;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract partial class UITMProText : UIComponent<TextMeshProUGUI>
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

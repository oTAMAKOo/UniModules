﻿
using UnityEngine;
using TMPro;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_InputField))]
    public abstract class UIInputField : UIComponent<TMP_InputField>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public TMP_InputField InputField { get { return component; } }

        //----- method -----
    }
}

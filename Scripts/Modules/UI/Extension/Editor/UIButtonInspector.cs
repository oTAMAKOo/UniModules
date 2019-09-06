﻿﻿
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIButton), true)]
    public class UIButtonInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            DrawDefaultScriptlessInspector();
        }
    }
}

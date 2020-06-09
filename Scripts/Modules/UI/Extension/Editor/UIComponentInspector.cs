
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CustomEditor(typeof(UIComponentBehaviour), true)]
    public class UIComponentInspector : ScriptlessEditor
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

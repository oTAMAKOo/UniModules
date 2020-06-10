﻿

using UnityEngine;
using UnityEditor;

namespace Extensions.Devkit
{
    public sealed class DisableScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public DisableScope(bool disabled)
        {
            EditorGUI.BeginDisabledGroup(disabled);
        }

        protected override void CloseScope()
        {
            EditorGUI.EndDisabledGroup();
        }
    }
}

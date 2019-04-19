
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextKerning))]
    public class TextKerninInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private TextKerning instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as TextKerning;

            EditorGUI.BeginChangeCheck();

            var spacing = EditorGUILayout.FloatField("Spacing", instance.Spacing);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("TextKerninInspector Undo", instance);
                instance.Spacing = spacing;
            }

            EditorGUI.BeginChangeCheck();

            var ttt = EditorGUILayout.FloatField("ttt", instance.TTT);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("TextKerninInspector Undo", instance);
                instance.TTT = ttt;
            }

            EditorGUI.BeginChangeCheck();

            var yyy = EditorGUILayout.FloatField("yyy", instance.YYY);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("TextKerninInspector Undo", instance);
                instance.YYY = yyy;
            }
        }
    }
}

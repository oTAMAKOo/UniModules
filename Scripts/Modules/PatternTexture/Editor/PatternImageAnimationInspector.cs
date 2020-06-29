
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
    [CustomEditor(typeof(PatternImageAnimation), true)]
    public sealed class PatternImageAnimationInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as PatternImageAnimation;

            serializedObject.Update();

            var setNativeSizeProperty = serializedObject.FindProperty("setNativeSize");

            DrawDefaultScriptlessInspector();

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            var index = Reflection.GetPrivateField<PatternImageAnimation, int>(instance, "patternIndex");
            
            var labels = instance.GetPatternNames();

            index = EditorGUILayout.Popup("PatternName", index, labels.ToArray());

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("PatternImageAnimationInspector-Undo", instance);

                if (index == -1)
                {
                    index = 0;
                }

                Reflection.SetPrivateField(instance, "patternIndex", index);
            }

            EditorGUILayout.PropertyField(setNativeSizeProperty);
        }
    }
}

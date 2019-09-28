
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextColor
{
    [CustomEditor(typeof(TextColor))]
    public sealed class TextColorInspector : Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextColor;

            EditorGUI.BeginChangeCheck();

            var setting = EditorGUILayout.ObjectField("Setting File", instance.Setting, typeof(TextColorSetting), false) as TextColorSetting;

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("TextColorInspector Undo", instance);

                instance.Setting = setting;
                instance.TextColorName = null;
            }

            if (setting != null)
            {
                var infos = setting.ColorInfos.Where(x => !string.IsNullOrEmpty(x.name)).ToArray();

                var textColorName = instance.TextColorName;

                var index = infos.IndexOf(x => x.name == textColorName) + 1;

                var labels = new List<string>{ "None" };

                labels.AddRange(infos.Select(x => x.name));

                EditorGUI.BeginChangeCheck();

                var select = EditorGUILayout.Popup("ColorName", index, labels.ToArray());

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("TextColorInspector Undo", instance);

                    var newTextColorName = string.Empty;

                    if (select == 0)
                    {
                        newTextColorName = null;
                    }
                    else if(0 < select)
                    {
                        newTextColorName = infos[select - 1].name;
                    }

                    instance.TextColorName = newTextColorName;
                }
            }
        }
    }
}

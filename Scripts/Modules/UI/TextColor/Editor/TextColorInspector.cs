
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.UI.TextEffect;
using UnityEngine.UI;

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

                var info = infos.FirstOrDefault(x => x.name == textColorName);

                var index = infos.IndexOf(x => x == info) + 1;

                var labels = new List<string>{ "None" };

                labels.AddRange(infos.Select(x => x.name));

                using (new EditorGUILayout.HorizontalScope())
                {
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
                        else if (0 < select)
                        {
                            newTextColorName = infos[select - 1].name;
                        }

                        instance.TextColorName = newTextColorName;

                        instance.ApplyColor();
                    }

                    if (!string.IsNullOrEmpty(instance.TextColorName))
                    {
                        if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(75f), GUILayout.Height(15f)))
                        {
                            instance.ApplyColor();
                        }
                    }
                }

                if (info != null)
                {
                    var components = UnityUtility.GetComponents<Component>(instance.gameObject).ToArray();

                    if (info.hasOutline)
                    {
                        var hasOutline = HasComponent(components, new Type[] { typeof(Outline), typeof(TextOutline), typeof(RichTextOutline) });

                        if (!hasOutline)
                        {
                            EditorGUILayout.HelpBox("Require component Outline or TextOutline or RichTextOutline.", MessageType.Warning);
                        }
                    }

                    if (info.hasShadow)
                    {
                        var hasShadow = HasComponent(components, new Type[] { typeof(Shadow), typeof(TextShadow), typeof(RichTextShadow) });

                        if (!hasShadow)
                        {
                            EditorGUILayout.HelpBox("Require component Shadow or TextShadow or RichTextShadow.", MessageType.Warning);
                        }
                    }
                }
            }
        }

        private static bool HasComponent(Component[] components, Type[] types)
        {
            foreach (var component in components)
            {
                var type = component.GetType();

                if (types.Any(x => x == type)) { return true; }
            }

            return false;
        }
    }
}

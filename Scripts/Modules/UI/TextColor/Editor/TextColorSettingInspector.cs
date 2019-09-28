
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
    [CustomEditor(typeof(TextColorSetting))]
    public sealed class TextColorSettingInspector : Editor
    {
        //----- params -----

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextColorSetting;

            var colorInfos = instance.ColorInfos;

            var headerItems = new List<EditorLayoutTools.ColumnHeaderContent>();

            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Name", GUILayout.MinWidth(100f)));
            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Color", GUILayout.Width(65f)));
            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Outline", GUILayout.Width(85f)));
            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Shadow", GUILayout.Width(85f)));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                EditorLayoutTools.DrawColumnHeader(headerItems.ToArray());

                GUILayout.Space(35f);

                if (28 <= colorInfos.Length)
                {
                    GUILayout.Space(18f);
                }
            }

            var list = colorInfos.ToList();

            var updated = false;
            var deleteIndexs = new List<int>();

            using (new ContentsScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(500f)))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var result = DrawTextColorInfoGUI(list[i]);

                        if (result == 1)
                        {
                            updated = true;
                        }

                        if (result == -1)
                        {
                            deleteIndexs.Add(i);
                            updated = true;
                        }
                    }

                    foreach (var index in deleteIndexs)
                    {
                        list.RemoveAt(index);
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    var info = new TextColorInfo();

                    while (true)
                    {
                        info.guid = Guid.NewGuid().ToString();

                        if (list.All(x => x.guid != info.guid))
                        {
                            break;
                        }
                    }

                    list.Add(info);

                    updated = true;
                }
            }

            if (updated)
            {
                UnityEditorUtility.RegisterUndo("TextColorSettingInspector Undo", instance);
                Reflection.SetPrivateField(instance, "colorInfos", list.ToArray());
            }
        }

        private int DrawTextColorInfoGUI(TextColorInfo info)
        {
            var result = 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                var name = EditorGUILayout.TextField(info.name, GUILayout.MinWidth(100f));

                GUILayout.Space(2f);

                var textColor = EditorGUILayout.ColorField(info.textColor, GUILayout.Width(65f));
                
                var hasOutline = EditorGUILayout.Toggle(info.hasOutline, GUILayout.Width(14f));

                var outlineColor = EditorGUILayout.ColorField(info.outlineColor, GUILayout.Width(65f));

                var hasShadow = EditorGUILayout.Toggle(info.hasShadow, GUILayout.Width(14f));

                var shadowColor = EditorGUILayout.ColorField(info.shadowColor, GUILayout.Width(65f));

                if (EditorGUI.EndChangeCheck())
                {
                    info.name = name;
                    info.textColor = textColor;
                    info.hasShadow = hasShadow;
                    info.shadowColor = shadowColor;
                    info.hasOutline = hasOutline;
                    info.outlineColor = outlineColor;

                    result = 1;
                }

                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(25f)))
                {
                    result = -1;
                }
            }

            return result;
        }
    }
}

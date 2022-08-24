
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextColorTag
{
    [CustomEditor(typeof(TextColorTagSetting))]
    public sealed class TextColorTagSettingInspector : Editor
    {
        //----- params -----
               
        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextColorTagSetting;

            var colorTagInfos = instance.GeTextColorTagInfos();

            EditorGUILayout.Separator();

            var headerItems = new List<Tuple<string, GUILayoutOptions>>();

            headerItems.Add(Tuple.Create("Color", new GUILayoutOptions(GUILayout.Width(65f))));
            headerItems.Add(Tuple.Create("Tag", new GUILayoutOptions(GUILayout.MinWidth(150f))));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                EditorLayoutTools.ColumnHeader(headerItems.ToArray());

                GUILayout.Space(35f);

                if (16 < colorTagInfos.Length)
                {
                    GUILayout.Space(18f);
                }
            }

            var list = colorTagInfos.ToList();

            var updated = false;
            var deleteIndexs = new List<int>();

            using (new ContentsScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(300f)))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var result = DrawTextColorTagInfoGUI(list[i]);

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
                    var info = new TextColorTagInfo();

                    list.Add(info);

                    updated = true;
                }
            }

            if (updated)
            {
                UnityEditorUtility.RegisterUndo(instance);
                Reflection.SetPrivateField(instance, "colorTagInfos", list.ToArray());
            }
        }

        private int DrawTextColorTagInfoGUI(TextColorTagInfo info)
        {
            var result = 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                var color = EditorGUILayout.ColorField(info.color, GUILayout.Width(65f));

                var tag = EditorGUILayout.TextField(info.tag, GUILayout.ExpandWidth(true));

                if (EditorGUI.EndChangeCheck())
                {
                    info.color = color;
                    info.tag = tag;

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

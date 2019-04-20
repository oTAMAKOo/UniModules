
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using UnityEditorInternal;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(FontKerningSetting))]
    public class FontKerningSettingInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Vector3 scrollPosition = Vector3.zero;
        private TextSpacing[] applyTargets = null;

        private FontKerningSetting instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as FontKerningSetting;
            
            var updated = false;
            var deleteIndexs = new List<int>();

            var infos = Reflection.GetPrivateField<FontKerningSetting, FontKerningSetting.CharInfo[]>(instance, "infos");
            var list = infos != null ? infos.ToList() : new List<FontKerningSetting.CharInfo>();

            if (applyTargets == null)
            {
                applyTargets = UnityUtility.FindObjectsOfType<TextSpacing>();
            }

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            var font = EditorGUILayout.ObjectField("Font", instance.Font, typeof(Font), false);

            if (EditorGUI.EndChangeCheck())
            {
                list.Clear();
                updated = true;
            }

            EditorGUILayout.Separator();

            if (font != null)
            {
                if (list.IsEmpty())
                {
                    EditorGUILayout.HelpBox("Press the + button if add info", MessageType.Info);
                }
                else
                {
                    DrawCharInfoHeaderGUI(list.Count);

                    using (new ContentsScope())
                    {
                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(300f)))
                        {
                            for (var i = 0; i < list.Count; i++)
                            {
                                EditorGUI.BeginChangeCheck();

                                var delete = DrawCharInfoGUI(list[i]);

                                GUILayout.Space(2f);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    var exist = list.Where(x => x.character != default(char))
                                        .Where(x => x != list[i])
                                        .Any(x => x.character == list[i].character);

                                    if (exist)
                                    {
                                        var title = "Register failed";
                                        var message = string.Format("Char : {0} is already exists.", list[i].character);

                                        EditorUtility.DisplayDialog(title, message, "Close");

                                        list[i].character = default(char);
                                    }

                                    updated = true;
                                }

                                if (delete)
                                {
                                    deleteIndexs.Add(i);
                                }
                            }

                           scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }

                    foreach (var index in deleteIndexs)
                    {
                        list.RemoveAt(index);
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(60f)))
                    {
                        var info = new FontKerningSetting.CharInfo();

                        list.Add(info);

                        updated = true;
                    }
                }
            }
            
            if (updated)
            {
                UnityEditorUtility.RegisterUndo("FontKerningSettingInspector Undo", instance);

                Reflection.SetPrivateField(instance, "font", font);
                Reflection.SetPrivateField(instance, "infos", list.Where(x => x != null).ToArray());
                Reflection.SetPrivateField(instance, "dictionary", (Dictionary<char, FontKerningSetting.CharInfo>)null);

                ApplyTextSpacing();
            }
        }

        private void DrawCharInfoHeaderGUI(int itemCount)
        {
            var headerItems = new List<EditorLayoutTools.ColumnHeaderContent>();

            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Char", GUILayout.Width(55f)));
            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Space (Left)", GUILayout.MinWidth(50f)));
            headerItems.Add(new EditorLayoutTools.ColumnHeaderContent("Space (Right)", GUILayout.MinWidth(50f)));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(2f);

                EditorLayoutTools.DrawColumnHeader(headerItems.ToArray());

                GUILayout.Space(35f);

                if (15 <= itemCount)
                {
                    GUILayout.Space(18f);
                }
            }
        }

        private bool DrawCharInfoGUI(FontKerningSetting.CharInfo info)
        {
            var delete = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                var text = EditorGUILayout.DelayedTextField(info.character.ToString(), GUILayout.Width(50f));

                if(EditorGUI.EndChangeCheck())
                {
                    info.character = text.FirstOrDefault();
                }

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(20f);

                info.leftSpace = EditorGUILayout.FloatField("◀▶", info.leftSpace, GUILayout.ExpandWidth(true));

                info.rightSpace = EditorGUILayout.FloatField("◀▶", info.rightSpace, GUILayout.ExpandWidth(true));

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(25f)))
                {
                    delete = true;
                }
            }

            return delete;
        }

        private void ApplyTextSpacing()
        {
            foreach (var target in applyTargets)
            {
                if (target.Text.font == null) { continue; }

                if (target.KerningSetting != instance) { continue; }

                target.Text.SetVerticesDirty();
            }

            InternalEditorUtility.RepaintAllViews();
        }
    }
}


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
        private TextSpacing[] targets = null;

        private FontKerningSetting instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            targets = UnityUtility.FindObjectsOfType<TextSpacing>();
        }

        public override void OnInspectorGUI()
        {
            instance = target as FontKerningSetting;
            
            var updated = false;
            var deleteIndexs = new List<int>();

            var infos = Reflection.GetPrivateField<FontKerningSetting, FontKerningSetting.CharInfo[]>(instance, "infos");
            var list = infos != null ? infos.ToList() : new List<FontKerningSetting.CharInfo>();

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

                ApplyTextSpacing();
            }
        }

        private void DrawCharInfoHeaderGUI(int itemCount)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(2, 2, 2, 2);
            style.fixedHeight = 16;
            style.contentOffset = new Vector2(0f, -2f);
            style.alignment = TextAnchor.MiddleCenter;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("Char", style, GUILayout.Width(50f));
                EditorGUILayout.LabelField("Space (Left)", style, GUILayout.MinWidth(50f));
                EditorGUILayout.LabelField("Space (Right)", style, GUILayout.MinWidth(50f));
                GUILayout.Space(30f);

                if (15 <= itemCount)
                {
                    GUILayout.Space(18f);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private bool DrawCharInfoGUI(FontKerningSetting.CharInfo info)
        {
            var delete = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();

                var text = EditorGUILayout.DelayedTextField(info.character.ToString(), GUILayout.Width(50f));

                if(EditorGUI.EndChangeCheck())
                {
                    info.character = text.FirstOrDefault();
                }

                info.leftSpace = EditorGUILayout.FloatField(info.leftSpace, GUILayout.MinWidth(50f));

                info.rightSpace = EditorGUILayout.FloatField(info.rightSpace, GUILayout.MinWidth(50f));

                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(25f)))
                {
                    delete = true;
                }

                GUILayout.FlexibleSpace();
            }

            return delete;
        }

        private void ApplyTextSpacing()
        {
            foreach (var target in targets)
            {
                if (target.Text.font == null) { continue; }

                if (target.KerningSetting != instance) { continue; }

                target.Text.SetVerticesDirty();
            }

            InternalEditorUtility.RepaintAllViews();
        }
    }
}

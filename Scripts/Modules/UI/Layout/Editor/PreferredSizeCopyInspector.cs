﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Layout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof (PreferredSizeCopy))]
    public sealed class PreferredSizeCopyInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var instance = target as PreferredSizeCopy;

            var copySource = Reflection.GetPrivateField<PreferredSizeCopy, RectTransform>(instance, "copySource");

            EditorGUI.BeginChangeCheck();

            copySource = EditorGUILayout.ObjectField("Source", copySource, typeof(RectTransform), true) as RectTransform;

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("PreferredSizeCopyEditor-Undo", instance);
                Reflection.SetPrivateField(instance, "copySource", copySource);
            }

            if(copySource != null)
            {
                var horizontal = Reflection.GetPrivateField<PreferredSizeCopy, PreferredSizeCopy.LayoutInfo>(instance, "horizontal");

                if(EditorLayoutTools.Header("Horizontal", "PreferredSizeCopyEditor-Horizontal"))
                {
                    using (new ContentsScope())
                    {
                        var edit = false;

                        EditorGUI.BeginChangeCheck();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Enable", GUILayout.Width(60f));
                            horizontal.Enable = EditorGUILayout.Toggle(horizontal.Enable);
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            edit = true;
                        }

                        using (new DisableScope(!horizontal.Enable))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Padding", GUILayout.Width(60f));
                                horizontal.Padding = EditorGUILayout.FloatField(horizontal.Padding, GUILayout.Width(100f));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Min", GUILayout.Width(60f));

                                var value = horizontal.Min.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(horizontal.Min.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    horizontal.Min = enable ? (float?)value : null;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Max", GUILayout.Width(60f));

                                var value = horizontal.Max.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(horizontal.Max.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    horizontal.Max = enable ? (float?)value : null;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Flexible", GUILayout.Width(60f));

                                var value = horizontal.Flexible.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(horizontal.Flexible.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    horizontal.Flexible = enable ? (float?)value : null;
                                }
                            }
                        }

                        if (edit)
                        {
                            UnityEditorUtility.RegisterUndo("PreferredSizeCopyEditor-Undo", instance);
                            Reflection.SetPrivateField(instance, "horizontal", horizontal);
                            Reflection.InvokePrivateMethod(instance, "SetDirty");
                        }
                    }
                }

                GUILayout.Space(3f);

                var vertical = Reflection.GetPrivateField<PreferredSizeCopy, PreferredSizeCopy.LayoutInfo>(instance, "vertical");

                if (EditorLayoutTools.Header("Vertical", "PreferredSizeCopyEditor-Vertical"))
                {
                    using (new ContentsScope())
                    {
                        var edit = false;

                        EditorGUI.BeginChangeCheck();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Enable", GUILayout.Width(60f));
                            vertical.Enable = EditorGUILayout.Toggle(vertical.Enable);
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            edit = true;
                        }

                        using (new DisableScope(!vertical.Enable))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Padding", GUILayout.Width(60f));
                                vertical.Padding = EditorGUILayout.FloatField(vertical.Padding, GUILayout.Width(100f));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Min", GUILayout.Width(60f));

                                var value = vertical.Min.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(vertical.Min.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    vertical.Min = enable ? (float?)value : null;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Max", GUILayout.Width(60f));

                                var value = vertical.Max.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(vertical.Max.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    vertical.Max = enable ? (float?)value : null;
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.LabelField("Flexible", GUILayout.Width(60f));

                                var value = vertical.Flexible.GetValueOrDefault(0f);
                                var enable = EditorGUILayout.Toggle(vertical.Flexible.HasValue, GUILayout.Width(30f));

                                if (enable)
                                {
                                    value = EditorGUILayout.FloatField(value, GUILayout.Width(100f));
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    edit = true;
                                    vertical.Flexible = enable ? (float?)value : null;
                                }
                            }
                        }

                        if (edit)
                        {
                            UnityEditorUtility.RegisterUndo("PreferredSizeCopyEditor-Undo", instance);
                            Reflection.SetPrivateField(instance, "vertical", vertical);
                            Reflection.InvokePrivateMethod(instance, "SetDirty");

                            var rt = UnityUtility.GetComponent<RectTransform>(instance.gameObject);
                            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                        }
                    }
                }
            }

            EditorGUILayout.Separator();
        }
    }
}

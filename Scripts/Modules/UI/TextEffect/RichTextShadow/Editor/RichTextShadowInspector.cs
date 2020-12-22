
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(RichTextShadow))]
    public sealed class RichTextShadowInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private RichTextShadow instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as RichTextShadow;

            serializedObject.Update();

            DrawInspector();
        }

        private void DrawInspector()
        {
            GUILayout.Space(2f);

            var placement = Reflection.GetPrivateField<RichTextShadow, RichTextShadow.Placement>(instance, "placement");

            EditorGUI.BeginChangeCheck();

            placement = (RichTextShadow.Placement)EditorGUILayout.EnumPopup("Placement", placement);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("placement", placement);
                SetValue("direction", (RichTextShadow.Direction)0);
                SetValue("infos", new RichTextShadow.Info[0]);
            }

            switch (placement)
            {
                case RichTextShadow.Placement.Simple:
                    {
                        var direction = Reflection.GetPrivateField<RichTextShadow, RichTextShadow.Direction>(instance, "direction");

                        EditorGUI.BeginChangeCheck();

                        direction = (RichTextShadow.Direction)EditorGUILayout.EnumFlagsField("Direction", direction);

                        if (EditorGUI.EndChangeCheck())
                        {
                            SetValue("direction", direction);
                            SetValue("infos", new RichTextShadow.Info[0]);
                        }

                        if (direction != 0)
                        {
                            if (EditorLayoutTools.Header("Distance", "RichTextShadowInspector-Distance"))
                            {
                                var infos = Reflection.GetPrivateField<RichTextShadow, RichTextShadow.Info[]>(instance, "infos");

                                using (new ContentsScope())
                                {
                                    var list = infos.ToList();

                                    EditorGUI.BeginChangeCheck();

                                    if ((direction & RichTextShadow.Direction.Top) != 0)
                                    {
                                        var label = RichTextShadow.Direction.Top.ToString();
                                        var index = GetIndex(RichTextShadow.Direction.Top, ref list);

                                        list[index].distance.y = EditorGUILayout.FloatField(label, list[index].distance.y);
                                    }

                                    if ((direction & RichTextShadow.Direction.Right) != 0)
                                    {
                                        var label = RichTextShadow.Direction.Right.ToString();
                                        var index = GetIndex(RichTextShadow.Direction.Right, ref list);

                                        list[index].distance.x = EditorGUILayout.FloatField(label, list[index].distance.x);
                                    }

                                    if ((direction & RichTextShadow.Direction.Bottom) != 0)
                                    {
                                        var label = RichTextShadow.Direction.Bottom.ToString();
                                        var index = GetIndex(RichTextShadow.Direction.Bottom, ref list);

                                        list[index].distance.y = EditorGUILayout.FloatField(label, list[index].distance.y * -1) * -1;
                                    }

                                    if ((direction & RichTextShadow.Direction.Left) != 0)
                                    {
                                        var label = RichTextShadow.Direction.Left.ToString();
                                        var index = GetIndex(RichTextShadow.Direction.Left, ref list);

                                        list[index].distance.x = EditorGUILayout.FloatField(label, list[index].distance.x * -1) * -1;
                                    }

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetValue("infos", list.ToArray());
                                    }
                                }
                            }
                        }
                    }
                    break;

                case RichTextShadow.Placement.Custom:
                    {
                        if (EditorLayoutTools.Header("Distance", "RichShadowInspector-Distance"))
                        {
                            var infos = Reflection.GetPrivateField<RichTextShadow, RichTextShadow.Info[]>(instance, "infos");
                            var list = infos.ToList();

                            using (new ContentsScope())
                            {
                                EditorGUI.BeginChangeCheck();

                                var size = EditorGUILayout.DelayedIntField("Size", list.Count);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if(list.Count < size)
                                    {
                                        while (list.Count < size)
                                        {
                                            list.Add(new RichTextShadow.Info());
                                        }
                                    }
                                    else
                                    {
                                        while (size < list.Count)
                                        {
                                            list.RemoveAt(list.Count - 1);
                                        }
                                    }

                                    SetValue("infos", list.ToArray());
                                }

                                GUILayout.Space(2f);

                                EditorGUI.BeginChangeCheck();
                                
                                for (var i = 0; i < list.Count; i++)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        GUILayout.FlexibleSpace();

                                        list[i].distance = EditorGUILayout.Vector2Field(string.Empty, list[i].distance);

                                        if (GUILayout.Button(string.Empty, new GUIStyle("OL Minus")))
                                        {
                                            list.RemoveAt(i);
                                        }
                                    }
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    SetValue("infos", list.ToArray());
                                }
                            }
                        }
                    }
                    break;
            }
            
            var color = Reflection.GetPrivateField<RichTextShadow, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Effect Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("color", color);
            }

            var useGraphicAlpha = Reflection.GetPrivateField<RichTextShadow, bool>(instance, "useGraphicAlpha");

            EditorGUI.BeginChangeCheck();

            useGraphicAlpha = EditorGUILayout.Toggle("Use Graphic Alpha", useGraphicAlpha);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("useGraphicAlpha", useGraphicAlpha);
            }

            GUILayout.Space(2f);
        }

        private int GetIndex(RichTextShadow.Direction flag, ref List<RichTextShadow.Info> infos)
        {
            var index = infos.IndexOf(x => x.direction.HasValue && x.direction.Value == (int)flag);

            if (index == -1)
            {
                var info = new RichTextShadow.Info()
                {
                    direction = (int)flag,
                    distance = Vector2.zero,
                };

                infos.Add(info);
                index = infos.Count - 1;
            }

            return index;
        }

        private void SetValue<TValue>(string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("RichTextShadowInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);
            Reflection.InvokePrivateMethod(instance, "OnValidate");
        }
    }
}

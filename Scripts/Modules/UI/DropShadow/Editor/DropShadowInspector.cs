﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI
{
    [CustomEditor(typeof(DropShadow))]
    public class DropShadowInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private DropShadow instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as DropShadow;

            serializedObject.Update();

            DrawInspector();
        }

        private void DrawInspector()
        {
            GUILayout.Space(2f);

            var placement = Reflection.GetPrivateField<DropShadow, DropShadow.Placement>(instance, "placement");

            EditorGUI.BeginChangeCheck();

            placement = (DropShadow.Placement)EditorGUILayout.EnumPopup("Placement", placement);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("placement", placement);
                SetValue("direction", (DropShadow.Direction)0);
                SetValue("infos", new DropShadow.Info[0]);
            }

            switch (placement)
            {
                case DropShadow.Placement.Simple:
                    {
                        var direction = Reflection.GetPrivateField<DropShadow, DropShadow.Direction>(instance, "direction");

                        EditorGUI.BeginChangeCheck();

                        direction = (DropShadow.Direction)EditorGUILayout.EnumFlagsField("Direction", direction);

                        if (EditorGUI.EndChangeCheck())
                        {
                            SetValue("direction", direction);
                            SetValue("infos", new DropShadow.Info[0]);
                        }

                        if (direction != 0)
                        {
                            if (EditorLayoutTools.DrawHeader("Distance", "DropShadowInspector-Distance"))
                            {
                                var infos = Reflection.GetPrivateField<DropShadow, DropShadow.Info[]>(instance, "infos");

                                using (new ContentsScope())
                                {
                                    var list = infos.ToList();

                                    EditorGUI.BeginChangeCheck();

                                    if ((direction & DropShadow.Direction.Top) != 0)
                                    {
                                        var label = DropShadow.Direction.Top.ToString();
                                        var index = GetIndex(DropShadow.Direction.Top, ref list);

                                        list[index].distance.y = EditorGUILayout.FloatField(label, list[index].distance.y);
                                    }

                                    if ((direction & DropShadow.Direction.Right) != 0)
                                    {
                                        var label = DropShadow.Direction.Right.ToString();
                                        var index = GetIndex(DropShadow.Direction.Right, ref list);

                                        list[index].distance.x = EditorGUILayout.FloatField(label, list[index].distance.x);
                                    }

                                    if ((direction & DropShadow.Direction.Bottom) != 0)
                                    {
                                        var label = DropShadow.Direction.Bottom.ToString();
                                        var index = GetIndex(DropShadow.Direction.Bottom, ref list);

                                        list[index].distance.y = EditorGUILayout.FloatField(label, list[index].distance.y * -1) * -1;
                                    }

                                    if ((direction & DropShadow.Direction.Left) != 0)
                                    {
                                        var label = DropShadow.Direction.Left.ToString();
                                        var index = GetIndex(DropShadow.Direction.Left, ref list);

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

                case DropShadow.Placement.Custom:
                    {
                        if (EditorLayoutTools.DrawHeader("Distance", "DropShadowInspector-Distance"))
                        {
                            var infos = Reflection.GetPrivateField<DropShadow, DropShadow.Info[]>(instance, "infos");
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
                                            list.Add(new DropShadow.Info());
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
            
            var color = Reflection.GetPrivateField<DropShadow, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Effect Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("color", color);
            }

            var useGraphicAlpha = Reflection.GetPrivateField<DropShadow, bool>(instance, "useGraphicAlpha");

            EditorGUI.BeginChangeCheck();

            useGraphicAlpha = EditorGUILayout.Toggle("Use Graphic Alpha", useGraphicAlpha);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("useGraphicAlpha", useGraphicAlpha);
            }

            GUILayout.Space(2f);
        }

        private int GetIndex(DropShadow.Direction flag, ref List<DropShadow.Info> infos)
        {
            var index = infos.IndexOf(x => x.direction.HasValue && x.direction.Value == (int)flag);

            if (index == -1)
            {
                var info = new DropShadow.Info()
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
            UnityEditorUtility.RegisterUndo("DropShadowInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);
            Reflection.InvokePrivateMethod(instance, "OnValidate");
        }
    }
}
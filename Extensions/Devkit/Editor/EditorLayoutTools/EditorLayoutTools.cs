
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Modules.Devkit.Prefs;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        public sealed class TitleGUIStyle
        {
            public Color? backgroundColor = null;
            public Color? labelColor = null;
            public TextAnchor alignment = TextAnchor.MiddleLeft;
            public FontStyle fontStyle = FontStyle.Bold;
            public float? width = null;
        }

        //----- field -----

        private static GUIStyle dragtabStyle = null;
        private static GUIStyle columnHeaderStyle = null;
        private static GUIStyle foldoutStyle = null;

        //----- property -----

        public static float SingleLineHeight
        {
            get
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        //----- method -----

        public static void Title(string text, params GUILayoutOption[] options)
        {
            Title(text, new TitleGUIStyle(), options);
        }

        public static void Title(string text, Color backgroundColor, params GUILayoutOption[] options)
        {
            var titleStyle = new TitleGUIStyle()
            {
                backgroundColor = backgroundColor,
            };

            Title(text, titleStyle, options);
        }

        public static void Title(string text, Color backgroundColor, Color labelColor, params GUILayoutOption[] options)
        {
            var titleStyle = new TitleGUIStyle()
            {
                backgroundColor = backgroundColor,
                labelColor = labelColor,
            };

            Title(text, titleStyle, options);
        }

        public static void Title(string text, TitleGUIStyle style, params GUILayoutOption[] options)
        {
            var labelStyleState = new GUIStyleState()
            {
                textColor = style.labelColor.HasValue ? style.labelColor.Value : LabelColor,
            };

            var labelStyle = new GUIStyle("IN TextField")
            {
                alignment = style.alignment,
                fontStyle = style.fontStyle,
                normal = labelStyleState,
            };

            var size = labelStyle.CalcSize(new GUIContent(text));

            var backgroundColor = style.backgroundColor.HasValue ? style.backgroundColor.Value : BackgroundColor;

            using (new BackgroundColorScope(backgroundColor))
            {
                var layoutOptions = new List<GUILayoutOption>()
                {
                    GUILayout.Height(size.y),
                };

                if (style.width.HasValue)
                {
                    layoutOptions.Add(GUILayout.Width(style.width.Value));
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea, layoutOptions.ToArray()))
                {
                    GUILayout.Space(10f);

                    GUILayout.Label(text, labelStyle, options);

                    GUILayout.Space(10f);
                }
            }
        }

        public static bool ColorButton(string text, bool enabled, Color color, params GUILayoutOption[] options)
        {
            var result = false;

            using (new DisableScope(!enabled))
            {
                using (new ColorScope(color))
                {
                    result = GUILayout.Button(text, options);
                }
            }

            return result;
        }

        public static bool PrefixButton(string text)
        {
            return PrefixButton(text, GUILayout.Width(76f));
        }

        public static bool PrefixButton(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, "DropDown", options);
        }

        public static bool Header(string text, Color? color = null, bool defaultState = true)
        {
            return Header(text, text, color, defaultState);
        }

        public static bool Header(string text, string key, Color? color = null, bool defaultState = true)
        {
            var state = ProjectPrefs.GetBool(key, defaultState);

            EditorGUI.BeginChangeCheck();

            state = Header(text, state, color);

            if (EditorGUI.EndChangeCheck())
            {
                ProjectPrefs.SetBool(key, state);
            }

            return state;
        }

        public static bool Header(string text, bool state, Color? color = null)
        {
            var c = color ?? DefaultHeaderColor;
            
            var backgroundColor = state ? c : new Color(c.r - 0.2f, c.g - 0.2f, c.b - 0.2f);

            text = "<b><size=11>" + text + "</size></b>";

            if (state)
            {
                text = "\u25BC " + text;
            }
            else
            {
                text = "\u25BA " + text;
            }

            if (dragtabStyle == null)
            {
                dragtabStyle = new GUIStyle("dragtab first");
            }

            using (new BackgroundColorScope(backgroundColor))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!GUILayout.Toggle(true, text, dragtabStyle, GUILayout.MinWidth(20f)))
                    {
                        if (Event.current.button == 0)
                        {
                            state = !state;
                        }
                    }
                }
            }
            
            if (state)
            {
                GUILayout.Space(-2f);
            }
            else
            {
                GUILayout.Space(3f);
            }

            return state;
        }

        public static void ColumnHeader(Tuple<string, GUILayoutOptions>[] contents)
        {
            if (columnHeaderStyle == null)
            {
                columnHeaderStyle = new GUIStyle("ShurikenModuleTitle")
                {
                    font = new GUIStyle(EditorStyles.label).font,
                    border = new RectOffset(2, 2, 2, 2),
                    fixedHeight = 17,
                    contentOffset = new Vector2(0f, -2f),
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var content in contents)
                {
                    var text = "<b><size=11>" + content.Item1 + "</size></b>";
                    var options = content.Item2.layoutOptions;

                    EditorGUILayout.LabelField(text, columnHeaderStyle, options);
                }         
            }
        }

        public static void ContentTitle(string text, Color? color = null)
        {
            GUILayout.Space(2f);

            if (!color.HasValue)
            {
                color = DefaultHeaderColor;
            }

            using (new BackgroundColorScope(color.Value))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    text = "<b><size=11>" + text + "</size></b>";

                    var style = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                    };

                    GUILayout.Label(text, style, GUILayout.MinWidth(20f));

                    GUILayout.Space(2f);
                }
            }

            GUILayout.Space(-2f);
        }

        public static bool Foldout(string text, bool display)
        {
            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle("ShurikenModuleTitle")
                {
                    font = new GUIStyle(EditorStyles.label).font,
                    border = new RectOffset(2, 2, 2, 2),
                    fixedHeight = 20f,
                    contentOffset = new Vector2(20f, -2f),
                };

                foldoutStyle.normal.textColor = LabelColor;
            }

            var rect = GUILayoutUtility.GetRect(16f, 17f, foldoutStyle);

            var c = DefaultHeaderColor;
            
            var backgroundColor = display ? c : new Color(c.r - 0.2f, c.g - 0.2f, c.b - 0.2f);

            using (new BackgroundColorScope(backgroundColor))
            {
                text = "<b><size=11>" + text + "</size></b>";

                GUI.Box(rect, text, foldoutStyle);
            }

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            
            if (e.type == EventType.Repaint) 
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) 
            {
                display = !display;
                e.Use();
            }

            return display;
        }

        public static void Outline(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var tex = blankTexture;

                using (new ColorScope(color))
                {
                    GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), tex);
                    GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, 1f, rect.height), tex);
                    GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), tex);
                    GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, 1f), tex);
                }
            }
        }      
    }
}

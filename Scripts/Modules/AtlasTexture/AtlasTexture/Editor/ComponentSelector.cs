﻿
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Atlas
{
    public class ComponentSelector : ScriptableWizard
    {
        public delegate void OnSelectionCallback(Object obj);

        private System.Type type;
        private OnSelectionCallback callback = null;
        private Object[] objects = null;
        private bool searched = false;
        private Vector2 scroll = Vector2.zero;
        private string[] extensions = null;

        public static void Draw<T>(string buttonName, T obj, OnSelectionCallback cb, params GUILayoutOption[] options) where T : Object
        {
            GUILayout.BeginHorizontal();
            {
                bool show = EditorLayoutTools.DrawPrefixButton(buttonName);

                T o = EditorGUILayout.ObjectField(obj, typeof(T), false, options) as T;

                if (o != null && GUILayout.Button("X", GUILayout.Width(20f)))
                {
                    o = null;
                }

                if (show)
                {
                    Show<T>(cb);
                }
                else
                {
                    cb(o);
                }
            }
            GUILayout.EndHorizontal();
        }

        public static void Draw<T>(T obj, OnSelectionCallback cb, params GUILayoutOption[] options) where T : Object
        {
            Draw<T>(GetTypeName(typeof(T)), obj, cb, options);
        }

        public static void Show<T>(OnSelectionCallback cb) where T : Object { Show<T>(cb, new string[] { ".prefab" }); }

        public static void Show<T>(OnSelectionCallback cb, string[] extensions) where T : Object
        {
            System.Type type = typeof(T);
            string title = string.Format("Select Asset ({0})", type.ToString());

            ComponentSelector comp = ScriptableWizard.DisplayWizard<ComponentSelector>(title);

            comp.type = type;
            comp.callback = cb;
            comp.extensions = extensions;
            comp.objects = Resources.FindObjectsOfTypeAll(typeof(T));

            if (comp.objects == null || comp.objects.Length == 0)
            {
                comp.Search();
            }
            else
            {
                if (typeof(T) == typeof(Font))
                {
                    for (int i = 0; i < comp.objects.Length; ++i)
                    {
                        Object obj = comp.objects[i];
                        if (obj.name == "Arial") continue;
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path)) comp.objects[i] = null;
                    }
                }

                System.Array.Sort(comp.objects,
                    delegate (Object a, Object b)
                    {
                        if (a == null) return (b == null) ? 0 : 1;
                        if (b == null) return -1;
                        return a.name.CompareTo(b.name);
                    });
            }
        }

        private void Search()
        {
            searched = true;

            if (extensions != null)
            {
                string[] paths = AssetDatabase.GetAllAssetPaths();
                bool isComponent = type.IsSubclassOf(typeof(Component));
                List<Object> list = new List<Object>();

                for (int i = 0; i < objects.Length; ++i)
                    if (objects[i] != null)
                        list.Add(objects[i]);

                for (int i = 0; i < paths.Length; ++i)
                {
                    string path = paths[i];

                    bool valid = false;

                    for (int b = 0; b < extensions.Length; ++b)
                    {
                        if (path.EndsWith(extensions[b], System.StringComparison.OrdinalIgnoreCase))
                        {
                            valid = true;
                            break;
                        }
                    }

                    if (!valid) continue;

                    EditorUtility.DisplayProgressBar("Loading", "Searching assets, please wait...", (float)i / paths.Length);
                    Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                    if (obj == null || list.Contains(obj)) continue;

                    if (!isComponent)
                    {
                        System.Type t = obj.GetType();
                        if (t == type || t.IsSubclassOf(type) && !list.Contains(obj))
                            list.Add(obj);
                    }
                    else if (PrefabUtility.GetPrefabInstanceStatus(obj) != PrefabInstanceStatus.NotAPrefab)
                    {
                        Object t = (obj as GameObject).GetComponent(type);
                        if (t != null && !list.Contains(t)) list.Add(t);
                    }
                }
                list.Sort(delegate (Object a, Object b) { return a.name.CompareTo(b.name); });
                objects = list.ToArray();
            }
            EditorUtility.ClearProgressBar();
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            GUILayout.Space(6f);

            if (objects == null || objects.Length == 0)
            {
                EditorGUILayout.HelpBox("No " + GetTypeName(type) + " components found.\nTry creating a new one.", MessageType.Info);
            }
            else
            {
                Object sel = null;
                scroll = GUILayout.BeginScrollView(scroll);

                foreach (Object o in objects)
                    if (DrawObject(o))
                        sel = o;

                GUILayout.EndScrollView();

                if (sel != null)
                {
                    callback(sel);
                    Close();
                }
            }

            if (!searched)
            {
                GUILayout.Space(6f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool search = GUILayout.Button("Show All", "LargeButton", GUILayout.Width(120f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (search) Search();
            }
        }

        private bool DrawObject(Object obj)
        {
            if (obj == null) return false;
            bool retVal = false;
            Component comp = obj as Component;

            GUILayout.BeginHorizontal();
            {
                string path = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(path))
                {
                    path = "[Embedded]";
                    GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
                }
                else if (comp != null && EditorUtility.IsPersistent(comp.gameObject))
                {
                    var dir = Path.GetDirectoryName(path);

                    if (dir.Contains("Resources"))
                    {
                        GUI.contentColor = Color.white;
                    }
                    else
                    {
                        GUI.contentColor = new Color(0.22f, 0.96f, 0.28f);
                    }
                }

                GUILayout.Space(5f);
                GUILayout.Label(obj.name, EditorLayoutTools.TextAreaStyle, GUILayout.Width(160f), GUILayout.Height(20f));
                GUILayout.Label(path.Replace("Assets/", ""), EditorLayoutTools.TextAreaStyle, GUILayout.Height(20f));

                GUI.contentColor = Color.white;

                retVal |= GUILayout.Button("Select", "ButtonLeft", GUILayout.Width(60f), GUILayout.Height(16f));
            }
            GUILayout.EndHorizontal();

            return retVal;
        }

        private static string GetTypeName(System.Type t)
        {
            if (t != null)
            {
                string s = t.ToString();
                s = s.Replace("UnityEngine.", "");

                return s;
            }

            return string.Empty;
        }
    }
}

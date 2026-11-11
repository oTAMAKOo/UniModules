
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetDependencies
{
    public sealed class AssetDependenciesWindow : SingletonEditorWindow<AssetDependenciesWindow>
    {
        //----- params -----

        private sealed class DependantInfo
        {
            /// <summary> アセットパス </summary>
            public string AssetPath { get; private set; }

            /// <summary> 依存関係のアセット情報 </summary>
            public string[] Dependencies { get; private set; }

            public DependantInfo(string assetPath)
            {
                AssetPath = assetPath;
                Dependencies = AssetDatabase.GetDependencies(assetPath);
            }
        }

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        //----- field -----

        private DependantInfo current = null;
        private string[] dependencies = null;
        private GUIStyle textAreaStyle = null;
        private string searchText = null;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private Vector2 scrollPosition = Vector2.zero;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

		public static void Open(Object target)
		{
			Instance.Initialize();

			Instance.Set(target);
		}

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("AssetDependencies");

            Show();

            initialized = true;
        }

        private void Set(Object target)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);

            current = new DependantInfo(assetPath);
            dependencies = GetDependencies(current);

            Repaint();
        }

        void OnGUI()
        {
            var e = Event.current;

            if (textAreaStyle == null)
            {
                textAreaStyle = GUI.skin.GetStyle("TextArea");
                textAreaStyle.alignment = TextAnchor.MiddleLeft;
                textAreaStyle.wordWrap = false;
                textAreaStyle.stretchWidth = true;
            }

            GUILayout.Space(3f);

            if (current != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                    GUILayout.Space(5f);

                    EditorGUI.BeginChangeCheck();

                    searchText = EditorGUILayout.TextField(string.Empty, searchText, "SearchTextField", GUILayout.Width(200f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        dependencies = GetDependencies(current);
                        scrollPosition = Vector2.zero;
                    }

                    if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                    {
                        searchText = string.Empty;
                        GUIUtility.keyboardControl = 0;
                        dependencies = GetDependencies(current);
                        scrollPosition = Vector2.zero;
                    }
                }

                GUILayout.Space(3f);

                var targetAsset = AssetDatabase.LoadMainAssetAtPath(current.AssetPath);

                EditorGUILayout.ObjectField(targetAsset, typeof(Object), false);

                GUILayout.Space(3f);

                if (dependencies.Any())
                {
                    EditorLayoutTools.Title("Dependencies");

                    GUILayout.Space(-2f);

                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        using (new ContentsScope())
                        {
                            foreach (var assetPath in dependencies)
                            {
                                switch (assetViewMode)
                                {
                                    case AssetViewMode.Asset:
                                        {
                                            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                                            EditorGUILayout.ObjectField(asset, typeof(Object), false);
                                        }
                                        break;

                                    case AssetViewMode.Path:
                                        {
                                            using (new EditorGUILayout.HorizontalScope())
                                            {
                                                EditorGUILayout.SelectableLabel(assetPath, textAreaStyle, GUILayout.Height(16f));

                                                if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f), GUILayout.Height(16f)))
                                                {
                                                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                                                    Selection.activeObject = asset;
                                                }
                                            }
                                        }
                                        break;
                                }

                                GUILayout.Space(1f);
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This object has no dependencies.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag and drop asset.", MessageType.Info);
            }

            // ドロップエリア.
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var validate = ValidateDragAndDrop(DragAndDrop.objectReferences);

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            Set(DragAndDrop.objectReferences.FirstOrDefault());
                        }
                    }

                    break;
            }
        }

        private string[] GetDependencies(DependantInfo assetDependantInfo)
        {
            var dependencies = assetDependantInfo.Dependencies
                .Where(x => x != current.AssetPath)
                .OrderBy(x => x)
                .ToArray();

            if (string.IsNullOrEmpty(searchText)) { return dependencies; }

            var list = new List<string>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var item in dependencies)
            {
                var isMatch = item.IsMatch(keywords);

                if (isMatch)
                {
                    list.Add(item);
                }
            }
           
            return list.ToArray();
        }

        private bool ValidateDragAndDrop(Object[] items)
        {
            var result = true;

            foreach (var item in items)
            {
                result &= AssetDatabase.IsMainAsset(item);
            }

            return result;
        }
    }
}

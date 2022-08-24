
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource
{
    public sealed class InvalidDependantWindow : SingletonEditorWindow<InvalidDependantWindow>
    {
        //----- params -----

        private static readonly string[] IgnoreExtensions = { ".cs" };

        private sealed class AssetInfo
        {
            public string AssetPath { get; private set; }
            public string[] InvalidDependants { get; private set; }

            public AssetInfo(string assetPath, string[] invalidDependants)
            {
                AssetPath = assetPath;
                InvalidDependants = invalidDependants;
            }
        }

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        private sealed class DependantInfoScrollView : EditorGUIFastScrollView<AssetInfo>
        {
            private HashSet<int> openedIds = null;
            private GUIStyle textAreaStyle = null;

            public AssetViewMode AssetViewMode { get; set; } 

            public override Direction Type
            {
                get { return Direction.Vertical; }
            }

            public DependantInfoScrollView()
            {
                openedIds = new HashSet<int>();
            }

            protected override void DrawContent(int index, AssetInfo content)
            {
                if (textAreaStyle == null)
                {
                    textAreaStyle = GUI.skin.GetStyle("TextArea");
                    textAreaStyle.alignment = TextAnchor.MiddleLeft;
                    textAreaStyle.wordWrap = false;
                    textAreaStyle.stretchWidth = true;
                }

                var opened = openedIds.Contains(index);

                using (new EditorGUILayout.VerticalScope())
                {
                    var open = EditorLayoutTools.Header(content.AssetPath, opened);

                    if (open)
                    {
                        using (new ContentsScope())
                        {
                            var targetAsset = AssetDatabase.LoadMainAssetAtPath(content.AssetPath);

                            EditorGUILayout.ObjectField(targetAsset, typeof(Object), false);

                            EditorLayoutTools.Title("InvalidDependants", Color.red, Color.white);

                            using (new ContentsScope())
                            {
                                foreach (var item in content.InvalidDependants)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        switch (AssetViewMode)
                                        {
                                            case AssetViewMode.Asset:
                                                {
                                                    var asset = AssetDatabase.LoadMainAssetAtPath(item);
                                                    EditorGUILayout.ObjectField(asset, typeof(Object), false);
                                                }
                                                break;

                                            case AssetViewMode.Path:
                                                {
                                                    EditorGUILayout.SelectableLabel(item, textAreaStyle, GUILayout.Height(18f));
                                                }
                                                break;
                                        }
                                    }                                  
                                }
                            }
                        }
                    }

                    if (!opened && open)
                    {
                        openedIds.Add(index);
                    }

                    if (opened && !open)
                    {
                        openedIds.Remove(index);
                    }
                }
            }
        }

        //----- field -----

        private AssetInfo[] assetInfos = null;
        private string searchText = null;
        private DependantInfoScrollView scrollView = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            var projectFolders = ProjectFolders.Instance;

            var externalResourcesPath = projectFolders.ExternalResourcesPath;

            Instance.Initialize(externalResourcesPath);
        }

        private void Initialize(string externalResourcesPath)
        {
            BuildAssetInfo(externalResourcesPath);

            scrollView = new DependantInfoScrollView();
            scrollView.Contents = GetAssetInfos();

            titleContent = new GUIContent("ExternalResources InvalidDependant");

            ShowUtility();
        }

        private void BuildAssetInfo(string externalResourcesPath)
        {
            var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var allAssetInfos = assetInfoManifest.GetAssetInfos().ToArray();

            var list = new List<AssetInfo>();

            foreach (var assetInfo in allAssetInfos)
            {
                var assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);

                var dependencies = AssetDatabase.GetDependencies(assetPath);

                dependencies = dependencies.Where(x => x != assetPath).ToArray();

                var invalidDependants = dependencies
                    .Where(x => !x.StartsWith(externalResourcesPath))
                    .Where(x => !IgnoreExtensions.Contains(Path.GetExtension(x)))
                    .ToArray();

                if (invalidDependants.IsEmpty()) { continue; }

                list.Add(new AssetInfo(assetPath, invalidDependants));
            }

            assetInfos = list.ToArray();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                scrollView.AssetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(scrollView.AssetViewMode, GUILayout.Width(60f));

                GUILayout.Space(5f);

                EditorGUI.BeginChangeCheck();
                
                searchText = EditorGUILayout.TextField(string.Empty, searchText, "SearchTextField", GUILayout.Width(200f));

                if (EditorGUI.EndChangeCheck())
                {
                    scrollView.Contents = GetAssetInfos();
                    scrollView.ScrollPosition = Vector2.zero;
                }

                if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    scrollView.Contents = GetAssetInfos();
                    scrollView.ScrollPosition = Vector2.zero;
                }
            }

            scrollView.Draw();
        }

        private AssetInfo[] GetAssetInfos()
        {
            if (string.IsNullOrEmpty(searchText)) { return assetInfos; }

            var list = new List<AssetInfo>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var item in assetInfos)
            {
                var isMatch = false;

                isMatch |= item.AssetPath.IsMatch(keywords);
                isMatch |= item.InvalidDependants.Any(x => x.IsMatch(keywords));

                if (isMatch)
                {
                    list.Add(item);
                }
            }

            return list.ToArray();
        }
    }
}

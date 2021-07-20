
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundles
{
    public sealed class FindDependencyAssetsWindow : SingletonEditorWindow<FindDependencyAssetsWindow>
    {
        //----- params -----

        private readonly Vector2 MinWindowSize = new Vector2(500f, 650f);

        private enum ViewType
        {
            Dependencies,
            Reference,
        }

        public sealed class AssetBundleInfo
        {
            public string AssetBundleName { get; private set; }
            public string[] Assets { get; private set; }
            public string[] DependentAssets { get; private set; }

            public bool IsOpen { get; set; }

            public AssetBundleInfo(string assetBundleName, string[] assets, string[] dependentAssets)
            {
                this.AssetBundleName = assetBundleName;
                this.Assets = assets;
                this.DependentAssets = dependentAssets;

                IsOpen = false;
            }
        }

        public sealed class AssetReferenceInfo
        {
            public string Asset { get; private set; }
            public string[] ReferenceAssets { get; private set; }

            public AssetReferenceInfo(string asset, string[] referenceAssets)
            {
                this.Asset = asset;
                this.ReferenceAssets = referenceAssets;
            }
        }

        //----- field -----

        private FindDependencyAssets findDependencyAssets = new FindDependencyAssets();
        private AssetBundleInfo[] assetBundleInfo = null;
        private AssetReferenceInfo[] assetReferenceInfo = null;

        private ViewType selection = ViewType.Dependencies;
        private DependencyScrollView dependencyScrollView = null;
        private ReferenceScrollView referenceScrollView = null;
        private bool ignoreDependentAssetBundle = false;
        private bool ignoreScriptAsset = false;
        private string searchText = string.Empty;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            AssetDatabase.SaveAssets();

            Instance.Initialize();
        }

        private void Initialize()
        {
            titleContent = new GUIContent("FindDependencyAssetsWindow");
            minSize = MinWindowSize;

            // 参照情報を収集.
            findDependencyAssets.CollectDependencies();

            // 依存情報を構築.
            if (findDependencyAssets.AssetBundleDependentInfos.Any())
            {
                var list = new List<AssetBundleInfo>();

                var count = 0;
                var size = findDependencyAssets.AssetBundleDependentInfos.Count;

                foreach (var item in findDependencyAssets.AssetBundleDependentInfos)
                {
                    EditorUtility.DisplayProgressBar("Find Dependency", item.Key, (float)count / size);
                    
                    list.Add(new AssetBundleInfo(item.Key, item.Value.AssetPaths, item.Value.DependentAssetPaths));

                    count++;
                }

                assetBundleInfo = list.ToArray();

                EditorUtility.ClearProgressBar();
            }
            else
            {
                assetBundleInfo = new AssetBundleInfo[0];
            }

            // 参照情報を構築.
            if (findDependencyAssets.ReferenceInfos.Any())
            {
                var list = new List<AssetReferenceInfo>();

                var count = 0;
                var size = findDependencyAssets.ReferenceInfos.Count;

                foreach (var item in findDependencyAssets.ReferenceInfos)
                {
                    EditorUtility.DisplayProgressBar("Find Reference", item.AssetPath, (float)count / size);

                    list.Add(new AssetReferenceInfo(item.AssetPath, item.ReferenceAssetBundles.ToArray()));

                    count++;
                }

                assetReferenceInfo = list.ToArray();

                EditorUtility.ClearProgressBar();
            }
            else
            {
                assetReferenceInfo = new AssetReferenceInfo[0];
            }

            dependencyScrollView = new DependencyScrollView();
            dependencyScrollView.Contents = GetListOfDependentInfos();

            referenceScrollView = new ReferenceScrollView();
            referenceScrollView.Contents = GetListOfReferenceInfos();

            ShowUtility();
            initialized = true;
        }

        void OnGUI()
        {
            if (!initialized) { return; }

            DrawHeader();
            DrawContents();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();

                selection = (ViewType)EditorGUILayout.EnumPopup(selection, GUILayout.Width(150f));

                if (EditorGUI.EndChangeCheck())
                {
                    searchText = string.Empty;

                    switch (selection)
                    {
                        case ViewType.Dependencies:
                            dependencyScrollView.ScrollPosition = Vector2.zero;
                            dependencyScrollView.Contents = GetListOfDependentInfos();
                            break;

                        case ViewType.Reference:
                            referenceScrollView.ScrollPosition = Vector2.zero;
                            referenceScrollView.Contents = GetListOfReferenceInfos();
                            break;
                    }
                }

                GUILayout.FlexibleSpace();

                switch (selection)
                {
                    case ViewType.Reference:
                        {
                            var originLabelWidth = EditorLayoutTools.SetLabelWidth(180f);

                            EditorGUI.BeginChangeCheck();

                            ignoreDependentAssetBundle = EditorGUILayout.Toggle("Ignore dependent assetbundle", ignoreDependentAssetBundle);

                            if (EditorGUI.EndChangeCheck())
                            {
                                referenceScrollView.ScrollPosition = Vector2.zero;
                                referenceScrollView.Contents = GetListOfReferenceInfos();
                            }

                            EditorLayoutTools.SetLabelWidth(80f);

                            EditorGUI.BeginChangeCheck();

                            ignoreScriptAsset = EditorGUILayout.Toggle("Ignore script", ignoreScriptAsset);

                            if (EditorGUI.EndChangeCheck())
                            {
                                referenceScrollView.ScrollPosition = Vector2.zero;
                                referenceScrollView.Contents = GetListOfReferenceInfos();
                            }

                            EditorLayoutTools.SetLabelWidth(originLabelWidth);
                        }
                        break;
                }
            }

            EditorGUILayout.Space(2f);
        }
        
        private void DrawContents()
        {
            var updateSearch = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var before = searchText;
                var after = EditorGUILayout.DelayedTextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                if (before != after)
                {
                    searchText = after;
                    updateSearch = true;
                }

                if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    updateSearch = true;
                }
            }

            GUILayout.Space(2f);

            switch (selection)
            {
                case ViewType.Dependencies:
                    {
                        if (updateSearch)
                        {
                            dependencyScrollView.ScrollPosition = Vector2.zero;
                            dependencyScrollView.Contents = GetListOfDependentInfos();
                        }

                        dependencyScrollView.Draw();
                    }
                    break;

                case ViewType.Reference:
                    {
                        if (updateSearch)
                        {
                            referenceScrollView.ScrollPosition = Vector2.zero;
                            referenceScrollView.Contents = GetListOfReferenceInfos();
                        }

                        referenceScrollView.Draw();
                    }
                    break;
            }
        }


        private static bool HasAssetBundleName(string assetPath)
        {
            var assetBundleName = UnityEditorUtility.GetAssetBundleName(assetPath);

            return !string.IsNullOrEmpty(assetBundleName);
        }

        private static bool IsScriptAssets(string assetPath)
        {
            return Path.GetExtension(assetPath) == ".cs";
        }

        private AssetReferenceInfo[] GetListOfReferenceInfos()
        {
            // 条件フィルタ.
            var infos = assetReferenceInfo
                // アセットバンドル名が付いていたら除外.
                .Where(x => !ignoreDependentAssetBundle || HasAssetBundleName(x.Asset) == false)
                // スクリプト(.cs)なら除外.
                .Where(x => !ignoreScriptAsset || IsScriptAssets(x.Asset) == false)
                .ToArray();

            if (string.IsNullOrEmpty(searchText)) { return infos; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            Func<AssetReferenceInfo, bool> filter = info =>
            {
                return info.Asset.IsMatch(keywords);
            };

            return infos.Where(x => filter(x)).ToArray();
        }

        private AssetBundleInfo[] GetListOfDependentInfos()
        {
            if (string.IsNullOrEmpty(searchText)) { return assetBundleInfo; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            Func<AssetBundleInfo, bool> filter = info =>
            {
                var result = false;

                result |= info.AssetBundleName.IsMatch(keywords);
                result |= info.DependentAssets.Any(y => y.IsMatch(keywords));

                return result;
            };

            return assetBundleInfo.Where(x => filter(x)).ToArray();
        }
    }
}

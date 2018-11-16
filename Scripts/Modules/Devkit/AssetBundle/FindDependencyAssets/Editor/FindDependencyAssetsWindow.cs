﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundles
{
	public class FindDependencyAssetsWindow : SingletonEditorWindow<FindDependencyAssetsWindow>
	{
        //----- params -----

        private readonly Vector2 MinWindowSize = new Vector2(500f, 650f);

        private enum ViewType
        {
            Dependencies,
            Reference,
        }

        public class AssetBundleInfo
        {
            public string AssetBundleName { get; private set; }
            public Object[] Assets { get; private set; }
            public Object[] DependentAssets { get; private set; }

            public bool IsOpen { get; set; }

            public AssetBundleInfo(string assetBundleName, Object[] assets, Object[] dependentAssets)
            {
                this.AssetBundleName = assetBundleName;
                this.Assets = assets;
                this.DependentAssets = dependentAssets;

                IsOpen = false;
            }
        }

        public class AssetReferenceInfo
        {
            public Object Asset { get; private set; }
            public Object[] ReferenceAssets { get; private set; }

            public AssetReferenceInfo(Object asset, Object[] referenceAssets)
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

                    var assets = item.Value.AssetPaths.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();
                    var dependentAssets = item.Value.DependentAssetPaths.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();
                    
                    list.Add(new AssetBundleInfo(item.Key, assets, dependentAssets));

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

                    var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                    var referenceAssets = item.ReferenceAssetBundles.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();

                    list.Add(new AssetReferenceInfo(asset, referenceAssets));

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
            referenceScrollView.Contents = assetReferenceInfo;

            ShowUtility();
            initialized = true;
        }

        void OnGUI()
        {
            if(!initialized) { return; }

            DrawHeader();
            DrawDependencies();
            DrawReference();
        }

        private void DrawHeader()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbarButton))
            {
                EditorGUI.BeginChangeCheck();

                selection = (ViewType)EditorGUILayout.EnumPopup(selection, GUILayout.Width(150f));

                if(EditorGUI.EndChangeCheck())
                {
                    dependencyScrollView.ScrollPosition = Vector2.zero;
                    referenceScrollView.ScrollPosition = Vector2.zero;
                }
            }

            EditorGUILayout.Separator();
        }

        private void DrawDependencies()
        {
            if(selection != ViewType.Dependencies) { return; }

            using(new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var before = searchText;
                var after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                if(before != after)
                {
                    searchText = after;

                    dependencyScrollView.ScrollPosition = Vector2.zero;
                    dependencyScrollView.Contents = GetListOfDependentInfos();
                }

                if(GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;

                    dependencyScrollView.ScrollPosition = Vector2.zero;
                    dependencyScrollView.Contents = GetListOfDependentInfos();
                }
            }
            
            GUILayout.Space(2f);

            dependencyScrollView.Draw();
        }

        private void DrawReference()
        {
            if(selection != ViewType.Reference){ return; }

            referenceScrollView.Draw();
        }

        private AssetBundleInfo[] GetListOfDependentInfos()
        {
            if (string.IsNullOrEmpty(searchText)) { return assetBundleInfo; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return assetBundleInfo
                .Where(x => x.AssetBundleName.IsMatch(keywords) ||
                            x.DependentAssets.Any(y => AssetDatabase.GetAssetPath(y).IsMatch(keywords)))
                .ToArray();
        }
    }
}

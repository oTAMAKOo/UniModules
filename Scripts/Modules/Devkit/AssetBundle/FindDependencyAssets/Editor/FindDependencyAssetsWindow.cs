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

        private readonly Vector2 WindowSize = new Vector2(500f, 650f);

        private enum ViewType
        {
            Dependencies,
            Reference,
        }

        private class AssetBundleInfo
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

                IsOpen = true;
            }
        }

        private class AssetReferenceInfo
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
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = string.Empty;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {            
            AssetDatabase.SaveAssets();

            Instance.Initialize();
        }

        public void Initialize()
        {
            titleContent = new GUIContent("FindDependencyAssetsWindow");
            minSize = WindowSize;
            maxSize = WindowSize;

            findDependencyAssets.CollectDependencies();

            assetBundleInfo = findDependencyAssets.AssetBundleDependentInfos
                .Select(
                    x =>
                    {
                        var assets = x.Value.AssetPaths.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();
                        var dependentAssets = x.Value.DependentAssetPaths.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();

                        return new AssetBundleInfo(x.Key, assets, dependentAssets);
                    })
                .ToArray();

            assetReferenceInfo = findDependencyAssets.ReferenceInfos
                .Select(
                    x =>
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(x.AssetPath);
                        var referenceAssets = x.ReferenceAssetBundles.Select(y => AssetDatabase.LoadMainAssetAtPath(y)).ToArray();

                        return new AssetReferenceInfo(asset, referenceAssets);
                    })
                .ToArray();

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
                    scrollPosition = Vector2.zero;
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
                    scrollPosition = Vector2.zero;
                }

                if(GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    scrollPosition = Vector2.zero;
                }
            }

            var infos = GetListOfDependentInfos();

            GUILayout.Space(2f);

            using(var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                foreach(var info in infos)
                {
                    info.IsOpen = EditorLayoutTools.DrawHeader(info.AssetBundleName, info.IsOpen);

                    if(info.IsOpen)
                    {
                        using(new ContentsScope())
                        {
                            if(info.Assets.Any())
                            {
                                foreach(var asset in info.Assets)
                                {
                                    using(new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorLayoutTools.DrawLabelWithBackground("Asset", new Color(0.9f, 0.4f, 0.4f, 0.3f), null, TextAnchor.MiddleCenter, 90f);
                                        EditorGUILayout.ObjectField("", asset, typeof(Object), false, GUILayout.Width(250f));
                                    }
                                }
                            }

                            if(info.DependentAssets.Any())
                            {
                                foreach(var dependentAsset in info.DependentAssets)
                                {
                                    using(new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorLayoutTools.DrawLabelWithBackground("Dependent", new Color(0.4f, 0.4f, 0.9f, 0.5f), null, TextAnchor.MiddleCenter, 90f);
                                        EditorGUILayout.ObjectField("", dependentAsset, typeof(Object), false, GUILayout.Width(250f));
                                    }
                                }
                            }
                        }
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private void DrawReference()
        {
            if(selection != ViewType.Reference){ return; }

            using(var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                foreach(var info in assetReferenceInfo)
                {
                    var count = info.ReferenceAssets.Length;

                    if(count < 2) { continue; }

                    using(new EditorGUILayout.HorizontalScope())
                    {
                        EditorLayoutTools.DrawLabelWithBackground(count.ToString(), new Color(1f, 0.65f, 0f, 0.5f), null, TextAnchor.MiddleCenter, 30f);
                        EditorGUILayout.ObjectField("", info.Asset, typeof(Object), false, GUILayout.Width(250f));
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private AssetBundleInfo[] GetListOfDependentInfos()
        {
            if(string.IsNullOrEmpty(searchText)){ return assetBundleInfo; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for(var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return assetBundleInfo
                .Where(x => x.DependentAssets.Any(y => AssetDatabase.GetAssetPath(y).IsMatch(keywords)))
                .ToArray();
        }
    }
}
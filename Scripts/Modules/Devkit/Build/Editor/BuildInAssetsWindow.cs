﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    public class BuildInAssetsWindow : SingletonEditorWindow<BuildInAssetsWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(530f, 450f);

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = null;
        private BuildInAssets.BuildInAssetInfo[] buildInAssetInfo = null;
        private Dictionary<string, Object> assetObjectByAssetPath = null;

        private string[] buildInAssetTargetPaths = null;
        private string[] ignoreBuildInAssetTargetPaths = null;
        private string[] ignoreBuildInFolderNames = null;
        private string[] ignoreValidationPaths = null;
        private string[] ignoreValidationExtensions = null;
        private float warningAssetSize = 0f;

        private AssetViewMode assetViewMode = AssetViewMode.Asset;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) { return; }

            var buildConfig = BuildConfig.Instance;

            titleContent = new GUIContent("BuildInAssets Report");
            minSize = WindowSize;

            buildInAssetInfo = new BuildInAssets.BuildInAssetInfo[0];
            buildInAssetTargetPaths = buildConfig.BuildInAssetTargets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreBuildInAssetTargetPaths = buildConfig.IgnoreBuildInAssetTargets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreBuildInFolderNames = buildConfig.IgnoreBuildInFolderNames;
            ignoreValidationPaths = buildConfig.IgnoreValidationAssets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreValidationExtensions = buildConfig.IgnoreValidationExtensions;
            warningAssetSize = buildConfig.WarningAssetSize;

            // 読み込みが終わったら表示.
            CollectBuildInAssets().Subscribe(_ => ShowUtility()).AddTo(Disposable);

            isInitialized = true;
        }

        public IObservable<Unit> CollectBuildInAssets()
        {
            return BuildInAssets.CollectBuildInAssets()
                .SelectMany(x => LoadBuildInAssets(x).ToObservable())
                .Do(_ => Repaint())
                .AsUnitObservable();
        }

        private IEnumerator LoadBuildInAssets(BuildInAssets.BuildInAssetInfo[] assetInfos)
        {
            const int FrameLoadCount = 100;

            // 対象外除外.
            buildInAssetInfo = assetInfos
                // 除外対象に指定されたAssetの子階層の場合.
                .Where(x => ignoreValidationPaths.All(y => !x.assetPath.StartsWith(y)))
                // 除外対象の拡張子のファイルの場合.
                .Where(x => ignoreValidationExtensions.All(y => y != Path.GetExtension(x.assetPath)))
                .ToArray();

            if (buildInAssetInfo != null)
            {
                var count = 0;
                var progressTitle = "progress";
                var progressMessage = "Loading assets";

                assetObjectByAssetPath = new Dictionary<string, Object>();

                EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

                for (var i = 0; i < buildInAssetInfo.Length; i++)
                {
                    var info = buildInAssetInfo[i];

                    var asset = AssetDatabase.LoadMainAssetAtPath(info.assetPath);

                    if (!assetObjectByAssetPath.ContainsKey(info.assetPath))
                    {
                        assetObjectByAssetPath.Add(info.assetPath, asset);
                    }

                    if (FrameLoadCount <= count++)
                    {
                        EditorUtility.DisplayProgressBar(progressTitle, progressMessage, (float)i / buildInAssetInfo.Length);

                        yield return null;
                    }
                }

                EditorUtility.ClearProgressBar();
            }
        }

        void OnGUI()
        {
            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(5f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                        GUILayout.Space(5f);

                        string before = searchText;
                        string after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                        if (before != after)
                        {
                            searchText = after;
                            scrollPosition = Vector2.zero;
                        }

                        if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                        {
                            searchText = string.Empty;
                            GUIUtility.keyboardControl = 0;
                            scrollPosition = Vector2.zero;
                        }
                    }

                    GUILayout.Space(5f);

                    var list = GetMatchOfList();

                    if (list.Any())
                    {
                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                        {
                            foreach (var item in list)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    var asset = assetObjectByAssetPath.GetValueOrDefault(item.assetPath);

                                    if (asset != null)
                                    {
                                        EditorGUILayout.LabelField(item.GetSizeText(), GUILayout.Width(70f));

                                        EditorGUILayout.LabelField(string.Format("{0}%", item.ratio), GUILayout.Width(50f));

                                        switch (assetViewMode)
                                        {
                                            case AssetViewMode.Asset:
                                                EditorGUILayout.ObjectField(asset, typeof(Object), false, GUILayout.MinWidth(250f));
                                                break;

                                            case AssetViewMode.Path:
                                                EditorGUILayout.DelayedTextField(item.assetPath, GUILayout.MinWidth(250f));
                                                break;
                                        }

                                        var isInvalidAsset =
                                            !buildInAssetTargetPaths.Any(x => item.assetPath.StartsWith(x)) ||          // 指定ディレクトリ以外のディレクトリに存在.
                                            ignoreBuildInAssetTargetPaths.Any(x => item.assetPath.StartsWith(x)) ||     // 含まれてはいけないディレクトリに存在.
                                            ignoreBuildInFolderNames.Any(x => item.assetPath.Split('/').Contains(x));   // 含まれていけないフォルダ名が含まれているか.

                                        // 指定されたAsset置き場にない or 同梱しないAsset置き場のAssetが混入.
                                        if (isInvalidAsset)
                                        {
                                            EditorLayoutTools.DrawLabelWithBackground("InvalidAsset", Color.red, Color.gray, width: 85f);
                                        }

                                        // ファイルサイズが指定された値を超えている.
                                        if (warningAssetSize <= item.size)
                                        {
                                            EditorLayoutTools.DrawLabelWithBackground("LargeAsset", Color.yellow, Color.gray, width: 80f);
                                        }

                                        GUILayout.Space(5f);
                                    }
                                }
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Asset Not Found.", MessageType.Info);
                    }

                    GUILayout.Space(5f);
                }

                GUILayout.Space(5f);
            }
        }

        private BuildInAssets.BuildInAssetInfo[] GetMatchOfList()
        {
            if (string.IsNullOrEmpty(searchText)) { return buildInAssetInfo; }

            var list = new List<BuildInAssets.BuildInAssetInfo>();

            string[] keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            foreach (var item in buildInAssetInfo)
            {
                var isMatch = item.assetPath.IsMatch(keywords);

                if (isMatch)
                {
                    list.Add(item);
                }
            }

            return list.ToArray();
        }
    }
}
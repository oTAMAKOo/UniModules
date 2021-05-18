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
    public sealed class BuiltInAssetsWindow : SingletonEditorWindow<BuiltInAssetsWindow>
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
        private BuiltInAssets.BuiltInAssetInfo[] builtInAssetInfo = null;
        private Dictionary<string, Object> assetObjectByAssetPath = null;

        private string[] builtInAssetTargetPaths = null;
        private string[] ignoreBuiltInAssetTargetPaths = null;
        private string[] ignoreBuiltInFolderNames = null;
        private string[] ignoreValidationPaths = null;
        private string[] ignoreValidationExtensions = null;
        private float warningAssetSize = 0f;

        private AssetViewMode assetViewMode = AssetViewMode.Asset;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            // ビルドログファイルを選択.

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var unityEditorLogDirectory = localAppData + "/Unity/Editor/";

            var logFilePath = EditorUtility.OpenFilePanel("Open File As", unityEditorLogDirectory, "log");

            if (string.IsNullOrEmpty(logFilePath)) { return; }

            if (!File.Exists(logFilePath)) { return; }

            Instance.Initialize(logFilePath);
        }

        private void Initialize(string logFilePath)
        {
            if (isInitialized) { return; }

            var builtInAssetConfig = BuiltInAssetConfig.Instance;

            titleContent = new GUIContent("BuiltInAssets Report");
            minSize = WindowSize;

            builtInAssetInfo = new BuiltInAssets.BuiltInAssetInfo[0];
            builtInAssetTargetPaths = builtInAssetConfig.BuiltInAssetTargets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreBuiltInAssetTargetPaths = builtInAssetConfig.IgnoreBuiltInAssetTargets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreBuiltInFolderNames = builtInAssetConfig.IgnoreBuiltInFolderNames;
            ignoreValidationPaths = builtInAssetConfig.IgnoreValidationAssets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
            ignoreValidationExtensions = builtInAssetConfig.IgnoreValidationExtensions;
            warningAssetSize = builtInAssetConfig.WarningAssetSize;

            // 読み込みが終わったら表示.
            CollectBuiltInAssets(logFilePath).Subscribe(_ => ShowUtility()).AddTo(Disposable);

            isInitialized = true;
        }

        private IObservable<Unit> CollectBuiltInAssets(string logFilePath)
        {
            return BuiltInAssets.CollectBuiltInAssets(logFilePath)
                .SelectMany(x => LoadBuiltInAssets(x).ToObservable())
                .Do(_ => Repaint())
                .AsUnitObservable();
        }

        private IEnumerator LoadBuiltInAssets(BuiltInAssets.BuiltInAssetInfo[] assetInfos)
        {
            const int FrameLoadCount = 100;

            // 対象外除外.
            builtInAssetInfo = assetInfos
                // 除外対象に指定されたAssetの子階層の場合.
                .Where(x => ignoreValidationPaths.All(y => !x.assetPath.StartsWith(y)))
                // 除外対象の拡張子のファイルの場合.
                .Where(x => ignoreValidationExtensions.All(y => y != Path.GetExtension(x.assetPath)))
                .ToArray();

            if (builtInAssetInfo != null)
            {
                var count = 0;
                var progressTitle = "progress";
                var progressMessage = "Loading assets";

                assetObjectByAssetPath = new Dictionary<string, Object>();

                EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

                for (var i = 0; i < builtInAssetInfo.Length; i++)
                {
                    var info = builtInAssetInfo[i];

                    var asset = AssetDatabase.LoadMainAssetAtPath(info.assetPath);

                    if (!assetObjectByAssetPath.ContainsKey(info.assetPath))
                    {
                        assetObjectByAssetPath.Add(info.assetPath, asset);
                    }

                    if (FrameLoadCount <= count++)
                    {
                        EditorUtility.DisplayProgressBar(progressTitle, progressMessage, (float)i / builtInAssetInfo.Length);

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
                                            !builtInAssetTargetPaths.Any(x => item.assetPath.StartsWith(x)) ||          // 指定ディレクトリ以外のディレクトリに存在.
                                            ignoreBuiltInAssetTargetPaths.Any(x => item.assetPath.StartsWith(x)) ||     // 含まれてはいけないディレクトリに存在.
                                            ignoreBuiltInFolderNames.Any(x => item.assetPath.Split('/').Contains(x));   // 含まれていけないフォルダ名が含まれているか.

                                        var titleStyle = new EditorLayoutTools.TitleGUIStyle();

                                        // 指定されたAsset置き場にない or 同梱しないAsset置き場のAssetが混入.
                                        if (isInvalidAsset)
                                        {
                                            titleStyle.backgroundColor = Color.red;
                                            titleStyle.labelColor = Color.gray;
                                            titleStyle.width = 85f;

                                            EditorLayoutTools.Title("InvalidAsset", titleStyle);
                                        }

                                        // ファイルサイズが指定された値を超えている.
                                        if (warningAssetSize <= item.size)
                                        {
                                            titleStyle.backgroundColor = Color.yellow;
                                            titleStyle.labelColor = Color.gray;
                                            titleStyle.width = 80f;

                                            EditorLayoutTools.Title("LargeAsset", titleStyle);
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

        private BuiltInAssets.BuiltInAssetInfo[] GetMatchOfList()
        {
            if (string.IsNullOrEmpty(searchText)) { return builtInAssetInfo; }

            var list = new List<BuiltInAssets.BuiltInAssetInfo>();

            string[] keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            foreach (var item in builtInAssetInfo)
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


using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.Build
{
	public enum AssetViewMode
	{
		Asset,
		Path
	}

    public sealed class BuiltInAssetsWindow : SingletonEditorWindow<BuiltInAssetsWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(530f, 450f);

		//----- field -----
		
        private string searchText = null;

		private BuiltInAssets.BuiltInAssetInfo[] builtInAssetInfo = null;

		private BuiltInAssetScrollView builtInAssetScrollView = null;

		private string[] ignoreValidationPaths = null;
		private string[] ignoreValidationExtensions = null;

		private AssetViewMode assetViewMode = AssetViewMode.Asset;

		[NonSerialized]
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

			builtInAssetScrollView = new BuiltInAssetScrollView();
			builtInAssetScrollView.Setup();
			builtInAssetScrollView.SetAssetViewMode(assetViewMode);

			ignoreValidationPaths = builtInAssetConfig.IgnoreValidationAssets.Select(x => AssetDatabase.GetAssetPath(x)).ToArray();
			ignoreValidationExtensions = builtInAssetConfig.IgnoreValidationExtensions;

            ShowUtility();

            CollectBuiltInAssets(logFilePath);

            isInitialized = true;
        }

        private void CollectBuiltInAssets(string logFilePath)
        {
            var builtInAssets = BuiltInAssets.CollectBuiltInAssets(logFilePath);

			// 対象外除外.
			builtInAssetInfo = builtInAssets
				// 除外対象に指定されたAssetの子階層の場合.
				.Where(x => ignoreValidationPaths.All(y => !x.assetPath.StartsWith(y)))
				// 除外対象の拡張子のファイルの場合.
				.Where(x => ignoreValidationExtensions.All(y => y != Path.GetExtension(x.assetPath)))
				.ToArray();

			builtInAssetScrollView.Contents = builtInAssetInfo;
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

						EditorGUI.BeginChangeCheck();

                        assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

						if (EditorGUI.EndChangeCheck())
						{
							builtInAssetScrollView.SetAssetViewMode(assetViewMode);
						}

                        GUILayout.Space(5f);

                        var before = searchText;
						var after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                        if (before != after)
                        {
                            searchText = after;
							builtInAssetScrollView.Contents = GetMatchOfList();
							builtInAssetScrollView.ScrollPosition = Vector2.zero;
                        }

                        if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                        {
                            searchText = string.Empty;
                            GUIUtility.keyboardControl = 0;

							builtInAssetScrollView.Contents = GetMatchOfList();
							builtInAssetScrollView.ScrollPosition = Vector2.zero;
                        }
                    }

                    GUILayout.Space(5f);

                    if (builtInAssetScrollView.Contents.Any())
                    {
						builtInAssetScrollView.Draw();
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

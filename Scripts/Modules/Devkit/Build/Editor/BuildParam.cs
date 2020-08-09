﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using DirectoryUtility = Extensions.DirectoryUtility;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    public interface IBuildParam
    {
        void Apply(bool isBuild);
        void Restore();
    }

    public abstract class BuildParam : ScriptableObject, IBuildParam
    {
        //----- params -----

        private static class Prefs
        {
            public static string[] clonedAssets
            {
                get { return ProjectPrefs.Get<string[]>("BuildParamPrefs-clonedAssets", null); }
                set { ProjectPrefs.Set("BuildParamPrefs-clonedAssets", value); }
            }
        }

        [Serializable]
        public sealed class CloneAssetInfo
        {
            [SerializeField]
            private string source = null;
            [SerializeField]
            private string to = null;

            public string Source { get { return source; } }
            public string To { get { return to; } }
        }

        //----- field -----

        [SerializeField, HideInInspector]
        private string applicationName = string.Empty;
        [SerializeField, HideInInspector]
        private Object iconFolder = null;
        [SerializeField, HideInInspector]
        private string directiveSymbols = null;
        [SerializeField, HideInInspector]
        private string version = "1.0.0";
        [SerializeField, HideInInspector]
        private int buildVersion = 0;
        [SerializeField, HideInInspector]
        private bool development = false;
        [SerializeField, HideInInspector]
		private CloneAssetInfo[] cloneAssets = new CloneAssetInfo[0];

        //----- property -----

        // アプリファイル名.
        public string ApplicationName
        {
            get { return applicationName; }
        }
        
        // リリースバージョン.
        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        // ビルドバージョン (申請時にはこの値で更新されているか確認される).
        public int BuildVersion
        {
            get { return buildVersion; }
            set { buildVersion = value; }
        }

        // 会社名.
        public abstract string CompanyName { get; }
        // プロダクト名.
        public abstract string ProductName { get; }
        // アプリ固有の識別子.
        public abstract string ApplicationIdentifier { get; }

        //----- method -----

        /// <summary>
        /// ビルド設定を適用.
        /// </summary>
        public virtual void Apply(bool isBuild)
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            //====== ビルドに必要なAssetを複製 ======

            CloneAssets();

            //====== EditorUserBuildSettings設定 ======

            EditorUserBuildSettings.development = development;

            //====== PlayerSettings設定 ======

            PlayerSettings.bundleVersion = version;

            switch (buildTarget)
            {
                case BuildTarget.iOS:
                    PlayerSettings.iOS.buildNumber = buildVersion.ToString();
                    break;

                case BuildTarget.Android:
                    PlayerSettings.Android.bundleVersionCode = buildVersion;
                    break;
            }

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.applicationIdentifier = ApplicationIdentifier;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, directiveSymbols);

            SetIconSettings(buildTargetGroup);
        }

        private void SetIconSettings(BuildTargetGroup buildTargetGroup)
        {
            if (iconFolder == null) { return; }

            // プラットフォームのアイコンサイズを取得。
            var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(buildTargetGroup);

            var icons = new Texture2D[iconSizes.Length];

            var folderPath = AssetDatabase.GetAssetPath(iconFolder);
            var iconTextures = UnityEditorUtility.LoadAssetsInFolder<Texture2D>(folderPath);

            for (var i = 0; i < iconSizes.Length; i++)
            {
                var size = iconSizes[i];

                Texture2D texture = null;

                foreach (var iconTexture in iconTextures)
                {
                    var assetPath = AssetDatabase.GetAssetPath(iconTexture);

                    var assetName = Path.GetFileName(assetPath);

                    // icon_72x72.png.
                    if (assetName.Contains(string.Format("{0}x{1}", size, size)))
                    {
                        texture = iconTexture;
                        break;
                    }
                }

                icons[i] = texture;
            }

            PlayerSettings.SetIconsForTargetGroup(buildTargetGroup, icons);
        }

        /// <summary>
        /// ビルド完了後に実行される.
        /// </summary>
        public virtual void Restore()
        {
            DeleteClonedAssets();
        }

        private void CloneAssets()
        {
            DeleteClonedAssets();

            var clonedAssets = new List<string>();

            using (new AssetEditingScope())
            {
                foreach (var cloneAsset in cloneAssets)
                {
                    if (string.IsNullOrEmpty(cloneAsset.Source) || string.IsNullOrEmpty(cloneAsset.To))
                    {
                        Debug.LogError("アセットコピーの設定が正しくありません.");
                        continue;
                    }

                    var projectFolder = UnityPathUtility.GetProjectFolderPath();

                    var source = PathUtility.RelativePathToFullPath(projectFolder, cloneAsset.Source);
                    var dest = PathUtility.RelativePathToFullPath(projectFolder, cloneAsset.To);

                    // 除外対象か判定.
                    Func<string, bool> ignoreCheck = x =>
                    {
                        return Path.GetExtension(x) != ".meta" &&
                            !string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(x));
                    };

                    // メタファイルはコピーしない.
                    var cloned = DirectoryUtility.Clone(source, dest, ignoreCheck)
                        .Select(x => UnityPathUtility.ConvertFullPathToAssetPath(x))
                        .ToArray();

                    foreach (var item in cloned)
                    {
                        AssetDatabase.ImportAsset(item);
                    }

                    clonedAssets.AddRange(cloned);
                }
            }

            Prefs.clonedAssets = clonedAssets.Select(x => AssetDatabase.AssetPathToGUID(x)).ToArray();
        }

        private void DeleteClonedAssets()
        {
            var clonedAssets = Prefs.clonedAssets;

            if (clonedAssets == null || clonedAssets.IsEmpty()) { return; }

            // フォルダから消したい為降順にソート.
            var clonedAssetPaths = clonedAssets
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .OrderByDescending(x => x.Length)
                .ToArray();

            using (new AssetEditingScope())
            {
                foreach (var clonedAssetPath in clonedAssetPaths)
                {
                    AssetDatabase.DeleteAsset(clonedAssetPath);
                }
            }
        }

        private string GetFullPath(string relativePath)
        {
            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var origin = Environment.CurrentDirectory;

            Environment.CurrentDirectory = projectFolder;

            var path = Path.GetFullPath(relativePath);

            Environment.CurrentDirectory = origin;

            return path;
        }
    }
}

﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;
using Extensions.Devkit;

namespace Modules.Devkit.Build
{
    public static class BuildManager
    {
        //----- params -----

        private static class Prefs
        {
            public static bool isBuilding
            {
                get { return ProjectPrefs.GetBool("BuildManagerPrefs-isBuilding", false); }
                set { ProjectPrefs.SetBool("BuildManagerPrefs-isBuilding", value); }
            }

            public static bool isBuildWait
            {
                get { return ProjectPrefs.GetBool("BuildManagerPrefs-isBuildWait", false); }
                set { ProjectPrefs.SetBool("BuildManagerPrefs-isBuildWait", value); }
            }

            public static string exportDir
            {
                get { return ProjectPrefs.GetString("BuildManagerPrefs-exportDir", null); }
                set { ProjectPrefs.SetString("BuildManagerPrefs-exportDir", value); }
            }
        }

        public class ActiveBuildTargetListener : IActiveBuildTargetChanged
        {
            public int callbackOrder { get { return 0; } }

            public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
            {
                if (IsBuildWait)
                {
                    BuildPlayer();

                    IsBuildWait = false;
                }
            }
        }

        //----- field -----

        private static BuildTarget buildTarget = BuildTarget.NoTarget;
        private static BuildParam buildParam = default(BuildParam);
        private static bool batchMode = false;

        //----- property -----

        private static bool BatchMode { get { return batchMode; } }

        public static bool IsBuilding { get { return Prefs.isBuilding; } }

        public static bool IsBuildWait
        {
            get { return Prefs.isBuilding; }
            set { Prefs.isBuilding = value; }
        }

        //----- method -----

        public static void Apply(BuildTarget buildTarget, BuildParam buildParam)
        {
            BuildManager.buildTarget = buildTarget;
            BuildManager.buildParam = buildParam;
            BuildManager.batchMode = false;

            ApplyBuildSettings(false);
        }

        public static void Build(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, BuildParam buildParam, bool batchMode)
        {
            BuildManager.buildTarget = buildTarget;
            BuildManager.buildParam = buildParam;
            BuildManager.batchMode = batchMode;

            if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
            {
                IsBuildWait = true;

                #if UNITY_5_6_OR_NEWER
            
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
            
                #else
            
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget);
            
                #endif
            }
            else
            {
                BuildPlayer();
            }
        }

        private static void ApplyBuildSettings(bool isBuild)
        {
            // ビルド設定適用.
            AssetDatabase.StartAssetEditing();

            buildParam.Apply(isBuild);
            buildParam.Restore();

            AssetDatabase.StopAssetEditing();
        }

        private static void BuildPlayer()
        {
            Prefs.isBuilding = true;

            // ビルド設定適用.
            ApplyBuildSettings(true);

            // 出力先.
            var directory = string.Empty;
            directory = GetExportDirectory(BatchMode);

            if (string.IsNullOrEmpty(directory)) { return; }

            directory = PathUtility.Combine(directory, EditorUserBuildSettings.development ? "Development" : "Release");
            directory = PathUtility.Combine(directory, UnityPathUtility.GetPlatformName());

            // 既存の成果物を破棄.
            FileUtil.DeleteFileOrDirectory(directory);

            // 出力先作成.
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // ファイル名.
            var fileName = buildParam.ApplicationName + GetBuildTargetExtension(buildTarget);

            // 出力先.
            var path = PathUtility.Combine(directory, fileName);

            // ビルドに含めるシーン.
            var scenePaths = GetAllScenePaths();

            if (scenePaths.Length == 0)
            {
                Debug.Log("Nothing to build.");
                return;
            }
            
            #if UNITY_IOS

            ProcessForXCode.Prefs.enable = true;
            
            #endif

            // ビルド実行.
            var option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
            var error = BuildPipeline.BuildPlayer(scenePaths, path, buildTarget, option);

            // 後片付け.
            buildParam.Restore();

            Prefs.isBuilding = false;

            Debug.LogFormat("[Build] : {1}", path);

            #if UNITY_2018_1_OR_NEWER

            var success = error == null;

            #else

            var success = string.IsNullOrEmpty(error);

            #endif

            // 出力先フォルダを開く.
            if　(success)
            {
                if (!BatchMode)
                {
                    if (EditorUtility.DisplayDialog("Notification", "Build Finish!", "Open Folder", "Close"))
                    {
                        System.Diagnostics.Process.Start(directory);
                    }
                }
            }
            else
            {
                Debug.LogError(error);
            }

            // 終了処理.
            if (BatchMode)
            {
                EditorApplication.Exit(success ? 0 : 1);
            }
        }

		private static string GetExportDirectory(bool batchMode)
		{
			var exportDir = string.Empty;

			// dataPathはAssetsフォルダ.
			var directory = Path.GetDirectoryName(Application.dataPath);

			if (!batchMode)
			{
				exportDir = string.IsNullOrEmpty(Prefs.exportDir) ? directory : Prefs.exportDir;

				var dir = Path.GetDirectoryName(exportDir);
				var name = Path.GetFileName(exportDir);

				directory = EditorUtility.SaveFolderPanel("Select OutputPath", dir, name);

				if (!string.IsNullOrEmpty(directory))
				{
					Prefs.exportDir = directory;
				}
				else
				{
					return null;
				}
			}

			return PathUtility.Combine(directory, PlayerSettings.productName);
		}

        private static string GetBuildTargetExtension(BuildTarget target)
        {
            var extension = string.Empty;

            switch (target)
            {
                case BuildTarget.Android:
                    extension = ".apk";
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    extension = ".exe";
                    break;
                case BuildTarget.StandaloneOSX:
                    extension = ".app";
                    break;

				default:
				extension = string.Empty; // 拡張子を定義しない場合はフォルダに出力.
					break;
            }

            return extension;
        }

        private static string[] GetAllScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }
    }
}

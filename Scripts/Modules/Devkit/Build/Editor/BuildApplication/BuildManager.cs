
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Build
{
    public static class BuildManager
    {
        //----- params -----

        private static class Prefs
        {
            public static bool buildRequest
            {
                get { return ProjectPrefs.GetBool("BuildManagerPrefs-buildRequest", false); }
                set { ProjectPrefs.SetBool("BuildManagerPrefs-buildRequest", value); }
            }

            public static string builderClassTypeName
            {
                get { return ProjectPrefs.GetString("BuildManagerPrefs-builderClassTypeName", null); }
                set { ProjectPrefs.SetString("BuildManagerPrefs-builderClassTypeName", value); }
            }

            public static string exportDir
            {
                get { return ProjectPrefs.GetString("BuildManagerPrefs-exportDir", null); }
                set { ProjectPrefs.SetString("BuildManagerPrefs-exportDir", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static async void DidReloadScripts()
        {
            if (!Prefs.buildRequest){ return; }

            var applicationBuilder = CreateSavedBuilderInstance();

            await Build(applicationBuilder);
        }

        public static async Task Build(IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                Prefs.buildRequest = false;
                return;
            }

            Prefs.buildRequest = true;

            var batchMode = Application.isBatchMode;

            var buildTarget = applicationBuilder.BuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            // ビルドクラスの型を保存.
            Prefs.builderClassTypeName = applicationBuilder.GetType().FullName;

            //------ DefineSymbol設定 ------

            var defineSymbols = string.Empty;

            var currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (applicationBuilder.DefineSymbols != null)
            {
                defineSymbols = string.Join(";", applicationBuilder.DefineSymbols);
            }

            if (defineSymbols != currentDefineSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

                // 現在のビルドターゲットのDefineSymbolを変更するとコンパイルが実行される為コンパイル後に再度ビルドを実行する.
                if (!batchMode && EditorUserBuildSettings.activeBuildTarget == buildTarget)
                {
                    return;
                }
            }

            //------ プラットフォーム切り替え ------

            if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                return;
            }

            // アセンブリリロードを停止.
            using (new LockReloadAssembliesScope())
            {
                //------ ビルド実行 ------

                var success = false;

                Prefs.buildRequest = false;

                using (new DisableStackTraceScope(LogType.Log))
                {
                    // 出力先フォルダ.
                    // ※ Editorの場合は出力先選択ダイアログを開く.
                    var directory = PrepareExportDirectory(applicationBuilder, batchMode);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        // 出力先.
                        var path = GetBuildPath(applicationBuilder, directory);

                        // ビルドに含めるシーン.
                        var scenePaths = applicationBuilder.GetAllScenePaths();

                        // ビルド前処理.

                        await applicationBuilder.OnBeforeBuild();

                        // ビルド実行.

                        EditorUserBuildSettings.development = applicationBuilder.Development;

                        var option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;

                        option = option | applicationBuilder.BuildOptions;

                        var buildReport = BuildPipeline.BuildPlayer(scenePaths, path, buildTarget, option);

                        success = buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;

                        // ビルド結果処理.
                        if (success)
                        {
                            await applicationBuilder.OnBuildSuccess();
                        }
                        else
                        {
                            await applicationBuilder.OnBuildError(buildReport);
                        }

                        // ビルド後処理.
                        await applicationBuilder.OnAfterBuild(success);

                        // 出力先フォルダを開く.
                        if (success && !batchMode)
                        {
                            if (EditorUtility.DisplayDialog("Build", "Build finish.", "Open Folder", "Close"))
                            {
                                System.Diagnostics.Process.Start(directory);
                            }
                        }
                    }
                }

                // 終了処理.
                if (batchMode)
                {
                    EditorApplication.Exit(success ? 0 : 1);
                }
            }
        }

        private static IApplicationBuilder CreateSavedBuilderInstance()
        {
            var builderClassTypeName = Prefs.builderClassTypeName;

            if (string.IsNullOrEmpty(builderClassTypeName)) { return null; }

            var classType = Type.GetType(builderClassTypeName);

            var applicationBuilder = Activator.CreateInstance(classType) as IApplicationBuilder;

            if (applicationBuilder != null)
            {
                applicationBuilder.OnCreateInstance();
            }

            return applicationBuilder;
        }

        private static string GetBuildPath(IApplicationBuilder applicationBuilder, string directory)
        {
            // 拡張子.
            var extension = GetBuildTargetExtension(applicationBuilder.BuildTarget);

            // ファイル名.
            var applicationName = applicationBuilder.GetApplicationName();
            var fileName = Path.ChangeExtension(applicationName, extension);

            // 出力先.
            var path = PathUtility.Combine(directory, fileName);

            return path;
        }

        private static string PrepareExportDirectory(IApplicationBuilder applicationBuilder, bool batchMode)
        {
            // 出力先.
            var directory = string.Empty;

            directory = GetExportDirectory(applicationBuilder, batchMode);

            if (string.IsNullOrEmpty(directory)) { return null; }

            var platformName = PlatformUtility.GetPlatformName();

            var exportFolderName = applicationBuilder.GetExportFolderName();

            directory = PathUtility.Combine(new string[] { directory, platformName, exportFolderName });

            // 既存の成果物を破棄.
            FileUtil.DeleteFileOrDirectory(directory);

            // 出力先作成.
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        private static string GetExportDirectory(IApplicationBuilder applicationBuilder, bool batchMode)
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
            
            return PathUtility.Combine(directory, "Build");
        }

        private static string GetBuildTargetExtension(BuildTarget target)
        {
            var extension = string.Empty;

            switch (target)
            {
                case BuildTarget.Android:
                    extension = ".apk";
                    break;

                case BuildTarget.iOS:
                    extension = ".ipa";
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
    }
}

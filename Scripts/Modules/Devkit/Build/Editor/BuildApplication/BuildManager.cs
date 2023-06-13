
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.Build.Reporting;
using System.IO;
using Cysharp.Threading.Tasks;
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
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-buildRequest", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-buildRequest", value); }
            }

            public static string builderClassTypeName
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-builderClassTypeName", null); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-builderClassTypeName", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnDidReloadScripts;
                return;
            }
 
            EditorApplication.delayCall += OnAfterDidReloadScripts;
        }

        private static void OnAfterDidReloadScripts()
        {
            if (!Prefs.buildRequest){ return; }

            var applicationBuilder = CreateSavedBuilderInstance();

            Build(applicationBuilder).Forget();
        }

        public static async UniTask Build(IApplicationBuilder applicationBuilder)
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

            //------ プラットフォーム切り替え ------

            if (EditorUserBuildSettings.selectedBuildTargetGroup != buildTargetGroup ||
                EditorUserBuildSettings.activeBuildTarget != buildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                return;
            }

            //------ DefineSymbol設定 ------

            var defineSymbols = string.Empty;

            var currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (applicationBuilder.DefineSymbols != null)
            {
                defineSymbols = string.Join(";", applicationBuilder.DefineSymbols);
            }

            using (new DisableStackTraceScope())
            {
                Debug.Log($"DefineSymbols : {defineSymbols}");
            }

            if (defineSymbols != currentDefineSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

                CompilationPipeline.RequestScriptCompilation();

                return;
            }

            //------ ビルド実行 ------

            using (new LockReloadAssembliesScope())
            {
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

                        var beforeBuildSuccess = await applicationBuilder.OnBeforeBuild();

                        if (beforeBuildSuccess)
                        {
                            // ビルド実行.

                            EditorUserBuildSettings.development = applicationBuilder.Development;

                            var option = EditorUserBuildSettings.development
                                ? BuildOptions.Development
                                : BuildOptions.None;

                            option |= applicationBuilder.BuildOptions;

                            var buildReport = BuildPipeline.BuildPlayer(scenePaths, path, buildTarget, option);

                            success = buildReport.summary.result == BuildResult.Succeeded;

                            // ビルド結果処理.
                            if (success)
                            {
                                await applicationBuilder.OnBuildSuccess(buildReport);
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
                                    var _ = System.Diagnostics.Process.Start(directory);
                                }
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
            var directory = applicationBuilder.GetExportDirectory(batchMode);

            if (string.IsNullOrEmpty(directory)) { return null; }

            var platformName = PlatformUtility.GetPlatformName();

            directory = PathUtility.Combine(directory, platformName);

            // 出力先作成.
            if (Directory.Exists(directory))
            {
                DirectoryUtility.Clean(directory);
            }
            else
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        private static string GetBuildTargetExtension(BuildTarget target)
        {
            var extension = string.Empty;

            switch (target)
            {
                case BuildTarget.Android:
                    extension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
                    break;

                case BuildTarget.iOS:
                    extension = ".project";
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

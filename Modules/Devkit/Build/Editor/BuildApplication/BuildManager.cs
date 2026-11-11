
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.Text;
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
            public static bool requestReload
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-requestReload", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-requestReload", value); }
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

        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoadMethod()
        {
            // 起動時のみ実行する.
            if (1 < EditorApplication.timeSinceStartup) { return; }

            Prefs.requestReload = false;
        }

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
            if (!Prefs.requestReload){ return; }

            Prefs.requestReload = false;

            var applicationBuilder = CreateSavedBuilderInstance();

            Build(applicationBuilder).Forget();
        }

        public static async UniTask Build(IApplicationBuilder applicationBuilder)
        {
            Prefs.requestReload = false;

            if (applicationBuilder == null) { return; }

            var batchMode = Application.isBatchMode;

            var buildTarget = applicationBuilder.BuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            // ビルドクラスの型を保存.
            Prefs.builderClassTypeName = applicationBuilder.GetType().FullName;

            //------ プラットフォーム切り替え ------

            if (EditorUserBuildSettings.selectedBuildTargetGroup != buildTargetGroup ||
                EditorUserBuildSettings.activeBuildTarget != buildTarget)
            {
                Prefs.requestReload = true;

                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                return;
            }

            //------ DefineSymbol設定 ------

            var defineSymbols = string.Empty;

            var currentDefineSymbols = string.Empty;

            #if UNITY_6000_0_OR_NEWER

            currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            #else

            currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            #endif

            using (new DisableStackTraceScope())
            {
                Debug.Log($"Current DefineSymbols : {currentDefineSymbols}");
            }

            if (applicationBuilder.DefineSymbols != null)
            {
                defineSymbols = string.Join(";", applicationBuilder.DefineSymbols);
            }

            if (defineSymbols != currentDefineSymbols)
            {
                Prefs.requestReload = true;

                using (new DisableStackTraceScope())
                {
                    Debug.Log($"Set DefineSymbols : {defineSymbols}");
                }

                #if UNITY_6000_0_OR_NEWER

                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), defineSymbols);

                #else

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

                #endif

                CompilationPipeline.RequestScriptCompilation();

                return;
            }

            //------ ビルド実行 ------

            var builder = new StringBuilder();

            using (new LockReloadAssembliesScope())
            {
                var success = false;

                using (new DisableStackTraceScope(LogType.Log))
                {
                    // 出力先フォルダ.
                    // ※ Editorの場合は出力先選択ダイアログを開く.
                    var directory = PrepareExportDirectory(applicationBuilder, batchMode);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        // ビルド前処理.

                        var beforeBuildSuccess = await applicationBuilder.OnBeforeBuild();

                        if (!beforeBuildSuccess)
                        {
                            Debug.Log("ApplicationBuilder before build process failed.");
                            return;
                        }

                        // 出力先.
                        var path = GetBuildPath(applicationBuilder, directory);

                        Debug.Log($"Export : {path}");

                        // ビルドに含めるシーン.

                        var scenePaths = applicationBuilder.GetAllScenePaths();

                        builder.Clear();

                        builder.AppendLine("Include Scenes:").AppendLine();

                        foreach (var scenePath in scenePaths)
                        {
                            builder.AppendLine(scenePath);
                        }

                        Debug.Log(builder.ToString());

                        // ビルド実行.

                        EditorUserBuildSettings.development = applicationBuilder.Development;

                        var option = EditorUserBuildSettings.development
                            ? BuildOptions.Development
                            : BuildOptions.None;

                        option |= applicationBuilder.BuildOptions;

                        var buildPlayerOptions = new BuildPlayerOptions()
                        {
                            target = buildTarget,
                            scenes = scenePaths,
                            locationPathName = path,
                            options = option,
                        };

                        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

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

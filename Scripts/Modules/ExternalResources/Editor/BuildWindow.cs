
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using UniRx;
using Extensions.Devkit;
using Extensions;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalResource.Editor
{
	public sealed class BuildWindow : SingletonEditorWindow<BuildWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(280f, 100f);

        private static readonly string[] IgnoreDependentCheckExtensions = { ".cs" };

        //----- field -----

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("ExternalResources");

            minSize = WindowSize;
            maxSize = WindowSize;

            initialized = true;
        }

        void Update()
        {
            if (!initialized)
            {
                Reload();
            }
        }

        void OnGUI()
        {
            if (!initialized) { return; }
            
            EditorGUILayout.Separator();
            
            EditorLayoutTools.Title("AssetInfoManifest");

            if (GUILayout.Button("Generate"))
            {
                // アセット情報ファイルを生成.
                AssetInfoManifestGenerator.Generate();
            }

            GUILayout.Space(6f);

            EditorLayoutTools.Title("ExternalResource");

            if (GUILayout.Button("Generate"))
            {
                if (BuildManager.BuildConfirm())
                {
                    var build = true;

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    // CRIの最新アセットに更新.
                    CriAssetUpdater.Execute();

                    #endif

                    try
                    {
                        EditorApplication.LockReloadAssemblies();

                        // アセット情報ファイルを生成.
                        var assetInfoManifest = AssetInfoManifestGenerator.Generate();

                        // 依存関係の検証.
                        var validate = AssetDependenciesValidate(assetInfoManifest);

                        if (!validate)
                        {
                            var message = "There is an incorrect reference.\nDo you want to cancel the build?";
                            
                            if (!EditorUtility.DisplayDialog("Invalid Dependencies", message, "build", "cancel"))
                            {
                                build = false;

                                // ExternalResourceフォルダ以外の参照が含まれる場合は依存関係を表示.
                                InvalidDependantWindow.Open();
                            }
                        }

                        // ビルド.
                        if (build)
                        {
                            var exportPath = BuildManager.SelectExportPath();

                            if (!string.IsNullOrEmpty(exportPath))
                            {
                                BuildManager.Build(exportPath, assetInfoManifest)
                                    .ToObservable()
                                    .Subscribe()
                                    .AddTo(Disposable);
                            }
                        }
                    }
                    finally
                    {
                        EditorApplication.UnlockReloadAssemblies();
                    }
                }
            }

            EditorGUILayout.Separator();
        }

        private bool AssetDependenciesValidate(AssetInfoManifest assetInfoManifest)
        {
            var projectFolders = ProjectFolders.Instance;

            var config = ManageConfig.Instance;

            var externalResourcesPath = projectFolders.ExternalResourcesPath;
            
            var allAssetInfos = assetInfoManifest.GetAssetInfos().ToArray();

            var ignoreValidatePaths = config.IgnoreValidateTarget
                  .Where(x => x != null)
                  .Select(x => AssetDatabase.GetAssetPath(x))
                  .ToArray();

            Func<string, bool> checkInvalid = path =>
            {
                // 除外対象拡張子はチェック対象外.

                var extension = Path.GetExtension(path);

                if (IgnoreDependentCheckExtensions.Any(y => y == extension)) { return false; }

                // 除外対象.

                if (ignoreValidatePaths.Any(x => path.StartsWith(x))) { return false; }

                // 外部アセット対象ではない.

                if (!path.StartsWith(externalResourcesPath)) { return true; }

                return false;
            };

            foreach (var assetInfo in allAssetInfos)
            {
                var assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);

                var dependencies = AssetDatabase.GetDependencies(assetPath);

                var invalidDependencies = dependencies.Where(x => checkInvalid(x)).ToArray();

                if (invalidDependencies.Any())
                {
                    var builder = new StringBuilder();

                    builder.AppendFormat("Asset: {0}", assetPath).AppendLine();
                    builder.AppendLine("Invalid Dependencies:");

                    foreach (var item in invalidDependencies)
                    {
                        builder.AppendLine(item);
                    }

                    Debug.LogWarningFormat(builder.ToString());

                    return false;
                }
            }

            return true;
        }

        private void Reload()
        {
            initialized = true;

            Repaint();
        }
    }
}

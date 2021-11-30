
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

        private readonly Vector2 WindowSize = new Vector2(280f, 106f);

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

            GUILayout.Space(2f);

            if (GUILayout.Button("Generate"))
            {
                // アセット情報ファイルを生成.
                AssetInfoManifestGenerator.Generate();
            }

            GUILayout.Space(6f);

            EditorLayoutTools.Title("ExternalResource");

            GUILayout.Space(2f);

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
                        var validate = BuildManager.AssetDependenciesValidate(assetInfoManifest);

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
                            var exportPath = BuildManager.GetExportPath();

                            if (!string.IsNullOrEmpty(exportPath))
                            {
                                BuildManager.Build(exportPath, assetInfoManifest)
                                    .ToObservable()
                                    .Subscribe()
                                    .AddTo(Disposable);
                            }
                            else
                            {
                                Debug.LogError("The export path is not set.");

                                var config = ManageConfig.Instance;

                                if (config != null)
                                {
                                    Selection.activeObject = config;
                                }
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

        private void Reload()
        {
            initialized = true;

            Repaint();
        }
    }
}

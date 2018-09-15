﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalResource.Editor
{
	public class ExternalResourceBuildWindow : SingletonEditorWindow<ExternalResourceBuildWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(280f, 100f);

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

            var editorConfig = ProjectFolders.Instance;
            var assetManageConfig = AssetManageConfig.Instance;

            var externalResourcesPath = editorConfig.ExternalResourcesPath;

            EditorGUILayout.Separator();

            var backgroundColor = new Color(0.3f, 0.3f, 0.5f);
            var labelColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            EditorLayoutTools.DrawLabelWithBackground("AssetInfoManifest", backgroundColor, labelColor);

            if (GUILayout.Button("Generate"))
            {
                // アセット情報ファイルを生成.
                AssetInfoManifestGenerator.Generate(externalResourcesPath, assetManageConfig);
            }

            GUILayout.Space(6f);

            EditorLayoutTools.DrawLabelWithBackground("ExternalResource", backgroundColor, labelColor);

            if (GUILayout.Button("Generate"))
            {
                if (ExternalResourceManager.BuildConfirm())
                {
                    // アセット情報ファイルを生成.
                    AssetInfoManifestGenerator.Generate(externalResourcesPath, assetManageConfig);

                    #if ENABLE_CRIWARE

                    // CRIの最新アセットに更新.
                    CriAssetUpdater.Execute();

                    #endif

                    // ビルド.
                    ExternalResourceManager.Build(externalResourcesPath);

                    // 依存関係の検証.
                    var validate = AssetDependencies.Validate(externalResourcesPath);

                    // ExternalResourceフォルダ以外のアセットを参照している場合は依存関係を表示.
                    if (!validate)
                    {
                        InvalidDependantWindow.Open(externalResourcesPath);
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


using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using Extensions.Devkit;

namespace Modules.ExternalAssets
{
	public sealed class GenerateAssetInfoManifestWindow : SingletonEditorWindow<GenerateAssetInfoManifestWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(200f, 90f);

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

            titleContent = new GUIContent("AssetInfoManifest");

            minSize = WindowSize;

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

            var autoUpdater = AssetInfoManifestAutoUpdater.Instance;
			
			EditorGUILayout.Separator();

            if (GUILayout.Button("Generate"))
            {
                // アセット情報ファイルを生成.
                AssetInfoManifestGenerator.Generate().Forget();
            }

            if (autoUpdater != null)
            {
                EditorGUILayout.Separator();

                EditorLayoutTools.ContentTitle("Settings");

                EditorGUILayout.Separator();

                EditorGUI.BeginChangeCheck();

                var enable = EditorGUILayout.Toggle("Auto Generate", autoUpdater.Enable);

                if (EditorGUI.EndChangeCheck())
                {
                    autoUpdater.Enable = enable;
                }
            }
        }

        private void Reload()
        {
            initialized = true;

            Repaint();
        }
	}
}

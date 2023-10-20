
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.ExternalAssets
{
	public sealed class GenerateAssetInfoManifestWindow : SingletonEditorWindow<GenerateAssetInfoManifestWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(200f, 35f);

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

            if (GUILayout.Button("Generate"))
            {
                // アセット情報ファイルを生成.
                AssetInfoManifestGenerator.Generate().Forget();
            }
		}

        private void Reload()
        {
            initialized = true;

            Repaint();
        }
	}
}

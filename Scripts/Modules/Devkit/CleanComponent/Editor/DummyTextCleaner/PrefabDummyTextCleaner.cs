
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Modules.Devkit.CleanComponent
{
    public sealed class PrefabDummyTextCleaner : AssetModificationProcessor
    {
        //----- params -----

        //----- field -----
		
        //----- property -----

        //----- method -----

		// 初回以降はOnDestroyで破棄処理を行うので実行不要.

		public static void OnWillCreateAsset(string assetPath)
		{
			if (!assetPath.EndsWith(".prefab")){ return; }

			ModifyPrefabContents(assetPath).Forget();
		}

		private static async UniTask ModifyPrefabContents(string assetPath)
		{
			await UniTask.NextFrame();

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

			if (prefab == null) { return; }

			var changed = DummyTextCleaner.ModifyComponents(prefab);

			if (changed)
			{
				PrefabUtility.SavePrefabAsset(prefab);
			}
		}
    }
}
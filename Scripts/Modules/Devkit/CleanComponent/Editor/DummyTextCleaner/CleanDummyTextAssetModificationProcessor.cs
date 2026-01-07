
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Modules.Devkit.CleanComponent
{
    public sealed class CleanDummyTextAssetModificationProcessor : AssetModificationProcessor
    {
        //----- params -----

        //----- field -----
		
        //----- property -----

        //----- method -----

        // 初回以降はOnDestroyで破棄処理を行うので実行不要.

        public static void OnWillCreateAsset(string assetPath)
        {
            if (!assetPath.EndsWith(".prefab")){ return; }

            DelayCallCleanPrefab(assetPath).Forget();
        }

        private static async UniTask DelayCallCleanPrefab(string assetPath)
        {
            await UniTask.DelayFrame(5);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            PrefabDummyTextCleaner.ModifyPrefabContents(prefab);
        }
    }
}
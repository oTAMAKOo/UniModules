
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager
    {
        private async UniTask<T> SimulateLoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
            {
                Debug.LogError("Asset load error : " + assetPath);

                return default(T);
            }

			await UniTask.NextFrame();

            return asset;
        }
    }
}

#endif

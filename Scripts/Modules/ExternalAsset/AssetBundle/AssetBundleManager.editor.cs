
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager
    {
        private async UniTask<T> SimulateLoadAsset<T>(string assetPath, CancellationToken cancelToken) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
            {
                Debug.LogError("Asset load error : " + assetPath);

                return default(T);
            }

			await UniTask.NextFrame(cancelToken);

            return asset;
        }
    }
}

#endif


#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager
    {
        private async UniTask<T> SimulateLoadAsset<T>(string installPath, string assetPath, CancellationToken cancelToken) where T : UnityEngine.Object
		{
			var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

			if (asset == null)
			{
				Debug.LogError("Asset load error : " + assetPath);

				return default(T);
			}

			await UniTask.NextFrame(cancelToken);

			// アセットバンドルのロードイベントを発行.

			if (onLoad != null)
			{
				var dependencies = AssetDatabase.GetDependencies(assetPath);

				var list = dependencies.Append(assetPath).Distinct();

				foreach (var item in list)
				{
					var assetBundleName = AssetDatabase.GetImplicitAssetBundleName(item);

					if (string.IsNullOrEmpty(assetBundleName)){ continue; }
	                    
					var assetInfos = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName);

					if (assetInfos == null){ continue; }

					var assetInfo = assetInfos.FirstOrDefault();

					if (assetInfo == null){ continue; }

					var filePath = GetFilePath(installPath, assetInfo);

					onLoad.OnNext(filePath);
				}

				await UniTask.NextFrame(cancelToken);
			}

			return asset;
		}
	}
}

#endif

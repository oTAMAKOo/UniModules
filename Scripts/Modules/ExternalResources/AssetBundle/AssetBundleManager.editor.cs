﻿
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UniRx;
using Extensions;

namespace Modules.AssetBundles
{
    public partial class AssetBundleManager
    {
        private IObservable<T> SimulateLoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
            {
                Debug.LogError("Asset load error:" + assetPath);

                return Observable.Return<T>(default(T));
            }

            return Observable.NextFrame().Select(_ => asset);
        }
    }
}

#endif

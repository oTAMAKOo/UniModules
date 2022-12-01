
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
	public abstract class ExtendAssetFileManager : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        protected Dictionary<string, UnityEngine.Object> localAssets = null;

        //----- property -----

        //----- method -----

        void Awake()
        {
            // ファイルのロードを上書きするコールバックを登録.
            AssetFileManager.GetCustomLoadManager().OnFindAsset += ExtendFindAsset;
        }

        public void SetLocalAssets(Dictionary<string, UnityEngine.Object> localAssets)
        {
            this.localAssets = localAssets;
        }

        protected void ExtendFindAsset(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData, ref AssetFileBase asset)
        {
            if (localAssets != null)
            {
                asset = GetInternalResourcesAssetFile(mangager, fileInfo, settingData);
            }

            if(asset == null)
            {
                asset = GetExternalAssetAssetFile(mangager, fileInfo, settingData);
            }
        }

        protected abstract AssetFileBase GetInternalResourcesAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData);

        protected abstract AssetFileBase GetExternalAssetAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData);
    }
}

#endif

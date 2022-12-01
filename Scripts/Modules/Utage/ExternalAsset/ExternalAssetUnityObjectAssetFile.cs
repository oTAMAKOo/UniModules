
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
	public sealed class ExternalAssetUnityObjectAssetFile : ExternalAssetAssetFile<UnityEngine.Object>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public ExternalAssetUnityObjectAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
        {

        }

        protected override void OnLoadComplete(UnityEngine.Object asset)
        {
            UnityObject = asset;
        }
    }
}

#endif

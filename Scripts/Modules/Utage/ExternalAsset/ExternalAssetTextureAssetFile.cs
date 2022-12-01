
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
	public sealed class ExternalAssetTextureAssetFile : ExternalAssetAssetFile<Texture2D>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public ExternalAssetTextureAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
        {

        }

        protected override void OnLoadComplete(Texture2D asset)
        {
            Texture = asset;
        }
    }
}

#endif

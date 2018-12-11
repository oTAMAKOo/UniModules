
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
    public class ExternalResourcesTextAssetFile : ExternalResourcesAssetFile<TextAsset>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public ExternalResourcesTextAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
        {

        }

        protected override void OnLoadComplete(TextAsset asset)
        {
            Text = asset;
        }
    }
}

#endif

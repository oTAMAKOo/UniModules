﻿
#if ENABLE_CRIWARE
﻿
using UnityEngine;
using Extensions;

namespace Modules.SoundManagement.Editor
{
	public class SoundConfig : Modules.CriWare.Editor.CriAssetConfig<SoundConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string acfAssetSourceFullPath = null;
        [SerializeField]
        private string acfAssetExportPath = null;

        //----- property -----

        public string AcfAssetSourceFullPath { get { return UnityPathUtility.RelativePathToFullPath(acfAssetSourceFullPath); } }

        public string AcfAssetExportPath { get { return UnityPathUtility.RelativePathToFullPath(acfAssetExportPath); } }

        //----- method -----
    }
}

#endif

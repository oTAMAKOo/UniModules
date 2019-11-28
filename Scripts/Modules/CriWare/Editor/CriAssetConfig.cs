
using UnityEngine;
using System;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.CriWare.Editor
{
    public sealed class CriAssetConfig : ReloadableScriptableObject<CriAssetConfig>
    {
        //----- params -----

        [Serializable]
        public sealed class AssetImportInfo
        {
            [SerializeField]
            private string folderName = null;
            [SerializeField]
            private string importPath = null;
            
            /// <summary> インポート時のルートフォルダ名. </summary>
            public string FolderName { get { return folderName; } }
            /// <summary> インポート元の相対パス. </summary>
            public string ImportPath { get { return UnityPathUtility.RelativePathToFullPath(importPath); } }
        }

        //----- field -----

        //---------------------------------------
        // Sound.
        //---------------------------------------

        #if ENABLE_CRIWARE_ADX

        [SerializeField]
        private AssetImportInfo soundImportInfo = null;
        [SerializeField]
        private string acfAssetSourceFullPath = null;
        [SerializeField]
        private string acfAssetExportPath = null;

        #endif

        //---------------------------------------
        // Movie.
        //---------------------------------------

        #if ENABLE_CRIWARE_SOFDEC

        [SerializeField]
        private AssetImportInfo movieImportInfo = null;

        #endif

        //----- property -----

        #if ENABLE_CRIWARE_ADX

        public AssetImportInfo SoundImportInfo { get { return soundImportInfo; } }

        public string AcfAssetSourceFullPath { get { return UnityPathUtility.RelativePathToFullPath(acfAssetSourceFullPath); } }

        public string AcfAssetExportPath { get { return UnityPathUtility.RelativePathToFullPath(acfAssetExportPath); } }        

        #endif

        #if ENABLE_CRIWARE_SOFDEC

        public AssetImportInfo MovieImportInfo { get { return movieImportInfo; } }

        #endif

        //----- method -----
    }
}

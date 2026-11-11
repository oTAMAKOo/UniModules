
using UnityEngine;
using System;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.CriWare.Editor
{
    [Serializable]
    public sealed class ImportInfo
    {
        /// <summary> Assetsフォルダからの相対パス </summary>
        [SerializeField]
        public string sourceFolderRelativePath = null;
        [SerializeField]
        public string destFolderGuid = null;
    }

    public sealed class CriAssetConfig : SingletonScriptableObject<CriAssetConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string scriptNamespace = null;

        //---------------------------------------
        // Sound.
        //---------------------------------------

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE
        
        [SerializeField]
        private string soundFolderName = null;
        [SerializeField]
        private ImportInfo internalSound = null;
        [SerializeField]
        private ImportInfo externalSound = null;
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
        private string movieFolderName = null;
        [SerializeField]
        private ImportInfo internalMovie = null;
        [SerializeField]
        private ImportInfo externalMovie = null;

        #endif

        //----- property -----

        public string ScriptNamespace { get { return scriptNamespace; } }

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

        public string AcfAssetSourceFullPath
        {
            get 
            {   
                return string.IsNullOrEmpty(acfAssetSourceFullPath) ? string.Empty : UnityPathUtility.RelativePathToFullPath(acfAssetSourceFullPath); 
            }
        }

        public string AcfAssetExportPath
        {
            get
            {
                return string.IsNullOrEmpty(acfAssetExportPath) ? string.Empty : UnityPathUtility.RelativePathToFullPath(acfAssetExportPath);
            }
        }

        public string SoundFolderName { get { return soundFolderName; } }
        
        public ImportInfo InternalSound { get { return internalSound; } }

        public ImportInfo ExternalSound { get { return externalSound; } }

        #endif

        #if ENABLE_CRIWARE_SOFDEC

        public string MovieFolderName { get { return movieFolderName; } }

        public ImportInfo InternalMovie { get { return internalMovie; } }

        public ImportInfo ExternalMovie { get { return externalMovie; } }

        #endif

        //----- method -----
    }
}

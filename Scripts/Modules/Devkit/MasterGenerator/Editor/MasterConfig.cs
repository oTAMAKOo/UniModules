
using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Master
{
    public sealed class MasterConfig : ReloadableScriptableObject<MasterConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private SerializationFileUtility.Format format = SerializationFileUtility.Format.Yaml;
        [SerializeField]
        private string sourceDirectory = null;
        [SerializeField]
        private string exportDirectory = null;
        [SerializeField]
        private string dataCryptKey = null;
        [SerializeField]
        private string dataCryptIv = null;
        [SerializeField]
        private string fileNameCryptKey = null;
        [SerializeField]
        private string fileNameCryptIv = null;
        [SerializeField]
        private bool lz4Compression = true;

        //----- property -----

        public SerializationFileUtility.Format DataFormat { get { return format; } }

        public string SourceDirectory
        {
            get
            {
                return string.IsNullOrEmpty(sourceDirectory) ? null : UnityPathUtility.RelativePathToFullPath(sourceDirectory);
            }
        }

        public string ExportDirectory
        {
            get
            {
                return string.IsNullOrEmpty(exportDirectory) ? null : UnityPathUtility.RelativePathToFullPath(exportDirectory);
            }
        }

        public string DataCryptKey { get { return dataCryptKey; } }

        public string DataCryptIv { get { return dataCryptIv; } }

        public string FileNameCryptKey { get { return fileNameCryptKey; } }

        public string FileNameCryptIv { get { return fileNameCryptIv; } }

        public bool Lz4Compression { get { return lz4Compression; } }

        //----- method -----

    }
}


using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Master
{
    public sealed class MasterConfig : SingletonScriptableObject<MasterConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string sourceDirectory = null;
        [SerializeField]
        private string exportDirectory = null;
        [SerializeField]
        private string cryptoKey = null;
        [SerializeField]
        private string cryptoIv = null;
        [SerializeField]
        private bool lz4Compression = true;

        //----- property -----

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

        public string CryptoKey { get { return cryptoKey; } }

        public string CryptoIv { get { return cryptoIv; } }

        public bool Lz4Compression { get { return lz4Compression; } }

        //----- method -----

    }
}

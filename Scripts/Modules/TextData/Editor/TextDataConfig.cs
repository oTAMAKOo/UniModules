
using Extensions;
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.TextData.Editor
{
    public sealed partial class TextDataConfig : ReloadableScriptableObject<TextDataConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private FileLoader.Format fileFormat = FileLoader.Format.Yaml;
        
        [SerializeField]
        private EmbeddedSetting embedded = null;
        [SerializeField]
        private DistributionSetting distribution = null;

        [Header("Crypto")]

        [SerializeField, Tooltip("32文字")]
        private string cryptoKey = null;
        [SerializeField, Tooltip("16文字")]
        private string cryptoIv = null;

        #pragma warning disable 414

        [Header("Windows")]

        [SerializeField]
        private string windowsConverterPath = null;

        [Header("Mac")]

        [SerializeField]
        private string osxConverterPath = null;

        #pragma warning restore 414

        //----- property -----

        /// <summary> ファイルフォーマット </summary>
        public FileLoader.Format FileFormat { get { return fileFormat; } }

        /// <summary> 暗号化Key(32文字) </summary>
        public string CryptoKey { get { return cryptoKey; } }
        /// <summary> 暗号化Iv (16文字)</summary>
        public string CryptoIv { get { return cryptoIv; } }

        /// <summary> 内蔵設定 </summary>
        public EmbeddedSetting Embedded { get { return embedded; } }
        /// <summary> 配信設定 </summary>
        public DistributionSetting Distribution { get { return distribution; } }

        public string ConverterPath
        {
            get
            {
                var converterPath = string.Empty;

                #if UNITY_EDITOR_WIN

                converterPath = windowsConverterPath;

                #endif

                #if UNITY_EDITOR_OSX

                converterPath = osxConverterPath;

                #endif
                
                return string.IsNullOrEmpty(converterPath) ? null : UnityPathUtility.RelativePathToFullPath(converterPath);
            }
        }

        //----- method -----
    }
}


using System.Linq;
using Extensions;
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.TextData.Editor
{
    public sealed partial class TextDataConfig : SingletonScriptableObject<TextDataConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private FileLoader.Format fileFormat = FileLoader.Format.Yaml;
        
        [SerializeField]
        private InternalSetting internalSetting = null;
        [SerializeField]
        private ExternalSetting externalSetting = null;

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

        /// <summary> 内蔵テキスト設定 </summary>
        public InternalSetting Internal { get { return internalSetting; } }
        /// <summary> 外部テキスト設定 </summary>
        public ExternalSetting External { get { return externalSetting; } }

        /// <summary> 外部テキスト有効か </summary>
        public bool EnableExternal
        {
            get { return externalSetting != null && externalSetting.Source.Any(); }
        }

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


using Extensions;
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.GameText.Editor
{
    public sealed partial class GameTextConfig : ReloadableScriptableObject<GameTextConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private FileLoader.Format fileFormat = FileLoader.Format.Yaml;
        
        [SerializeField]
        private EmbeddedSetting embedded = null;
        [SerializeField]
        private DistributionSetting distribution = null;

        #pragma warning disable 414

        [Header("Windows")]

        [SerializeField]
        private string windowsConverterPath = null;

        [Header("Mac")]

        [SerializeField]
        private string osxConverterPath = null;

        #pragma warning restore 414

        //----- property -----

        public FileLoader.Format FileFormat { get { return fileFormat; } }
        
        public EmbeddedSetting Embedded { get { return embedded; } }

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

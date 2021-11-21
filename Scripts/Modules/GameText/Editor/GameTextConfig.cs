
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
        private string windowsImporterFileName = null;
        [SerializeField]
        private string windowsExporterFileName = null;

        [Header("Mac")]

        [SerializeField]
        private string osxImporterFileName = null;
        [SerializeField]
        private string osxExporterFileName = null;

        #pragma warning restore 414

        //----- property -----

        public FileLoader.Format FileFormat { get { return fileFormat; } }
        
        public EmbeddedSetting Embedded { get { return embedded; } }

        public DistributionSetting Distribution { get { return distribution; } }

        //----- method -----
    }
}

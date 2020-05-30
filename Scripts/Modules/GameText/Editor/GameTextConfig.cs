
using System;
using UnityEngine;
using UnityEditor;
using Extensions;
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
        private BuiltInGameTextSetting builtInGameTextSetting = null;
        [SerializeField]
        private UpdateGameTextSetting updateGameTextSetting = null;
        [SerializeField]
        private ExtendGameTextSetting extendGameTextSetting = null;

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
        
        public BuiltInGameTextSetting BuiltInGameText { get { return builtInGameTextSetting; } }

        public UpdateGameTextSetting UpdateGameText { get { return updateGameTextSetting; } }

        public ExtendGameTextSetting ExtendGameText { get { return extendGameTextSetting; } }

        //----- method -----
    }
}

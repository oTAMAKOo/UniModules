
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.GameText.Editor
{
    public enum GenerateMode
    {
        [Label("Asset+Script")]
        FullGenerate,
        [Label("Asset")]
        OnlyAsset,
    }

    public sealed class GameTextConfig : ReloadableScriptableObject<GameTextConfig>
    {
        //----- params -----

        [Serializable]
        public sealed class AseetGenerateSetting
        {
            [SerializeField]
            private string label = null;
            [SerializeField]
            private UnityEngine.Object assetFolder = null;
            [SerializeField]
            private GenerateMode defaultMode = GenerateMode.FullGenerate;

            public string Label { get { return label; } }

            public string AssetFolderPath { get { return AssetDatabase.GetAssetPath(assetFolder); } }

            public GenerateMode DefaultMode { get { return defaultMode; } }
        }

        /// <summary> データフォルダ名 </summary>
        public const string ContentsFolderName = "Contents";

        /// <summary> Json拡張子 </summary>
        private const string JsonFileExtension = ".json";

        /// <summary> Yaml拡張子 </summary>
        private const string YamlFileExtension = ".yaml";

        //----- field -----

        [SerializeField]
        private FileLoader.Format fileFormat = FileLoader.Format.Yaml;
        [SerializeField]
        private string workspaceFolder = string.Empty;
        [SerializeField]
        private string excelFileName = string.Empty;
        [SerializeField]
        private UnityEngine.Object scriptFolder = null;
        [SerializeField]
        private AseetGenerateSetting[] aseetGenerateSettings = null;

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
        
        public string ScriptFolderPath
        {
            get { return AssetDatabase.GetAssetPath(scriptFolder); }
        }

        public IReadOnlyList<AseetGenerateSetting> AseetGenerateSettings
        {
            get { return aseetGenerateSettings; }
        }

        //----- method -----

        public string GetGameTextWorkspacePath()
        {
            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, workspaceFolder);

            return workspacePath;
        }
        
        public string GetFileExtension()
        {
            var extension = string.Empty;

            switch (fileFormat)
            {
                case FileLoader.Format.Yaml:
                    extension = YamlFileExtension;
                    break;
                    
                case FileLoader.Format.Json:
                    extension = JsonFileExtension;
                    break;
            }

            return extension;
        }

        public string GetExcelPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            return PathUtility.Combine(workspacePath, excelFileName);
        }

        public string GetContentsFolderPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            return PathUtility.Combine(workspacePath, ContentsFolderName);
        }

        public string GetImporterPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            var fileName = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = windowsImporterFileName;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = osxImporterFileName;

            #endif

            if (string.IsNullOrEmpty(fileName)) { return null; }

            return PathUtility.Combine(workspacePath, fileName);
        }

        public string GetExporterPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            var fileName = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = windowsExporterFileName;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = osxExporterFileName;

            #endif

            if (string.IsNullOrEmpty(fileName)) { return null; }

            return PathUtility.Combine(workspacePath, fileName);
        }
    }
}

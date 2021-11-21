
using UnityEngine;
using UnityEditor;
using System;
using Extensions;

namespace Modules.GameText.Editor
{
    public sealed partial class GameTextConfig
    {
        [Serializable]
        public abstract class GenerateAssetSetting
        {
            /// <summary> データフォルダ名 </summary>
            public const string ContentsFolderName = "Contents";

            [SerializeField]
            private UnityEngine.Object aseetFolder = null;
            [SerializeField]
            private string excelFileName = string.Empty;
            [SerializeField]
            private string workspaceFolder = string.Empty;

            public string AseetFolderPath
            {
                get { return aseetFolder != null ? AssetDatabase.GetAssetPath(aseetFolder) : null; }
            }

            public string GetGameTextWorkspacePath()
            {
                if (string.IsNullOrEmpty(workspaceFolder)) { return null; }

                var projectFolder = UnityPathUtility.GetProjectFolderPath();

                var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, workspaceFolder);

                return workspacePath;
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
                var config = GameTextConfig.Instance;

                var workspacePath = GetGameTextWorkspacePath();

                var fileName = string.Empty;

                #if UNITY_EDITOR_WIN

                fileName = config.windowsImporterFileName;

                #endif

                #if UNITY_EDITOR_OSX

                fileName = config.osxImporterFileName;

                #endif

                if (string.IsNullOrEmpty(fileName)) { return null; }

                return PathUtility.Combine(workspacePath, fileName);
            }

            public string GetExporterPath()
            {
                var config = GameTextConfig.Instance;

                var workspacePath = GetGameTextWorkspacePath();

                var fileName = string.Empty;

                #if UNITY_EDITOR_WIN

                fileName = config.windowsExporterFileName;

                #endif

                #if UNITY_EDITOR_OSX

                fileName = config.osxExporterFileName;

                #endif

                if (string.IsNullOrEmpty(fileName)) { return null; }

                return PathUtility.Combine(workspacePath, fileName);
            }
        }

        [Serializable]
        public sealed class EmbeddedSetting : GenerateAssetSetting
        {
            [SerializeField]
            private UnityEngine.Object scriptFolder = null;

            public string ScriptFolderPath
            {
                get { return scriptFolder != null ? AssetDatabase.GetAssetPath(scriptFolder) : null; }
            }
        }

        [Serializable]
        public sealed class DistributionSetting : GenerateAssetSetting
        {
            [SerializeField]
            private bool enable = false;

            public bool Enable { get { return enable; } }
        }
    }
}

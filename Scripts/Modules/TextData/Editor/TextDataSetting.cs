
using UnityEngine;
using UnityEditor;
using System;
using Extensions;

namespace Modules.TextData.Editor
{
    public sealed partial class TextDataConfig
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

            public string GetTextDataWorkspacePath()
            {
                if (string.IsNullOrEmpty(workspaceFolder)) { return null; }

                var projectFolder = UnityPathUtility.GetProjectFolderPath();

                var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, workspaceFolder);

                return workspacePath;
            }

            public string GetExcelPath()
            {
                var workspacePath = GetTextDataWorkspacePath();

                return PathUtility.Combine(workspacePath, excelFileName);
            }

            public string GetContentsFolderPath()
            {
                var workspacePath = GetTextDataWorkspacePath();

                return PathUtility.Combine(workspacePath, ContentsFolderName);
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

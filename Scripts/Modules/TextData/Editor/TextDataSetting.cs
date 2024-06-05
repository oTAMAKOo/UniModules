
using UnityEngine;
using UnityEditor;
using System;
using Extensions;

namespace Modules.TextData.Editor
{
    [Serializable]
    public sealed class TextDataSource
    {
        /// <summary> データフォルダ名 </summary>
        public const string ContentsFolderName = "Contents";

        [SerializeField]
        private string excelFileName = string.Empty;
        [SerializeField]
        private string workspaceFolder = string.Empty;
        [SerializeField]
        private string displayName = string.Empty;

        public string DisplayName { get { return displayName; } }

        public string GetWorkspacePath()
        {
            if (string.IsNullOrEmpty(workspaceFolder)) { return null; }

            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, workspaceFolder);

            return workspacePath;
        }

        public string GetExcelPath()
        {
            var workspacePath = GetWorkspacePath();

            return PathUtility.Combine(workspacePath, excelFileName);
        }

        public string GetContentsFolderPath()
        {
            var workspacePath = GetWorkspacePath();

            return PathUtility.Combine(workspacePath, ContentsFolderName);
        }
    }

    public sealed partial class TextDataConfig
    {
        [Serializable]
        public sealed class InternalSetting
        {
            [SerializeField]
            private UnityEngine.Object aseetFolder = null;
            [SerializeField]
            private UnityEngine.Object scriptFolder = null;
            [SerializeField]
            private TextDataSource[] source  = null;

            public string AseetFolderPath
            {
                get { return aseetFolder != null ? AssetDatabase.GetAssetPath(aseetFolder) : null; }
            }

            public string ScriptFolderPath
            {
                get { return scriptFolder != null ? AssetDatabase.GetAssetPath(scriptFolder) : null; }
            }

            public TextDataSource[] Source { get { return source; } }
        }

        [Serializable]
        public sealed class ExternalSetting
        {
            [SerializeField]
            private bool enable = false;
            [SerializeField]
            private UnityEngine.Object aseetFolder = null;
            [SerializeField]
            private TextDataSource[] source  = null;

            public bool Enable { get { return enable; } }

            public string AseetFolderPath
            {
                get { return aseetFolder != null ? AssetDatabase.GetAssetPath(aseetFolder) : null; }
            }

            public TextDataSource[] Source { get { return source; } }
        }
    }
}

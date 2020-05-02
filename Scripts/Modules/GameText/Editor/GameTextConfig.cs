
using UnityEngine;
using UnityEditor;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.GameText.Editor
{
	public sealed class GameTextConfig : ReloadableScriptableObject<GameTextConfig>
    {
        //----- params -----

        /// <summary> レコード格納フォルダ名 </summary>
        public const string RecordFolderName = "Records";

        /// <summary> シートファイル拡張子 </summary>
        public const string SheetFileExtension = ".sheet";

        /// <summary> レコードファイル拡張子 </summary>
        public const string RecordFileExtension = ".record";

        //----- field -----

        [SerializeField]
        private FileSystem.Format fileFormat = FileSystem.Format.Yaml;
        [SerializeField]
        private string workspaceFolder = string.Empty;
        [SerializeField]
        private string excelFileName = string.Empty;
        [SerializeField]
        private UnityEngine.Object tableScriptFolder = null;
        [SerializeField]
        private UnityEngine.Object enumScriptFolder = null;
        [SerializeField]
        private UnityEngine.Object scriptableObjectFolder = null;

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

        public FileSystem.Format FileFormat { get { return fileFormat; } }
        
        public string TableScriptFolderPath { get { return AssetDatabase.GetAssetPath(tableScriptFolder); } }

        public string EnumScriptFolderPath { get { return AssetDatabase.GetAssetPath(enumScriptFolder); } }

        public string ScriptableObjectFolderPath { get { return AssetDatabase.GetAssetPath(scriptableObjectFolder); } }

        //----- method -----

        public string GetGameTextWorkspacePath()
        {
            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, workspaceFolder);

            return workspacePath;
        }

        public string GetSheetFileExtension() { return SheetFileExtension; }

        public string GetRecordFileExtension() { return RecordFileExtension; }

        public string GetExcelPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            return PathUtility.Combine(workspacePath, excelFileName);
        }

        public string GetRecordFolderPath()
        {
            var workspacePath = GetGameTextWorkspacePath();

            return PathUtility.Combine(workspacePath, RecordFolderName);
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

            fileName = oscExporterFileName;

            #endif

            if (string.IsNullOrEmpty(fileName)) { return null; }

            return PathUtility.Combine(workspacePath, fileName);
        }
    }
}

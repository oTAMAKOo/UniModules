
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;


namespace Modules.BehaviorControl
{
    public sealed class BehaviorControlSetting : SingletonScriptableObject<BehaviorControlSetting>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string importFolderPath = null;
        [SerializeField]
        private Object exportFolder = null;
        [SerializeField]
        private FileLoader.Format format = FileLoader.Format.Yaml;

        //----- property -----

        public FileLoader.Format Format { get { return format; } }

        //----- method -----

        public string GetExportFolderPath()
        {
            return exportFolder != null ? AssetDatabase.GetAssetPath(exportFolder) : null;
        }

        public string GetImportFolderPath()
        {
            if (string.IsNullOrEmpty(importFolderPath)) { return null; }

            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var workspacePath = PathUtility.RelativePathToFullPath(projectFolder, importFolderPath);

            return workspacePath;
        }
    }
}

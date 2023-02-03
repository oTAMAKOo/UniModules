
#if ENABLE_UTAGE

using Extensions;
using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;
using Utage;

namespace Modules.UtageExtension
{
    public sealed class UtageBuildConfig : ReloadableScriptableObject<UtageBuildConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object excelFolder = null;
        [SerializeField]
        private Object exportFolder = null;
        [SerializeField]
        private AdvScenarioDataProject scenarioProjectTemplate = null;
        [SerializeField]
        private AdvImportScenarios importScenarioTemplate = null;

        //----- property -----

        public string ExcelFolderAssetPath
        {
            get { return excelFolder != null ? PathUtility.ConvertPathSeparator(AssetDatabase.GetAssetPath(excelFolder)) : null; }
        }

        public string ExportFolderAssetPath
        {
            get { return exportFolder != null ? PathUtility.ConvertPathSeparator(AssetDatabase.GetAssetPath(exportFolder)) : null; }
        }

        public AdvScenarioDataProject ScenarioProjectTemplate
        {
            get { return scenarioProjectTemplate; }
        }

        public AdvImportScenarios ImportScenarioTemplate
        {
            get { return importScenarioTemplate; }
        }

        //----- method -----
    }
}

#endif


#if ENABLE_UTAGE

using Extensions;
using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;
using Utage;

namespace Modules.UtageExtension
{
    public sealed class UtageBuildConfig : SingletonScriptableObject<UtageBuildConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object targetFolder = null;
        [SerializeField]
        private AdvScenarioDataProject scenarioProjectTemplate = null;
        [SerializeField]
        private AdvImportScenarios importScenarioTemplate = null;

        //----- property -----

        public string TargetFolderAssetPath
        {
            get { return targetFolder != null ? PathUtility.ConvertPathSeparator(AssetDatabase.GetAssetPath(targetFolder)) : null; }
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

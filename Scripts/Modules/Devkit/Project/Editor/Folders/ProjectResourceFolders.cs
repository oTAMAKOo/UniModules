using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Project
{
    public sealed class ProjectResourceFolders : ReloadableScriptableObject<ProjectResourceFolders>
    {
        //----- params -----

        //----- field -----

		[SerializeField]
        private Object internalResourcesFolder = null;
        [SerializeField]
        private Object externalAssetFolder = null;
        [SerializeField]
        private Object shareResourcesFolder = null;

        //----- property -----

		public string InternalResourcesPath { get { return AssetDatabase.GetAssetPath(internalResourcesFolder); } }
        public string ShareResourcesPath { get { return AssetDatabase.GetAssetPath(shareResourcesFolder); } }
        public string ExternalAssetPath { get { return AssetDatabase.GetAssetPath(externalAssetFolder); } }

        //----- method -----
    }
}

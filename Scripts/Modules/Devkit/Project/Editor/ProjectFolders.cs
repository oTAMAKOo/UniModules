using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Project
{
    public sealed class ProjectFolders : ReloadableScriptableObject<ProjectFolders>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object scriptFolder = null;
        [SerializeField]
        private Object scriptConstantsFolder = null;
        [SerializeField]
        private Object editorScriptFolder = null;
        [SerializeField]
        private Object resourcesFolder = null;
        [SerializeField]
        private Object internalResourcesFolder = null;
        [SerializeField]
        private Object externalResourcesFolder = null;
        [SerializeField]
        private Object shareResourcesFolder = null;
        [SerializeField]
        private Object streamingAssetFolder = null;

        //----- property -----

        public string ScriptPath { get { return AssetDatabase.GetAssetPath(scriptFolder); } }
        public string ConstantsScriptPath { get { return AssetDatabase.GetAssetPath(scriptConstantsFolder); } }
        public string EditorScriptPath { get { return AssetDatabase.GetAssetPath(editorScriptFolder); } }
        public string ResourcesPath { get { return AssetDatabase.GetAssetPath(resourcesFolder); } }
        public string InternalResourcesPath { get { return AssetDatabase.GetAssetPath(internalResourcesFolder); } }
        public string ShareResourcesFolder { get { return AssetDatabase.GetAssetPath(shareResourcesFolder); } }
        public string ExternalResourcesPath { get { return AssetDatabase.GetAssetPath(externalResourcesFolder); } }
        public string StreamingAssetPath { get { return AssetDatabase.GetAssetPath(streamingAssetFolder); } }

        //----- method -----
    }
}

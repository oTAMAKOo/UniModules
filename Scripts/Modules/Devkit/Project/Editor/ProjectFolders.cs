
using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Project
{
    public class ProjectFolders : ReloadableScriptableObject<ProjectFolders>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object scriptFolder = null;
        [SerializeField]
        private Object clientScriptFolder = null;
        [SerializeField]
        private Object editorScriptFolder = null;
        [SerializeField]
        private Object resourcesFolder = null;
        [SerializeField]
        private Object internalResourcesFolder = null;
        [SerializeField]
        private Object externalResourcesFolder = null;
        [SerializeField]
        private Object streamingAssetFolder = null;

        //----- property -----

        public string ScriptPath { get { return AssetDatabase.GetAssetPath(scriptFolder); } }
        public string ClientScriptFolder { get { return AssetDatabase.GetAssetPath(clientScriptFolder); } }
        public string EditorScriptPath { get { return AssetDatabase.GetAssetPath(editorScriptFolder); } }
        public string ResourcesPath { get { return AssetDatabase.GetAssetPath(resourcesFolder); } }
        public string InternalResourcesPath { get { return AssetDatabase.GetAssetPath(internalResourcesFolder); } }
        public string ExternalResourcesPath { get { return AssetDatabase.GetAssetPath(externalResourcesFolder); } }
        public string StreamingAssetPath { get { return AssetDatabase.GetAssetPath(streamingAssetFolder); } }

        //----- method -----
    }
}
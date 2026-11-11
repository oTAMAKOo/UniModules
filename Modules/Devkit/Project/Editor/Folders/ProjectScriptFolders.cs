
using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Project
{
	public sealed class ProjectScriptFolders : SingletonScriptableObject<ProjectScriptFolders>
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
        private string scriptConstantsNamespace = null;

		//----- property -----
		
		public string ScriptPath { get { return AssetDatabase.GetAssetPath(scriptFolder); } }

		public string ConstantsScriptPath { get { return AssetDatabase.GetAssetPath(scriptConstantsFolder); } }

        public string EditorScriptPath { get { return AssetDatabase.GetAssetPath(editorScriptFolder); } }
        
        public string ScriptConstantsNamespace { get { return scriptConstantsNamespace; } }

		//----- method -----
	}
}

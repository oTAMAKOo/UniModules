
using UnityEngine;
using UnityEditor;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Project
{
	public sealed class ProjectUnityFolders : ReloadableScriptableObject<ProjectUnityFolders>
	{
		//----- params -----

		//----- field -----

		[SerializeField]
		private Object resourcesFolder = null;
		[SerializeField]
		private Object streamingAssetFolder = null;

		//----- property -----

		public string ResourcesPath { get { return AssetDatabase.GetAssetPath(resourcesFolder); } }
		public string StreamingAssetPath { get { return AssetDatabase.GetAssetPath(streamingAssetFolder); } }

		//----- method -----
	}
}

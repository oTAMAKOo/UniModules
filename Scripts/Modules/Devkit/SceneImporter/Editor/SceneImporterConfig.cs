
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.SceneImporter
{
    public sealed class SceneImporterConfig : SingletonScriptableObject<SceneImporterConfig>
    {
        //----- params -----

        public const string SceneFileExtension = ".unity";

        //----- field -----

        [SerializeField]
        private Object initialScene = null;
        [SerializeField]
        private Object[] managedFolders = new Object[0];

        //----- property -----

		public string GetInitialScenePath()
		{
			if (initialScene == null){ return null; }

			var assetPath = AssetDatabase.GetAssetPath(initialScene);

			return PathUtility.ConvertPathSeparator(assetPath);
		}
		
		public string[] GetManagedFolderPaths()
		{
			var assetPaths = managedFolders.Where(x => x != null)
				.Select(x => AssetDatabase.GetAssetPath(x))
				.Select(x => PathUtility.ConvertPathSeparator(x))
				.Select(x => x.EndsWith(PathUtility.PathSeparator.ToString()) ? x : x + PathUtility.PathSeparator)
				.ToArray();

			return assetPaths;
		}
	}
}

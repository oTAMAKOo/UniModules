
using UnityEngine;
using System.Collections.Generic;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.SceneImporter
{
    public sealed class SceneImporterConfig : ReloadableScriptableObject<SceneImporterConfig>
    {
        //----- params -----

        public const string SceneFileExtension = ".unity";

        //----- field -----

        [SerializeField]
        private string initialScene = null;
        [SerializeField]
        private List<string> managedFolders = new List<string>();

        //----- property -----

        public string InitialScene { get { return initialScene; } }
        public string[] ManagedFolders { get { return managedFolders.ToArray(); } }
    }
}

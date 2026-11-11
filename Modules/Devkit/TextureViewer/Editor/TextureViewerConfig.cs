
using UnityEngine;
using UnityEditor;
using System.Linq;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.TextureViewer
{
    public sealed class TextureViewerConfig : SingletonScriptableObject<TextureViewerConfig>
    {
        //----- params -----

        //----- field -----

        [Header("Ignore")]

        [SerializeField]
        private Object[] ignoreFolders = null;
        [SerializeField]
        private string[] ignoreFolderNames = null;

        //----- property -----

        public string[] IgnoreFolderPaths
        {
            get
            {
                if (ignoreFolders == null){ return new string[0]; }

                return ignoreFolders
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .Where(x => AssetDatabase.IsValidFolder(x))
                    .ToArray();
            }
        }

        public string[] IgnoreFolderNames
        {
            get { return ignoreFolderNames ?? (ignoreFolderNames = new string[0]); }
        }

        //----- method -----
    }
}
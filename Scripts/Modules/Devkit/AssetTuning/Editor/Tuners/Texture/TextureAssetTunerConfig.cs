
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureAssetTunerConfig : ReloadableScriptableObject<TextureAssetTunerConfig>
    {
        //----- params -----

        //----- field -----

        // compress

        [SerializeField]
        private Object[] compressFolders = null;
        [SerializeField]
        private string[] ignoreCompressFolderNames = null;

        // sprite

        [SerializeField]
        private Object[] spriteFolders = null;
        [SerializeField]
        private string[] spriteFolderNames = null;
        [SerializeField]
        private string[] ignoreSpriteFolderNames = null;

        //----- property -----

        public Object[] CompressFolders
        {
            get { return compressFolders ?? (compressFolders = new Object[0]); }
        }

        public string[] IgnoreCompressFolderNames
        {
            get { return ignoreCompressFolderNames ?? (ignoreCompressFolderNames = new string[0]); }
        }

        public Object[] SpriteFolders
        {
            get { return spriteFolders ?? (spriteFolders = new Object[0]); }
        }

        public string[] IgnoreSpriteFolderNames
        {
            get { return ignoreSpriteFolderNames ?? (ignoreSpriteFolderNames = new string[0]); }
        }

        public string[] SpriteFolderNames
        {
            get { return spriteFolderNames ?? (spriteFolderNames = new string[0]); }
        }

        //----- method -----
    }
}

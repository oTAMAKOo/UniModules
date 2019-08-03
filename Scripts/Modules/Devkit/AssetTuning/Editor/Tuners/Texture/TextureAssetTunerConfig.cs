
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureAssetTunerConfig : ReloadableScriptableObject<TextureAssetTunerConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object[] compressFolders = null;
        [SerializeField]
        private Object[] spriteFolders = null;

        //----- property -----

        public Object[] CompressFolders
        {
            get { return compressFolders ?? (compressFolders = new Object[0]); }
        }

        public Object[] SpriteFolders
        {
            get { return spriteFolders ?? (spriteFolders = new Object[0]); }
        }

        //----- method -----
    }
}

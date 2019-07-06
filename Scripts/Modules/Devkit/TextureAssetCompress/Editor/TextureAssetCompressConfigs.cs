
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.TextureAssetCompress
{
    public sealed class TextureAssetCompressConfigs : ReloadableScriptableObject<TextureAssetCompressConfigs>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object[] compressFolders = null;

        //----- property -----

        public Object[] CompressFolders
        {
            get { return compressFolders ?? (compressFolders = new Object[0]); }
        }

        //----- method -----
    }
}

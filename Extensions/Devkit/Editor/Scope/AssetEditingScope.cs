
using UnityEditor;

namespace Extensions.Devkit
{
    public sealed class AssetEditingScope : Scope
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        //----- method -----

        public AssetEditingScope()
        {
            AssetDatabase.StartAssetEditing();
        }

        protected override void CloseScope()
        {
            AssetDatabase.StopAssetEditing();
        }
    }
}

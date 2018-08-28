
using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.CriWare.Editor
{
    public class CriAssetConfig<TInstance> : ReloadableScriptableObject<TInstance> where TInstance : ReloadableScriptableObject<TInstance>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string rootFolderName = null;   // インポート時のルートフォルダ名.
        [SerializeField]
        private string criExportDir = null;     // Criのアセット出力ディレクトリ(相対パス).

        //----- property -----

        public string RootFolderName { get { return rootFolderName; } }
        public string CriExportDir { get { return UnityPathUtility.RelativePathToFullPath(criExportDir); } }

        //----- method -----
    }
}

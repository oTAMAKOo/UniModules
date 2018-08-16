
using UnityEngine;
using UnityEditor;
using Extensions.Serialize;
using Modules.Devkit.ScriptableObjects;

namespace Modules.LocalMaster
{
    public class LocalMasterConfig : ReloadableScriptableObject<LocalMasterConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object assetFolder = null;
        [SerializeField]
        private string masterNameAddress = null;
        [SerializeField]
        private string dataStartAddress = null;

        //----- property -----

        public string AssetFolderPath { get { return AssetDatabase.GetAssetPath(assetFolder); } }
        public string MasterNameAddress { get { return masterNameAddress; } }
        public string DataStartAddress { get { return dataStartAddress; } }

        //----- method -----
    }
}

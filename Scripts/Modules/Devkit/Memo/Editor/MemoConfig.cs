
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Memo
{
    public sealed class MemoConfig : ReloadableScriptableObject<MemoConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string aesKey = null;
        [SerializeField]
        private string aesIv = null;

        //----- property -----

        public string AESKey { get { return aesKey; } }
        
        public string AESIv { get { return aesIv; } }

        //----- method -----
    }
}

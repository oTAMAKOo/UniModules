
using UnityEngine;

namespace Modules.Devkit.Memo
{
    [DisallowMultipleComponent]
    public sealed class Memo : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        #if UNITY_EDITOR

        [SerializeField]
        private string memo = null;

        #endif

        //----- property -----

        //----- method -----
    }
}

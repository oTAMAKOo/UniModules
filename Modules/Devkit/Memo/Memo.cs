
using UnityEngine;

namespace Modules.Devkit.Memo
{
    [DisallowMultipleComponent]
    public sealed class Memo : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        #if UNITY_EDITOR

        #pragma warning disable 0414

        [SerializeField]
        private string memo = null;

        #pragma warning restore 0414

        #endif

        //----- property -----

        //----- method -----
    }
}

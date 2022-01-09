
using UnityEngine;
using System;
using System.Collections.Generic;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.DefineSymbol
{
    public sealed class DefineSymbolConfig : ReloadableScriptableObject<DefineSymbolConfig>
    {
        //----- params -----

        [Serializable]
        public sealed class DefineSymbolInfo
        {
            public string symbol = null;
            public string description = null;
        }

        //----- field -----

        [SerializeField]
        private DefineSymbolInfo[] infos = null;

        //----- property -----

        public IReadOnlyList<DefineSymbolInfo> Infos
        {
            get { return infos ?? (infos = new DefineSymbolInfo[0]); }
        }

        //----- method -----
    }
}
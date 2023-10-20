
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Console
{
    public sealed class UnityConsoleConfig : SingletonScriptableObject<UnityConsoleConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private ConsoleInfo[] definedInfos = null;

        //----- property -----

        //----- method -----

        public List<ConsoleInfo> GetDefinedInfos()
        {
            return definedInfos.ToList();
        }
    }
}

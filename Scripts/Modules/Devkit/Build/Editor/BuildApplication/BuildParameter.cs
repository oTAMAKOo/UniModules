
using UnityEditor;
using System;

namespace Modules.Devkit.Build
{
    [Serializable]
    public abstract class BuildParameter
    {
        public BuildTarget buildTarget = BuildTarget.NoTarget;

        public BuildOptions buildOptions = BuildOptions.None;

        /// <summary> デバッグビルドか. </summary>
        public bool development = false;

        /// <summary> Defineシンボル. </summary>
        public string[] defineSymbols = null;
    }
}

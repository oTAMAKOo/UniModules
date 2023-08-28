
using UnityEditor;
using System;
using System.Collections.Generic;
using Extensions;

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
        public List<string> defineSymbols = null;

        /// <summary> ビルド番号 </summary>
        public int buildNumber = 1;

        /// <summary> ブランチ名 </summary>
        public string branchName = null;

        /// <summary> コマンドライン引数適用 </summary>
        public virtual void ApplyCommandLineArguments()
        {
            buildNumber = CommandLineUtility.Get<int>("-BuildNumber");
            branchName = CommandLineUtility.Get<string>("-BranchName");
        }
    }
}

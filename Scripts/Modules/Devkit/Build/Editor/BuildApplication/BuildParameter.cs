
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

		/// <summary> ビルド番号 </summary>
		public int buildNumber = 0;

		/// <summary> コマンドライン引数適用 </summary>
		public virtual void ApplyCommandLineArguments()
		{
			var args = System.Environment.GetCommandLineArgs();
			
			for(var i = 0; i < args.Length; i++)
			{
				switch(args[i])
				{
					case "-BuildNumber":
						{
							buildNumber = int.Parse(args[i+1]);
							i++;
						}
						break;
				}
			}
		}
    }
}


using UnityEngine;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
    public sealed class MessagePackConfig : ReloadableScriptableObject<MessagePackConfig>
    {
        //----- params -----
        
        private const string DefaultDotNetPath = "/usr/local/share/dotnet/dotnet";

        private const string DefaultMpcPath = "$HOME/.dotnet/tools/mpc";

        private const string DefaultMsBuildPath = "/Library/Frameworks/Mono.framework/Versions/Current/bin";

        public static class Prefs
        {
            public static string DotnetPath
            {
                get { return ProjectPrefs.GetString("MessagePackConfigPrefs-dotnetPath", DefaultDotNetPath); }
                set { ProjectPrefs.SetString("MessagePackConfigPrefs-dotnetPath", value); }
            }

            public static string MpcPath
            {
                get { return ProjectPrefs.GetString("MessagePackConfigPrefs-mpcPath", DefaultMpcPath); }
                set { ProjectPrefs.SetString("MessagePackConfigPrefs-mpcPath", value); }
            }

            public static string MsbuildPath
            {
                get { return ProjectPrefs.GetString("MessagePackConfigPrefs-msbuildPath", DefaultMsBuildPath); }
                set { ProjectPrefs.SetString("MessagePackConfigPrefs-msbuildPath", value); }
            }
        }

        //----- field -----

        [SerializeField]
        private string scriptExportAssetDir = null;
        [SerializeField]
        private string scriptName = null;
        [SerializeField]
        private bool useMapMode = true;
        [SerializeField]
        [Tooltip("(option) Generated resolver class namespace.")]
        private string resolverNameSpace = "MessagePack";
        [SerializeField]
        [Tooltip("(option) Generated resolver class name.")]
        private string resolverName = "GeneratedResolver";
        [SerializeField]
        [Tooltip("(option) Split with ','.")]
        private string conditionalCompilerSymbols = null;

        //----- property -----

        /// <summary> スクリプト出力先. </summary>
        public string ScriptExportDir
        {
            get { return UnityPathUtility.ConvertAssetPathToFullPath(scriptExportAssetDir); }
        }

        /// <summary> 出力スクリプト名. </summary>
        public string ExportScriptName { get { return scriptName; } }

        /// <summary> マップモード. </summary>
        public bool UseMapMode { get { return useMapMode; } }

        /// <summary> 生成されるResolverクラス名前空間名. </summary>
        public string ResolverNameSpace { get { return resolverNameSpace; } }

        /// <summary> 生成されるResolverクラス名. </summary>
        public string ResolverName { get { return resolverName; } }

        /// <summary> 条件付きコンパイラシンボル. </summary>
        public string ConditionalCompilerSymbols { get { return conditionalCompilerSymbols; } }

        //----- method -----
    }
}

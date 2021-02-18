
using UnityEngine;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
    public sealed class MessagePackConfig : ReloadableScriptableObject<MessagePackConfig>
    {
        //----- params -----

        private const string DefaultMsBuildPath = "/Library/Frameworks/Mono.framework/Versions/Current/bin";

        public static class Prefs
        {
            public static string msbuildPath
            {
                get { return ProjectPrefs.GetString("MessagePackConfigPrefs-msbuildPath", DefaultMsBuildPath); }
                set { ProjectPrefs.SetString("MessagePackConfigPrefs-msbuildPath", value); }
            }
        }

        //----- field -----

        #pragma warning disable 0414

        [SerializeField]
        private string winMpcRelativePath = null; // コンパイラまでのパス(相対パス).
        [SerializeField]
        private string osxMpcRelativePath = null; // コンパイラまでのパス(相対パス).

        #pragma warning restore 0414

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

        /// <summary> コードジェネレーターパス. </summary>
        public string CodeGeneratorPath
        {
            get
            {
                var relativePath = string.Empty;

                #if UNITY_EDITOR_WIN

                relativePath = winMpcRelativePath;

                #endif

                #if UNITY_EDITOR_OSX

                relativePath = osxMpcRelativePath;

                #endif

                return UnityPathUtility.RelativePathToFullPath(relativePath);
            }
        }

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

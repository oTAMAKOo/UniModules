
using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
    public sealed class MessagePackConfig : ReloadableScriptableObject<MessagePackConfig>
    {
        //----- params -----

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
        
        #pragma warning disable 0414

        [SerializeField]
        private string winMpcRelativePath = null;
        [SerializeField]
        private string osxMpcRelativePath = null;

        #pragma warning restore 0414

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

        /// <summary> コードジェネレータまでのパス(相対パス). </summary>
        public string MpcRelativePath
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
                
                return string.IsNullOrEmpty(relativePath) ? null : UnityPathUtility.RelativePathToFullPath(relativePath);
            }
        }

        //----- method -----
    }
}

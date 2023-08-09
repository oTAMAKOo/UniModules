
using UnityEngine;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
    public sealed class MessagePackConfig : ReloadableScriptableObject<MessagePackConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string mpcRelativePath = null;
        [SerializeField]
        private string codeGenerateTarget = null;
        [SerializeField]
        private string scriptExportAssetDir = null;
        [SerializeField]
        private string scriptName = null;
        [SerializeField]
        private bool useMapMode = true;
        [SerializeField]
        private string processCommand = null;

        [SerializeField]
        [Tooltip("(option) Generated resolver class namespace.")]
        private string resolverNameSpace = "MessagePack";
        [SerializeField]
        [Tooltip("(option) Generated resolver class name.")]
        private string resolverName = "GeneratedResolver";
        [SerializeField]
        [Tooltip("(option) Split with ','.")]
        private string conditionalCompilerSymbols = null;

        [SerializeField]
        [Tooltip("(fix) Force add missing global namespace")]
        private string[] forceAddGlobalSymbols = null;

        //----- property -----

        /// <summary> コードジェネレータまでのパス. </summary>
        public string MpcPath
        {
            get
            {
                if (string.IsNullOrEmpty(mpcRelativePath)){ return null; }

                return UnityPathUtility.RelativePathToFullPath(mpcRelativePath);
            }
        }

        /// <summary> コード生成ターゲット. </summary>
        public string CodeGenerateTarget
        {
            get { return UnityPathUtility.RelativePathToFullPath(codeGenerateTarget); }
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

        /// <summary> コード生成時のコマンド. </summary>
        public string ProcessCommand { get { return processCommand; } }

        /// <summary> 生成されるResolverクラス名前空間名. </summary>
        public string ResolverNameSpace { get { return resolverNameSpace; } }

        /// <summary> 生成されるResolverクラス名. </summary>
        public string ResolverName { get { return resolverName; } }

        /// <summary> 条件付きコンパイラシンボル. </summary>
        public string ConditionalCompilerSymbols { get { return conditionalCompilerSymbols; } }

        /// <summary> 追加参照namespace. </summary>
        public string[] ForceAddGlobalSymbols { get { return forceAddGlobalSymbols; } }

        //----- method -----
    }
}

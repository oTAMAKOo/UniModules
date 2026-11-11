
using UnityEngine;
using System;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
    public sealed class MessagePackConfig : SingletonScriptableObject<MessagePackConfig>
    {
        //----- params -----

        public sealed class Prefs
        {
            public static MpcSetting PrivateSetting
            {
                get { return ProjectPrefs.Get<MpcSetting>(typeof(Prefs).FullName + "-Result", null); }
                set { ProjectPrefs.Set<MpcSetting>(typeof(Prefs).FullName + "-Result", value); }
            }
        }

        [Serializable]
        public sealed class MpcSetting
        {
            [SerializeField]
            public string processCommand = null;
            [SerializeField]
            public string mpcRelativePath = null;
        }

        //----- field -----

        #pragma warning disable CS0414

        [SerializeField]
        private MpcSetting winMpcSetting = null;
        [SerializeField]
        private MpcSetting osxMpcSetting = null;

        #pragma warning restore CS0414

        [SerializeField]
        private string codeGenerateTarget = null;
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

        [SerializeField]
        [Tooltip("(fix) Force add missing global namespace")]
        private string[] forceAddGlobalSymbols = null;

        //----- property -----

        /// <summary> コード生成時のコマンド. </summary>
        public string ProcessCommand
        {
            get
            {
                var processCommand = string.Empty;

                #if UNITY_EDITOR_WIN

                processCommand = winMpcSetting.processCommand;

                #endif

                #if UNITY_EDITOR_OSX

                processCommand = osxMpcSetting.processCommand;

                #endif

                if (Prefs.PrivateSetting != null)
                {
                    processCommand = Prefs.PrivateSetting.processCommand;
                }

                return processCommand;
            }
        }

        /// <summary> コードジェネレータまでのパス. </summary>
        public string MpcPath
        {
            get
            {
                var mpcRelativePath = string.Empty;

                #if UNITY_EDITOR_WIN

                mpcRelativePath = winMpcSetting.mpcRelativePath;

                #endif

                #if UNITY_EDITOR_OSX

                mpcRelativePath = osxMpcSetting.mpcRelativePath;

                #endif

                if (Prefs.PrivateSetting != null)
                {
                    mpcRelativePath = Prefs.PrivateSetting.mpcRelativePath;
                }

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


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
        private string winCompilerRelativePath = null;  // コンパイラまでのパス(相対パス).
        [SerializeField]
        private string osxCompilerRelativePath = null;  // コンパイラまでのパス(相対パス).

        #pragma warning restore 0414

        [SerializeField]
        private string scriptExportAssetDir = null;     // スクリプト出力先.
        [SerializeField]
        private string scriptName = null;               // 出力スクリプト名.

        //----- property -----

        public string CompilerPath
        {
            get
            {
                var relativePath = string.Empty;

                #if UNITY_EDITOR_WIN

                relativePath = winCompilerRelativePath;

                #endif

                #if UNITY_EDITOR_OSX

                relativePath = osxCompilerRelativePath;

                #endif

                return UnityPathUtility.RelativePathToFullPath(relativePath);
            }
        }

        public string ScriptExportDir { get { return UnityPathUtility.ConvertAssetPathToFullPath(scriptExportAssetDir); } }

        public string ExportScriptName { get { return scriptName; } }

        //----- method -----
    }
}

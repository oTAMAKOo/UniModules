﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.MessagePack
{
	public class MessagePackConfig : ReloadableScriptableObject<MessagePackConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string winCompilerRelativePath = null;  // コンパイラまでのパス(相対パス).
        [SerializeField]
        private string osxCompilerRelativePath = null;  // コンパイラまでのパス(相対パス).
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

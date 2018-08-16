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
        private string compilerAssetsRelativePath = null;   // コンパイラまでのパス(相対パス).
        [SerializeField]
        private string scriptExportAssetDir = null;         // スクリプト出力先.
        [SerializeField]
        private string scriptName = null;                   // 出力スクリプト名.

        //----- property -----

        public string CompilerPath
        {
            get { return UnityPathUtility.RelativePathToFullPath(compilerAssetsRelativePath); }
        }

        public string ScriptExportDir { get { return UnityPathUtility.ConvertAssetPathToFullPath(scriptExportAssetDir); } }

        public string ExportScriptName { get { return scriptName; } }

        //----- method -----
    }
}
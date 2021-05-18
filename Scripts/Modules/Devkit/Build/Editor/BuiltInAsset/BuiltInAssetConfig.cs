
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    public sealed class BuiltInAssetConfig : ReloadableScriptableObject<BuiltInAssetConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField, Tooltip("内蔵アセットの対象")]
        private Object[] builtInAssetTargets = null;
        [SerializeField, Tooltip("内蔵アセットに含まない対象")]
        private Object[] ignoreBuiltInAssetTargets = null;
        [SerializeField, Tooltip("内蔵アセットに含まないフォルダ名")]
        private string[] ignoreBuiltInFolderNames = null;
        [SerializeField, Tooltip("アセット検証の対象にしないアセット")]
        private Object[] ignoreValidationAssets = null;
        [SerializeField, Tooltip("アセット検証の対象にしない拡張子")]
        private string[] ignoreValidationExtensions = null;
        [SerializeField, Tooltip("アセット検証でこのサイズ以上は警告")]
        private float warningAssetSize = 1024 * 2f;

        //----- property -----

        public Object[] BuiltInAssetTargets
        {
            get { return builtInAssetTargets ?? (builtInAssetTargets = new Object[0]); }
        }

        public Object[] IgnoreBuiltInAssetTargets
        {
            get { return ignoreBuiltInAssetTargets ?? (ignoreBuiltInAssetTargets = new Object[0]); }
        }

        public string[] IgnoreBuiltInFolderNames
        {
            get { return ignoreBuiltInFolderNames ?? (ignoreBuiltInFolderNames = new string[0]); }
        }

        public Object[] IgnoreValidationAssets
        {
            get { return ignoreValidationAssets ?? (ignoreValidationAssets = new Object[0]); }
        }

        public string[] IgnoreValidationExtensions
        {
            get { return ignoreValidationExtensions ?? (ignoreValidationExtensions = new string[0]); }
        }

        public float WarningAssetSize { get { return warningAssetSize; } }

        //----- method -----
    }
}

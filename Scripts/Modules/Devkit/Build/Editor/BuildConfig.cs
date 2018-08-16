
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    [Serializable]
    public class BuildParamInfo
    {
        public string name;
        public BuildTargetGroup buildGroup;
        public BuildTarget buildTarget;
        public BuildParam buildParam;

        public override string ToString()
        {
            return string.Format("[{0}] {1}", buildTarget, name);
        }
    }

    public class BuildConfig : ReloadableScriptableObject<BuildConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField, ReadOnly, Tooltip("保存されたビルド設定")]
        private BuildParamInfo[] customBuildParams = null;
        [SerializeField, Tooltip("ビルドインアセットの対象")]
        private Object[] buildInAssetTargets = null;
        [SerializeField, Tooltip("ビルドインアセットに含まない対象")]
        private Object[] ignoreBuildInAssetTargets = null;
        [SerializeField, Tooltip("ビルドインアセットに含まないフォルダ名")]
        private string[] ignoreBuildInFolderNames = null;
        [SerializeField, Tooltip("アセット検証の対象にしないアセット")]
        private Object[] ignoreValidationAssets = null;
        [SerializeField, Tooltip("アセット検証の対象にしない拡張子")]
        private string[] ignoreValidationExtensions = null;
        [SerializeField, Tooltip("アセット検証でこのサイズ以上は警告")]
        private float warningAssetSize = 1024 * 2f;

        //----- property -----

        public BuildParamInfo[] CustomBuildParams
        {
            get { return customBuildParams ?? new BuildParamInfo[0]; }
            set { customBuildParams = value; }
        }

        public Object[] BuildInAssetTargets
        {
            get { return buildInAssetTargets ?? (buildInAssetTargets = new Object[0]); }
        }

        public Object[] IgnoreBuildInAssetTargets
        {
            get { return ignoreBuildInAssetTargets ?? (ignoreBuildInAssetTargets = new Object[0]); }
        }

        public string[] IgnoreBuildInFolderNames
        {
            get { return ignoreBuildInFolderNames ?? (ignoreBuildInFolderNames = new string[0]); }
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
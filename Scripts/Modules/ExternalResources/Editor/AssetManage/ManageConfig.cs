
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public sealed class ManageConfig : ReloadableScriptableObject<ManageConfig>
    {
        //----- params -----

        //----- field -----
        [SerializeField]
        private string exportDirectory = null;

        [Header("Crypto")]

        [SerializeField, Tooltip("32文字")]
        private string cryptoKey = null;
        [SerializeField, Tooltip("16文字")]
        private string cryptoIv = null;

        [Header("Ignore AssetManagement")]

        [SerializeField, Tooltip("除外対象(管理しない)")]
        private Object[] ignoreManage = null;
        [SerializeField, Tooltip("除外対象(アセットバンドルの対象にしない)")]
        private Object[] ignoreAssetBundle = null;        
        [SerializeField, Tooltip("除外対象(フォルダ名が含まれる場合対象外)")]
        private string[] ignoreFolder = null;
        [SerializeField, Tooltip("除外対象(対象外の拡張子)")]
        private string[] ignoreExtension = null;

        [Header("Invalid Dependencies Check")]
        [SerializeField, Tooltip("検査除外対象")]
        private Object[] ignoreValidateTarget = null;

        //----- property -----

        /// <summary> 出力先 </summary>
        public string ExportDirectory
        {
            get
            {
                return string.IsNullOrEmpty(exportDirectory) ? null : UnityPathUtility.RelativePathToFullPath(exportDirectory);
            }
        }

        /// <summary> 暗号化Key(32文字) </summary>
        public string CryptoKey { get { return cryptoKey; } }
        /// <summary> 暗号化Iv (16文字)</summary>
        public string CryptoIv { get { return cryptoIv; } }

        /// <summary> 除外対象(管理しない) </summary>
        public Object[] IgnoreManage { get { return ignoreManage; } }
        /// <summary> 除外対象(アセットバンドルの対象にしない) </summary>
        public Object[] IgnoreAssetBundle { get { return ignoreAssetBundle; } }
        /// <summary> 除外対象(フォルダ名が含まれる場合対象外) </summary>
        public string[] IgnoreFolder { get { return ignoreFolder; } }
        /// <summary> 除外対象(対象外の拡張子) </summary>
        public string[] IgnoreExtension { get { return ignoreExtension; } }
        /// <summary> 検査除外対象 </summary>
        public Object[] IgnoreValidateTarget { get { return ignoreValidateTarget; } }

        //----- method -----

        protected override void OnLoadInstance()
        {
            var changed = CleanContents();

            if (changed)
            {
                instance = null;

                UnityEditorUtility.SaveAsset(this);

                Reload();
            }
        }

        private bool CleanContents()
        {
            var change = false;

            Func<Object, bool> IsMissing = asset =>
            {
                if (asset == null) { return true; }

                var assetPath = AssetDatabase.GetAssetPath(asset);

                return string.IsNullOrEmpty(assetPath);
            };

            //====== ignoreManage ======

            if (ignoreManage != null)
            {
                var values = ignoreManage.Where(x => !IsMissing(x)).ToArray();

                if (ignoreManage.Length != values.Length)
                {
                    ignoreManage = values;
                    change = true;
                }
            }

            //====== ignoreAssetBundle ======

            if (ignoreAssetBundle != null)
            {
                var values = ignoreAssetBundle.Where(x => !IsMissing(x)).ToArray();

                if (ignoreAssetBundle.Length != values.Length)
                {
                    ignoreAssetBundle = values;
                    change = true;
                }
            }

            //====== ignoreFolder ======

            if (ignoreFolder != null)
            {
                var values = ignoreFolder.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (ignoreFolder.Length != values.Length)
                {
                    ignoreFolder = values;
                    change = true;
                }
            }

            //====== ignoreExtension ======

            if (ignoreExtension != null)
            {
                var values = ignoreExtension.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (ignoreExtension.Length != values.Length)
                {
                    ignoreExtension = values;
                    change = true;
                }
            }

            //====== ignoreValidateTarget ======

            if (ignoreValidateTarget != null)
            {
                var values = ignoreValidateTarget.Where(x => !IsMissing(x)).ToArray();

                if (ignoreValidateTarget.Length != values.Length)
                {
                    ignoreValidateTarget = values;
                    change = true;
                }
            }

            return change;
        }
    }
}

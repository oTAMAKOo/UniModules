
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public enum IgnoreType
    {
        /// <summary> アセットバンドルの対象にしない. </summary>
        IgnoreAssetBundle,
        /// <summary> 管理しない. </summary>
        IgnoreManage,
        /// <summary> フォルダ名が含まれる場合対象外. </summary>
        IgnoreFolder,
        /// <summary> 対象外の拡張子. </summary>
        IgnoreExtension,
    }

    [Serializable]
    public sealed class IgnoreInfo
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Object[] ignoreManage = null;           // 管理しない.
        [SerializeField]
        private Object[] ignoreAssetBundle = null;      // アセットバンドルの対象にしない.        
        [SerializeField]
        private string[] ignoreFolder = null;           // フォルダ名が含まれる場合対象外.
        [SerializeField]
        private string[] ignoreExtension = null;        // 対象外の拡張子.
        
        private string[] ignoreManagePaths = null;
        private string[] ignoreAssetBundlePaths = null;

        //----- property -----

        //----- method -----

        /// <summary> 最適化 </summary>
        public bool Optimisation()
        {
            var change = false;

            Func<Object, bool> IsMissing = asset =>
            {
                if (asset == null) { return true; }

                var assetPath = AssetDatabase.GetAssetPath(asset);

                return string.IsNullOrEmpty(assetPath);
            };

            ignoreManagePaths = null;
            ignoreAssetBundlePaths = null;

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

            //====== ignoreManage ======

            if (ignoreAssetBundle != null)
            {
                var values = ignoreAssetBundle.Where(x => !IsMissing(x)).ToArray();

                if (ignoreAssetBundle.Length != values.Length)
                {
                    ignoreAssetBundle = values;
                    change = true;
                }
            }

            //====== ignoreManage ======

            if (ignoreFolder != null)
            {
                var values = ignoreFolder.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (ignoreFolder.Length != values.Length)
                {
                    ignoreFolder = values;
                    change = true;
                }
            }

            //====== ignoreManage ======

            if (ignoreExtension != null)
            {
                var values = ignoreExtension.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (ignoreExtension.Length != values.Length)
                {
                    ignoreExtension = values;
                    change = true;
                }
            }            

            return change;
        }

        /// <summary> 除外対象のパスか検証 </summary>
        public IgnoreType? GetIgnoreType(string assetPath)
        {
            if (ignoreManagePaths == null)
            {
                ignoreManagePaths = ignoreManage
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            foreach (var item in ignoreManagePaths)
            {
                if (assetPath.StartsWith(item))
                {
                    return IgnoreType.IgnoreManage;
                }
            }

            if (ignoreAssetBundlePaths == null)
            {
                ignoreAssetBundlePaths = ignoreAssetBundle
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            foreach (var item in ignoreAssetBundlePaths)
            {
                if (assetPath.StartsWith(item))
                {
                    return IgnoreType.IgnoreAssetBundle;
                }
            }

            foreach (var item in ignoreFolder)
            {
                if (assetPath.Split('/').Contains(item))
                {
                    return IgnoreType.IgnoreFolder;
                }
            }

            foreach (var item in ignoreExtension)
            {
                if (Path.GetExtension(assetPath) == item)
                {
                    return IgnoreType.IgnoreExtension;
                }
            }

            return null;
        }
    }
}

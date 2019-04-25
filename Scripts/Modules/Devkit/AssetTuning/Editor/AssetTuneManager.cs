

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using System.IO;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public class AssetTuneManager : Singleton<AssetTuneManager>
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, AssetTuner> assetTuners = null;

        private HashSet<string> firstImportAssets = null;

        //----- property -----

        public AssetTuner[] AssetTuners { get; private set; }

        //----- method -----

        protected AssetTuneManager()
        {
            assetTuners = new Dictionary<string, AssetTuner>();
            firstImportAssets = new HashSet<string>();

            AssetTuners = new AssetTuner[0];
        }

        public void Register<T>() where T : AssetTuner, new()
        {
            var className = typeof(T).FullName;

            if (assetTuners.ContainsKey(className)) { return; }

            var assetTuner = new T();
            assetTuners.Add(className, assetTuner);

            AssetTuners = assetTuners.Values.OrderBy(x => x.Priority).ToArray();
        }

        /// <summary> 
        /// 初回インポートアセットを記録.
        /// </summary>
        public void MarkFirstImport(string assetPath)
        {
            if (!firstImportAssets.Contains(assetPath))
            {
                firstImportAssets.Add(assetPath);
            }
        }

        /// <summary>
        /// 初回インポートか確認.
        /// </summary>
        public bool IsFirstImport(string assetPath)
        {
            var firstImport = false;

            // 登録されていたら初回.
            firstImport |= firstImportAssets.Contains(assetPath);

            // .metaがなかったら初回.
            var metaFilePath = string.Empty;

            try
            {
                metaFilePath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath) + UnityEditorUtility.MetaFileExtension;
            }
            catch (Exception)
            {
                return false;
            }

            firstImport |= !File.Exists(metaFilePath);

            return firstImport;
        }

        /// <summary> 
        /// 初回インポート完了. 
        /// </summary>
        public void FinishFirstImport(string assetPath)
        {
            if (firstImportAssets.Contains(assetPath))
            {
                firstImportAssets.Remove(assetPath);
            }
        }
    }
}

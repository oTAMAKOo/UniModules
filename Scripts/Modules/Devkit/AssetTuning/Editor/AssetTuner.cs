

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
    public interface IAssetTuner
    {
        bool Validate(string path);
        void OnAssetCreate(string path);
        void OnAssetImport(string path);
        void OnAssetDelete(string path);
        void OnAssetMove(string path, string from);
    }

    public class AssetTuner
    {
        //----- params -----

        //----- field -----

        private static AssetTuner instance = null;

        private Dictionary<string, IAssetTuner> assetTuners = new Dictionary<string, IAssetTuner>();

        private HashSet<string> firstImportAssets = new HashSet<string>();

        //----- property -----

        public static AssetTuner Instance
        {
            get { return instance ?? (instance = new AssetTuner()); }
        }

        public IAssetTuner[] AssetTuners
        {
            get { return assetTuners.Values.ToArray(); }
        }

        //----- method -----

        public void Register<T>() where T : IAssetTuner, new()
        {
            var className = typeof(T).FullName;

            if (assetTuners.ContainsKey(className)) { return; }

            var assetTuner = new T();
            assetTuners.Add(className, assetTuner);
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
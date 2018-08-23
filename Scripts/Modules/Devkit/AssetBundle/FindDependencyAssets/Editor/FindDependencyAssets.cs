﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundles
{
    // 他のアセットがアセットを参照している情報.
    public class ReferenceInfo
    {
        public string AssetPath { get; private set; }
        public List<string> ReferenceAssetBundles { get; private set; }

        public ReferenceInfo(string assetPath)
        {
            this.AssetPath = assetPath;
            this.ReferenceAssetBundles = new List<string>();
        }
    }

    // アセットバンドルの依存情報.
    public class AssetBundleDependentInfo
    {
        public string AssetBundleName { get; private set; }
        public string[] AssetPaths { get; private set; }
        public string[] DependentAssetPaths { get; private set; }

        public AssetBundleDependentInfo(string assetBundleName, string[] assetPaths, string[] dependentAssetPaths)
        {
            this.AssetBundleName = assetBundleName;
            this.AssetPaths = assetPaths;
            this.DependentAssetPaths = dependentAssetPaths;
        }
    }

    public class FindDependencyAssets
    {
        //----- params -----

        //----- field -----

        private List<ReferenceInfo> referenceInfos = null;
        private Dictionary<string, AssetBundleDependentInfo> assetBundleDependentInfos = null;

        //----- property -----

        public List<ReferenceInfo> ReferenceInfos { get { return referenceInfos; } }
        public Dictionary<string, AssetBundleDependentInfo> AssetBundleDependentInfos { get { return assetBundleDependentInfos; } }

        //----- method -----

        public void CollectDependencies()
        {
            var progressTitle = "Find Dependencies";
            var progressInfo = string.Empty;
            var progressCount = 0;

            referenceInfos = new List<ReferenceInfo>();
            assetBundleDependentInfos = new Dictionary<string, AssetBundleDependentInfo>();

            var allAssetPathByAssetBundleName = GetAllAssetPathByAssetBundleName();
            
            // アセットバンドルからの参照情報を構築.

            progressCount = 0;
            progressInfo = "CollectDependencies.";

            foreach(var assetBundle in allAssetPathByAssetBundleName)
            {
                var progress = progressCount / allAssetPathByAssetBundleName.Count;
                EditorUtility.DisplayProgressBar(progressTitle, progressInfo, progress);
                progressCount++;

                var assetPaths = new List<string>();
                var dependentAssetPaths = new List<string>();

                foreach(var assetPath in assetBundle)
                {
                    var dependencies = AssetDatabase.GetDependencies(assetPath);

                    foreach (var path in dependencies)
                    {
                        var bundleName = GetBundleName(path);

                        if(assetBundle.Key == bundleName)
                        {
                            // 既に参照済みの場合は再登録.
                            if(dependentAssetPaths.Contains(path))
                            {
                                dependentAssetPaths.Remove(path);
                            }

                            // 既に参照済みの場合は追加なし.
                            if(assetPaths.Contains(path)){ continue; }

                            assetPaths.Add(path);
                        }
                        else
                        {
                            // 既に参照済みの場合は参照扱いしない.
                            if(dependentAssetPaths.Contains(path)){ continue; }

                            dependentAssetPaths.Add(path);
                        }
                    }
                }

                var item = new AssetBundleDependentInfo(assetBundle.Key, assetPaths.ToArray(), dependentAssetPaths.ToArray());
                assetBundleDependentInfos.Add(item.AssetBundleName, item);
            }

            // 参照されているアセット情報を収集.

            progressCount = 0;
            progressInfo = "BuildReferenceInfo.";

            foreach (var info in assetBundleDependentInfos)
            {
                var progress = progressCount / assetBundleDependentInfos.Count;
                EditorUtility.DisplayProgressBar(progressTitle, progressInfo, progress);
                progressCount++;

                foreach (var assetPath in info.Value.DependentAssetPaths)
                {
                    var referenceInfo = referenceInfos.FirstOrDefault(x => x.AssetPath == assetPath);

                    if(referenceInfo == null)
                    {
                        referenceInfo = new ReferenceInfo(assetPath);
                        referenceInfo.ReferenceAssetBundles.Add(info.Key);
                        referenceInfos.Add(referenceInfo);
                    }
                    else
                    {
                        referenceInfo.ReferenceAssetBundles.Add(info.Key);
                    }
                }
            }

            referenceInfos = referenceInfos
                .OrderBy(x => x.ReferenceAssetBundles.Count)
                .Reverse()
                .ToList();

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// アセットバンドル名でアセットをグループ化.
        /// </summary>
        /// <returns></returns>
        private ILookup<string, string> GetAllAssetPathByAssetBundleName()
        {
            var allAssetGuids = AssetDatabase.FindAssets("", new string[] { UnityPathUtility.AssetsFolder });
            var allAssetPaths = allAssetGuids.Select(x => AssetDatabase.GUIDToAssetPath(x));

            return allAssetPaths
                .Select(x => Tuple.Create(GetBundleName(x), x))
                .Where(x => !string.IsNullOrEmpty(x.Item1))
                .ToLookup(x => x.Item1, x => x.Item2);
        }

        /// <summary>
        /// アセットからアセットバンドル名を取得.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetBundleName(string path)
        {
            var importer = AssetImporter.GetAtPath(path);

            var bundleName = importer.assetBundleName;
            var variantName = importer.assetBundleVariant;

            if(string.IsNullOrEmpty(bundleName)) { return null; }
            
            return string.IsNullOrEmpty(variantName) ? bundleName : bundleName + "." + variantName;
        }
    }
}

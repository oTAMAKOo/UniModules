
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
            var progressCount = 0f;
            var totalCount = 0f;

            referenceInfos = new List<ReferenceInfo>();
            assetBundleDependentInfos = new Dictionary<string, AssetBundleDependentInfo>();

            var allAssetPathByAssetBundleName = GetAllAssetPathByAssetBundleName();

            //====== アセットバンドルからの参照情報を構築 ======

            progressCount = 0;
            totalCount = allAssetPathByAssetBundleName.Count;

            foreach (var assetBundle in allAssetPathByAssetBundleName)
            {
                EditorUtility.DisplayProgressBar("Find Dependencies", assetBundle.Key, progressCount / totalCount);

                progressCount++;

                var assetPaths = new List<string>();
                var dependentAssetPaths = new List<string>();

                foreach (var assetPath in assetBundle)
                {
                    var dependencies = AssetDatabase.GetDependencies(assetPath);

                    foreach (var path in dependencies)
                    {
                        var bundleName = GetBundleName(path);

                        if (assetBundle.Key == bundleName)
                        {
                            // 既に参照済みの場合は再登録.
                            if (dependentAssetPaths.Contains(path))
                            {
                                dependentAssetPaths.Remove(path);
                            }

                            // 既に参照済みの場合は追加なし.
                            if (assetPaths.Contains(path)) { continue; }

                            assetPaths.Add(path);
                        }
                        else
                        {
                            // 既に参照済みの場合は参照扱いしない.
                            if (dependentAssetPaths.Contains(path)) { continue; }

                            dependentAssetPaths.Add(path);
                        }
                    }
                }

                var item = new AssetBundleDependentInfo(assetBundle.Key, assetPaths.ToArray(), dependentAssetPaths.ToArray());
                assetBundleDependentInfos.Add(item.AssetBundleName, item);
            }

            //====== 参照されているアセット情報を収集 ======

            progressCount = 0;
            totalCount = assetBundleDependentInfos.Count;

            foreach (var info in assetBundleDependentInfos)
            {
                EditorUtility.DisplayProgressBar("Build ReferenceInfo", info.Key, progressCount / totalCount);

                progressCount++;

                foreach (var assetPath in info.Value.DependentAssetPaths)
                {
                    var referenceInfo = referenceInfos.FirstOrDefault(x => x.AssetPath == assetPath);

                    if (referenceInfo == null)
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
            var title = "Build AssetBundle Info";

            EditorUtility.DisplayProgressBar(title, "Find AllAssets", 0);

            var allAssetGuids = AssetDatabase.FindAssets("", new string[] { UnityPathUtility.AssetsFolder });

            var allAssetPaths = new List<Tuple<string, string>>();

            for (var i = 0; i < allAssetGuids.Length; i++)
            {
                var guid = allAssetGuids[i];

                EditorUtility.DisplayProgressBar(title, guid, (float)i / allAssetGuids.Length);

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                var assetBundleName = GetBundleName(assetPath);

                if (string.IsNullOrEmpty(assetBundleName)) { continue; }

                allAssetPaths.Add(Tuple.Create(assetBundleName, assetPath));
            }

            EditorUtility.ClearProgressBar();

            return allAssetPaths.ToLookup(x => x.Item1, x => x.Item2);
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

            if (string.IsNullOrEmpty(bundleName)) { return null; }

            return string.IsNullOrEmpty(variantName) ? bundleName : bundleName + "." + variantName;
        }
    }
}


using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Extensions.Devkit;
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
    
    public sealed class AssetManagement : Singleton<AssetManagement>
    {
        //----- params -----

        private const string AssetNameSeparator = "_";

        //----- field -----

        private string externalResourcesPath = null;

        private Dictionary<string, ManageInfo> managedInfos = null;

        private string[] ignoreManagePaths = null;

        private string[] ignoreAssetBundlePaths = null;

        private bool initialized = false;

        //----- property -----

        public string ExternalResourcesPath { get { return externalResourcesPath; } }

        //----- method -----

        public void Initialize(string externalResourcesPath)
        {
            var managedAssets = ManagedAssets.Instance;

            if (managedAssets == null) { return; }

            if (initialized) { return; }

            this.externalResourcesPath = externalResourcesPath;

            managedInfos = managedAssets.GetAllInfos().ToDictionary(x => x.guid);

            ManageConfig.OnReloadAsObservable()
                .Subscribe(_ =>
                   {
                       ignoreManagePaths = null;
                       ignoreAssetBundlePaths = null;
                   })
                .AddTo(Disposable);

            initialized = true;
        }

        public AssetInfo GetAssetInfo(string assetPath, ManageInfo managedInfo = null)
        {
            if (IsIgnoreManageAsset(assetPath)){ return null; }

            if (managedInfo == null)
            {
                managedInfo = GetAssetManagedInfo(assetPath);
            }

            if (managedInfo == null) { return null; }

            var assetInfo = new AssetInfo(assetPath, managedInfo.category, managedInfo.tag);

            var assetBundleName = GetAssetBundleName(assetPath, managedInfo);

            if (!string.IsNullOrEmpty(assetBundleName))
            {
                var assetBundleInfo = new AssetBundleInfo(assetBundleName);

                assetInfo.SetAssetBundleInfo(assetBundleInfo);
            }

            return assetInfo;
        }

        public IEnumerable<AssetInfo> GetAssetInfos(string assetPath)
        {
            var assetInfos = new List<AssetInfo>();

            var assetPaths = new string[0];

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                assetPaths = UnityEditorUtility.GetAllAssetPathInFolder(assetPath);
            }
            else
            {
                assetPaths = new string[] { assetPath };
            }

            foreach (var path in assetPaths)
            {
                var assetInfo = GetAssetInfo(path);

                if (assetInfo != null)
                {
                    assetInfos.Add(assetInfo);
                }
            }

            return assetInfos;
        }

        public IEnumerable<AssetInfo> GetAllAssetInfos()
        {
            var assetInfos = new List<AssetInfo>();

            foreach (var manageInfo in managedInfos.Values)
            {
                var assetPaths = GetManageAssetPaths(manageInfo);

                foreach (var assetPath in assetPaths)
                {
                    var assetInfo = GetAssetInfo(assetPath, manageInfo);

                    if (assetInfo != null)
                    {
                        assetInfos.Add(assetInfo);
                    }
                }
            }

            return assetInfos;
        }

        public string[] GetAllCategoryNames()
        {
            return managedInfos.Values.Select(x => x.category)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToArray();
        }

        public string[] GetManageAssetPaths(ManageInfo manageInfo)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(manageInfo.guid);

            if (!AssetDatabase.IsValidFolder(assetPath)){ return new string[] { assetPath }; }

            var assetPaths = UnityEditorUtility.GetAllAssetPathInFolder(assetPath);

            return assetPaths.Where(x => !IsIgnoreManageAsset(x)).ToArray();
        }

        public bool SetAssetBundleName(string assetPath, string assetBundleName)
        {
            var importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null){ return false; }

            var changed = false;

            // バリアントは使わない.
            if (!string.IsNullOrEmpty(importer.assetBundleVariant))
            {
                importer.assetBundleVariant = string.Empty;

                importer.SaveAndReimport();

                changed = true;
            }

            // アセットバンドル名設定.

            if (importer.assetBundleName != assetBundleName)
            {
                importer.assetBundleName = assetBundleName;

                EditorUtility.SetDirty(importer);

                changed = true;
            }

            return changed;
        }

        public string GetAssetLoadPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return string.Empty; }

            var externalResourcesDir = ExternalResourcesPath + PathUtility.PathSeparator;

            var assetLoadPath = assetPath.Substring(externalResourcesDir.Length, assetPath.Length - externalResourcesDir.Length);

            return assetLoadPath;
        }

        private string GetAssetBundleName(string assetPath, ManageInfo manageInfo)
        {
            //-------------------------------------------------------------------------------------
            // ※ nullを返すと再インポート時に無限ループになるので未設定の場合はstring.Emptyを返す.
            //-------------------------------------------------------------------------------------

            var assetBundleName = string.Empty;

            if (manageInfo == null) { return string.Empty; }

            if (string.IsNullOrEmpty(manageInfo.guid)) { return string.Empty; }

            if (!manageInfo.isAssetBundle) { return string.Empty; }

            // カテゴリ名設定.
            var category = manageInfo.category;

            if (!string.IsNullOrEmpty(category))
            {
                assetBundleName = category + PathUtility.PathSeparator;
            }

            // 管理アセットの親フォルダパス.
            var managePath = AssetDatabase.GUIDToAssetPath(manageInfo.guid);
            var parentDir = PathUtility.ConvertPathSeparator(Directory.GetParent(managePath).ToString() + PathUtility.PathSeparator);
            var externalResourcesDir = externalResourcesPath + PathUtility.PathSeparator;
            var folder = parentDir.Substring(externalResourcesDir.Length);

            assetBundleName += folder.Replace(PathUtility.PathSeparator.ToString(), AssetNameSeparator);

            switch (manageInfo.assetBundleNamingRule)
            {
                case AssetBundleNamingRule.ManageAssetName:
                    assetBundleName += Path.GetFileNameWithoutExtension(managePath);
                    break;

                case AssetBundleNamingRule.ChildAssetName:
                case AssetBundleNamingRule.PrefixAndChildAssetName:
                    {
                        assetBundleName += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;

                        if (AssetDatabase.IsValidFolder(managePath))
                        {
                            folder += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;
                        }

                        var resourcePath = assetPath.Substring((externalResourcesDir + folder).Length);

                        switch (manageInfo.assetBundleNamingRule)
                        {
                            case AssetBundleNamingRule.ChildAssetName:
                                assetBundleName += Path.GetFileNameWithoutExtension(resourcePath);
                                break;

                            case AssetBundleNamingRule.PrefixAndChildAssetName:
                                assetBundleName += manageInfo.assetBundleNameStr + AssetNameSeparator + Path.GetFileNameWithoutExtension(resourcePath);
                                break;
                        }
                    }
                    break;

                case AssetBundleNamingRule.Specified:
                    {
                        assetBundleName += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;

                        assetBundleName += manageInfo.assetBundleNameStr;
                    }
                    break;
            }

            if (string.IsNullOrEmpty(assetBundleName)) { return string.Empty; }

            // アセットバンドル名は小文字しか設定出来ないので小文字に変換.
            return assetBundleName.ToLower();
        }

        public bool IsExternalResourcesTarget(Object[] dropObjects)
        {
            foreach (var dropObject in dropObjects)
            {
                var path = AssetDatabase.GetAssetPath(dropObject);

                if (!path.StartsWith(externalResourcesPath)) { return false; }
            }

            return true;
        }

        private bool IsIgnoreManageAsset(string assetPath)
        {
            var ignoreType = GetIgnoreType(assetPath);

            if (ignoreType.HasValue)
            {
                return ignoreType != IgnoreType.IgnoreAssetBundle;
            }

            return false;
        }

        #region ManageInfo

        public ManageInfo[] GetManageInfos(string category)
        {
            if (string.IsNullOrEmpty(category)) { return null; }

            return managedInfos.Values.Where(x => x.category == category).ToArray();
        }

        public void UpdateManageInfo(ManageInfo manageInfo)
        {
            managedInfos[manageInfo.guid] = manageInfo;

            Save();
        }

        public ManageInfo AddManageInfo(string category, Object manageTarget)
        {
            if (manageTarget == null) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(manageTarget);

            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var ignoreType = GetIgnoreType(assetPath);
            
            var manageInfo = new ManageInfo()
            {
                guid = assetGuid,
                category = category,
                isAssetBundle = ignoreType != IgnoreType.IgnoreAssetBundle,
            };

            managedInfos.Add(assetGuid, manageInfo);

            Save();

            return manageInfo;
        }

        public void DeleteManageInfo(ManageInfo manageInfo)
        {
            if (manageInfo == null) { return; }

            managedInfos.Remove(manageInfo.guid);

            Save();
        }

        private ManageInfo GetAssetManagedInfo(string assetPath)
        {
            var path = assetPath;

            // 除外対象.
            
            var ignoreType = GetIgnoreType(assetPath);

            if (ignoreType.HasValue)
            {
                if (ignoreType != IgnoreType.IgnoreAssetBundle) { return null; }
            }

            // 対象アセットを管理しているManageInfoを末尾から検索していく.

            while (true)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);

                var manageInfo = managedInfos.GetValueOrDefault(guid);

                if (manageInfo != null) { return manageInfo; }

                path = Path.GetDirectoryName(path);

                if (string.IsNullOrEmpty(path)) { break; }
            }

            return null;
        }

        public bool ValidateManageInfo(Object assetObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(assetObject);

            // 既に登録済み.
            var manageInfo = GetAssetManagedInfo(assetPath);

            if (manageInfo != null)
            {
                Debug.Log("Already registered.");
                return false;
            }

            // 除外対象.

            var ignoreType = GetIgnoreType(assetPath);

            if (ignoreType == IgnoreType.IgnoreManage)
            {
                Debug.Log("This asset is ignore manage asset.");
                return false;
            }

            if (ignoreType == IgnoreType.IgnoreExtension)
            {
                Debug.Log("This asset is ignore extension asset.");
                return false;
            }

            return true;
        }

        #endregion

        #region Category

        public void RenameCategory(string from, string to)
        {
            var targets = managedInfos.Values.Where(x => x.category == from).ToArray();

            foreach (var target in targets)
            {
                target.category = to;
            }

            Save();
        }

        public void DeleteCategory(string category)
        {
            var targets = managedInfos.Values.Where(x => x.category == category).ToArray();

            for (var i = 0; i < targets.Length; i++)
            {
                var guid = targets[i].guid;

                managedInfos.Remove(guid);
            }

            Save();
        }

        #endregion

        public void Save()
        {
            var managedAssets = ManagedAssets.Instance;

            var infos = managedInfos.Values.ToArray();

            managedAssets.SetManageInfos(infos);
        }

        /// <summary> 除外対象のパスか検証 </summary>
        public IgnoreType? GetIgnoreType(string assetPath)
        {
            var manageConfig = ManageConfig.Instance;

            if (manageConfig == null){ return null; }

            if (ignoreManagePaths == null)
            {
                ignoreManagePaths = manageConfig.IgnoreManage
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            if (ignoreAssetBundlePaths == null)
            {
                ignoreAssetBundlePaths = manageConfig.IgnoreAssetBundle
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            foreach (var item in ignoreManagePaths)
            {
                if (assetPath.StartsWith(item))
                {
                    return IgnoreType.IgnoreManage;
                }
            }

            foreach (var item in ignoreAssetBundlePaths)
            {
                if (assetPath.StartsWith(item))
                {
                    return IgnoreType.IgnoreAssetBundle;
                }
            }

            foreach (var item in manageConfig.IgnoreFolder)
            {
                if (assetPath.Split('/').Contains(item))
                {
                    return IgnoreType.IgnoreFolder;
                }
            }

            foreach (var item in manageConfig.IgnoreExtension)
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

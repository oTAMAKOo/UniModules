
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using Modules.Devkit.Project;

using Object = UnityEngine.Object;

namespace Modules.ExternalAssets
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

        public static class Prefs
        {
            public static bool manifestUpdateRequest
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-manifestUpdateRequest", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-manifestUpdateRequest", value); }
            }
        }

        //----- field -----

        private string externalAssetPath = null;
        private string shareResourcesPath = null;

        private Dictionary<string, ManageInfo> managedInfos = null;

        private Dictionary<string, string> manageInfoAssetPathByGuid = null;

        private Dictionary<string, string> cryptoFileNameCache = null;

        private string[] ignoreManagePaths = null;

        private string[] ignoreAssetBundlePaths = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (Application.isBatchMode){ return; }

            if (Prefs.manifestUpdateRequest)
            {
                AssetInfoManifestGenerator.Generate().Forget();
            }
        }

        public void Initialize()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return; }

            var managedAssets = ManagedAssets.Instance;

            if (managedAssets == null) { return; }

            if (initialized) { return; }

            externalAssetPath = projectResourceFolders.ExternalAssetPath;
            shareResourcesPath = projectResourceFolders.ShareResourcesPath;

            managedInfos = managedAssets.GetAllInfos().ToDictionary(x => x.guid);

            manageInfoAssetPathByGuid = managedAssets.GetAllInfos()
                .ToDictionary(x => x.guid, x =>
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(x.guid);

                    return PathUtility.ConvertPathSeparator(assetPath);
                });

            cryptoFileNameCache = new Dictionary<string, string>();

            ignoreManagePaths = null;
            ignoreAssetBundlePaths = null;

            initialized = true;
        }

        public AssetInfo GetAssetInfo(string assetPath, ManageInfo managedInfo = null)
        {
            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            if (IsIgnoreManageAsset(assetPath)){ return null; }

            if (managedInfo == null)
            {
                managedInfo = GetAssetManagedInfo(assetPath);
            }

            if (managedInfo == null) { return null; }

            var resourcePath = GetAssetLoadPath(assetPath);

            var assetInfo = new AssetInfo(resourcePath, managedInfo.group, managedInfo.labels);

            var assetBundleName = GetAssetBundleName(assetPath, managedInfo);

            if (!string.IsNullOrEmpty(assetBundleName))
            {
                var assetBundleInfo = new AssetBundleInfo(assetBundleName);

                assetInfo.SetAssetBundleInfo(assetBundleInfo);
                
                SetCryptoFileName(assetInfo);
            }

            return assetInfo;
        }

        public async UniTask<IEnumerable<AssetInfo>> GetAssetInfos(string assetPath)
        {
            var assetInfos = new List<AssetInfo>();

            var assetPaths = new string[0];

            if (PathUtility.IsFolder(assetPath))
            {
                assetPaths = await UnityEditorUtility.GetAllAssetPathInFolder(assetPath);
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

        public async UniTask<AssetInfo[]> GetAllAssetInfos()
        {
            var list = new List<Tuple<string, ManageInfo>>();

            foreach (var manageInfo in managedInfos.Values)
            {
                var assetPaths = await GetManageAssetPaths(manageInfo);

                foreach (var assetPath in assetPaths)
                {
                    list.Add(Tuple.Create(assetPath, manageInfo));
                }
            }

            var chunck = list.Chunk(250);

            var tasks = new List<UniTask<List<AssetInfo>>>();

            foreach (var targets in chunck)
            {
                var items = targets.ToArray();

                var task = UniTask.RunOnThreadPool(() =>
                {
                    var assetInfos = new List<AssetInfo>();

                    foreach (var item in items)
                    {
                        var assetInfo = GetAssetInfo(item.Item1, item.Item2);

                        if (assetInfo != null)
                        {
                            assetInfos.Add(assetInfo);
                        }
                    }

                    return assetInfos;
                });

                tasks.Add(task);
            }

            var result = await UniTask.WhenAll(tasks);

            return result.SelectMany(x => x).ToArray();
        }

        public string[] GetAllGroupNames()
        {
            return managedInfos.Values.Select(x => x.group)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .Append(ExternalAsset.ShareGroupName)
                .ToArray();
        }

        public async UniTask<string[]> GetManageAssetPaths(ManageInfo manageInfo)
        {
            var manageAssetPath = manageInfoAssetPathByGuid.GetValueOrDefault(manageInfo.guid);

            if (!PathUtility.IsFolder(manageAssetPath)){ return new string[] { manageAssetPath }; }

            var assetPaths = await UnityEditorUtility.GetAllAssetPathInFolder(manageAssetPath);

            var ignoreAssetPaths = managedInfos.Values
                .Select(x => manageInfoAssetPathByGuid.GetValueOrDefault(x.guid))
                .Where(x => x != manageAssetPath && x.StartsWith(manageAssetPath))
                .ToArray();

            var manageAssetPaths = assetPaths.Where(x => !IsIgnoreManageAsset(x))
                .Where(x => ignoreAssetPaths.All(y => !x.StartsWith(y)))
                .ToArray();

            return manageAssetPaths;
        }

        public void SetCryptoFileName(AssetInfo assetInfo)
        {
            var fileName = string.Empty;
            var extension = string.Empty;

            if (assetInfo.IsAssetBundle)
            {
                fileName = assetInfo.AssetBundle.AssetBundleName;
            }
            else if(!string.IsNullOrEmpty(assetInfo.ResourcePath))
            {
                extension = Path.GetExtension(assetInfo.ResourcePath);

                fileName = PathUtility.GetPathWithoutExtension(assetInfo.ResourcePath);
            }

            if (string.IsNullOrEmpty(fileName)) { return; }

            var cryptoFileName = string.Empty;

            lock (cryptoFileNameCache)
            {
                cryptoFileName = cryptoFileNameCache.GetValueOrDefault(fileName);

                if (string.IsNullOrEmpty(cryptoFileName))
                {
                    cryptoFileName = fileName.GetHash();

                    lock (cryptoFileNameCache)
                    {
                        cryptoFileNameCache.Add(fileName, cryptoFileName);
                    }
                }
            }

            if (!string.IsNullOrEmpty(extension))
            {
                cryptoFileName += extension;
            }

            assetInfo.SetFileName(cryptoFileName);
        }

        public bool SetAssetBundleName(string assetPath, string assetBundleName, bool force = false)
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

            if (ApplyAssetBundleName(importer, assetBundleName, force))
            {
                changed = true;
            }

            return changed;
        }

        private bool ApplyAssetBundleName(AssetImporter importer, string assetBundleName, bool force)
        {
            var apply = false;

            if (importer == null) { return false; }

            if (force || importer.assetBundleName != assetBundleName)
            {
                importer.assetBundleName = assetBundleName;

                EditorUtility.SetDirty(importer);

                apply = true;
            }

            return apply;
        }

        public string GetAssetLoadPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return string.Empty; }

            var resourcesDir = string.Empty;

            if (assetPath.StartsWith(externalAssetPath))
            {
                resourcesDir = externalAssetPath + PathUtility.PathSeparator;
            }
            else if (assetPath.StartsWith(shareResourcesPath))
            {
                resourcesDir = shareResourcesPath + PathUtility.PathSeparator;
            }

            var assetLoadPath = assetPath.Substring(resourcesDir.Length, assetPath.Length - resourcesDir.Length);

            if (assetPath.StartsWith(shareResourcesPath))
            {
                assetLoadPath = ExternalAsset.ShareGroupPrefix + assetLoadPath;
            }

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

            // グループ名設定.
            var group = manageInfo.group;

            if (!string.IsNullOrEmpty(group))
            {
                assetBundleName += group + PathUtility.PathSeparator;
            }

            // 管理アセットの親フォルダパス.
            var managePath = manageInfoAssetPathByGuid.GetValueOrDefault(manageInfo.guid);

            var parentDir = PathUtility.ConvertPathSeparator(Path.GetDirectoryName(managePath) + PathUtility.PathSeparator);

            var resourcesDir = string.Empty;

            if (assetPath.StartsWith(externalAssetPath))
            {
                resourcesDir = externalAssetPath + PathUtility.PathSeparator;
            }
            else if (assetPath.StartsWith(shareResourcesPath))
            {
                resourcesDir = shareResourcesPath + PathUtility.PathSeparator;
            }

            var folder = parentDir.Substring(resourcesDir.Length);

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

                        if (PathUtility.IsFolder(managePath))
                        {
                            folder += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;
                        }

                        var resourcePath = assetPath.Substring((resourcesDir + folder).Length);

                        var targetName = resourcePath.Split(PathUtility.PathSeparator).FirstOrDefault(x => !string.IsNullOrEmpty(x));

                        switch (manageInfo.assetBundleNamingRule)
                        {
                            case AssetBundleNamingRule.ChildAssetName:
                                assetBundleName += Path.GetFileNameWithoutExtension(targetName);
                                break;

                            case AssetBundleNamingRule.PrefixAndChildAssetName:
                                assetBundleName += manageInfo.assetBundleNameStr + AssetNameSeparator + Path.GetFileNameWithoutExtension(targetName);
                                break;
                        }
                    }
                    break;

                case AssetBundleNamingRule.AssetFilePath:
                    {
                        var resourcePath = assetPath.Substring((resourcesDir + folder).Length);

                        var targetName = resourcePath.Replace(PathUtility.PathSeparator, '_');

                        assetBundleName += Path.GetFileNameWithoutExtension(targetName);
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

        public bool IsExternalAssetTarget(Object[] dropObjects)
        {
            return CheckResourcesTarget(externalAssetPath, dropObjects);
        }

        public bool IsShareResourcesTarget(Object[] dropObjects)
        {
            return CheckResourcesTarget(shareResourcesPath, dropObjects);
        }

        private bool CheckResourcesTarget(string targetPath, Object[] dropObjects)
        {
            foreach (var dropObject in dropObjects)
            {
                var path = AssetDatabase.GetAssetPath(dropObject);

                if (!path.StartsWith(targetPath)) { return false; }
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
        
        public async UniTask ApplyAllAssetBundleName(bool force = false)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var allAssetInfos = await GetAllAssetInfos();

            using (new AssetEditingScope())
            {
                var count = allAssetInfos.Length;

                for (var i = 0; i < count; i++)
                {
                    var assetInfo = allAssetInfos[i];

                    if (!assetInfo.IsAssetBundle) { continue; }
                    
                    var assetPath = PathUtility.Combine(externalAssetPath, assetInfo.ResourcePath);

                    var importer = AssetImporter.GetAtPath(assetPath);

                    if (ApplyAssetBundleName(importer, assetInfo.AssetBundle.AssetBundleName, force))
                    {
                        EditorUtility.DisplayProgressBar("Apply AssetBundleName", assetInfo.FileName, (float)i / count);
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        #region ManageInfo

        public ManageInfo[] GetManageInfos(string group)
        {
            if (string.IsNullOrEmpty(group)) { return null; }

            return managedInfos.Values.Where(x => x.group == group)
                .OrderBy(x => AssetDatabase.GUIDToAssetPath(x.guid), new NaturalComparer())
                .ToArray();
        }

        public void UpdateManageInfo(ManageInfo manageInfo)
        {
            managedInfos[manageInfo.guid] = manageInfo;

            Save();
        }

        public ManageInfo AddManageInfo(string group, Object manageTarget)
        {
            if (manageTarget == null) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(manageTarget);

            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var ignoreType = GetIgnoreType(assetPath);
            
            var manageInfo = new ManageInfo()
            {
                guid = assetGuid,
                group = group,
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
            if (string.IsNullOrEmpty(assetPath)){ return null; }

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
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // 既に登録済み.
            var manageInfo = GetAssetManagedInfo(assetPath);

            if (manageInfo != null && manageInfo.guid == assetGuid)
            {
                Debug.LogWarningFormat("Already registered.\nGroup : {0}", manageInfo.group);
                return false;
            }

            // 除外対象.

            var ignoreType = GetIgnoreType(assetPath);

            if (ignoreType == IgnoreType.IgnoreManage)
            {
                Debug.LogWarning("This asset is ignore manage asset.");
                return false;
            }

            if (ignoreType == IgnoreType.IgnoreExtension)
            {
                Debug.LogWarning("This asset is ignore extension asset.");
                return false;
            }

            return true;
        }

        #endregion

        #region Group

        public void DeleteGroup(string group)
        {
            var targets = managedInfos.Values.Where(x => x.group == group).ToArray();

            for (var i = 0; i < targets.Length; i++)
            {
                var guid = targets[i].guid;

                managedInfos.Remove(guid);
            }

            Save();
        }

        public void RenameGroup(string from, string to)
        {
            var targets = managedInfos.Values.Where(x => x.group == from).ToArray();

            foreach (var target in targets)
            {
                target.group = to;
            }

            Save();
        }

        #endregion

        public void Save()
        {
            var managedAssets = ManagedAssets.Instance;

            var infos = managedInfos.Values.ToArray();

            managedAssets.SetManageInfos(infos);

            Prefs.manifestUpdateRequest = true;
        }

        /// <summary> 除外対象のパスか検証 </summary>
        public IgnoreType? GetIgnoreType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)){ return null; }

            var manageConfig = ExternalAssetConfig.Instance;

            if (manageConfig == null){ return null; }

            if (ignoreManagePaths == null)
            {
                ignoreManagePaths = manageConfig.IgnoreManage
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            if (ignoreAssetBundlePaths == null)
            {
                ignoreAssetBundlePaths = manageConfig.IgnoreAssetBundle
                    .Select(x => AssetDatabase.GetAssetPath(x))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .OrderByDescending(x => x.Length)
                    .ToArray();
            }

            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            if (string.IsNullOrEmpty(assetPath)){ return null; }

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

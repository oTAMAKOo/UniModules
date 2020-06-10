
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public sealed class AssetManageManager : Singleton<AssetManageManager>
    {
        //----- params -----

        private const string AssetNameSeparator = "_";

        //----- field -----

        private string externalResourcesPath = null;

        private AssetManageConfig config = null;

        private Dictionary<string, AssetCollectInfo> assetCollectInfoByAssetPath = null;

        // 編集用の情報.
        private List<GroupInfo> groupInfos = new List<GroupInfo>();
        private List<ManageInfo> manageInfos = new List<ManageInfo>();

        // Tuple<管理アセットのパス, 管理情報>をパスの長い順に抽出したキャッシュ.
        private Tuple<string, ManageInfo>[] manageInfoSearchCache = null;

        private bool initialized = false;

        //----- property -----

        public string ExternalResourcesPath { get { return externalResourcesPath; } }

        public IEnumerable<GroupInfo> GroupInfos { get { return groupInfos; } }
        public IEnumerable<ManageInfo> ManageInfos { get { return manageInfos; } }

        //----- method -----

        private AssetManageManager()
        {
            assetCollectInfoByAssetPath = new Dictionary<string, AssetCollectInfo>();
        }

        public void Initialize(string externalResourcesPath, AssetManageConfig config)
        {
            if (initialized) { return; }

            this.externalResourcesPath = externalResourcesPath;
            this.config = config;

            groupInfos = new List<GroupInfo>(config.GroupInfos);
            manageInfos = new List<ManageInfo>(config.ManageInfos);

            initialized = true;
        }

        public IEnumerable<AssetInfo> GetAllAssetInfos()
        {
            return GetAllAssetCollectInfo()
                .Where(x => x.AssetInfo != null)
                .Select(x => x.AssetInfo);
        }

        public IEnumerable<AssetCollectInfo> GetAllAssetCollectInfo()
        {
            return assetCollectInfoByAssetPath.Values;
        }

        public void CollectInfo()
        {
            var progress = new ScheduledNotifier<Tuple<string, float>>();

            progress.Subscribe(
                    x =>
                    {
                        EditorUtility.DisplayProgressBar("Collect asset info", x.Item1, x.Item2);
                    })
                .AddTo(Disposable);

            assetCollectInfoByAssetPath.Clear();

            CollectInfo(externalResourcesPath, progress);

            EditorUtility.ClearProgressBar();
        }

        public AssetCollectInfo[] CollectInfo(string targetAssetPath, IProgress<Tuple<string, float>> progress = null)
        {
            var list = new List<AssetCollectInfo>();

            var assetPaths = new string[0];

            if (AssetDatabase.IsValidFolder(targetAssetPath))
            {
                assetPaths = AssetDatabase.FindAssets(null, new string[] { targetAssetPath })
                    .Distinct()
                    .Select(x => AssetDatabase.GUIDToAssetPath(x))
                    .ToArray();
            }
            else
            {
                assetPaths = new string[] { targetAssetPath };
            }

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];

                if (AssetDatabase.IsValidFolder(assetPath)) { continue; }

                if(progress != null)
                {
                    progress.Report(Tuple.Create(assetPath, (float)i / assetPaths.Length));
                }

                var info = CreateAssetCollectInfo(assetPath);

                assetCollectInfoByAssetPath[assetPath] = info;

                list.Add(info);
            }

            return list.ToArray();
        }

        public AssetCollectInfo CreateAssetCollectInfo(string assetPath)
        {
            var assetInfo = GetAssetInfo(assetPath);
            var manageInfo = GetManageInfo(assetPath);
            var ignoreType = GetIgnoreType(assetPath);

            var assetCollectInfo = new AssetCollectInfo(this, assetPath, assetInfo, manageInfo, ignoreType);

            return assetCollectInfo;
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

        public bool SetAssetBundleName(string assetPath, string assetBundleName)
        {
            var change = false;

            // アセットバンドル名を割り振り.
            var importer = AssetImporter.GetAtPath(assetPath);

            // バリアントは使わない.
            if (!string.IsNullOrEmpty(importer.assetBundleVariant))
            {
                importer.assetBundleVariant = string.Empty;

                importer.SaveAndReimport();

                change = true;
            }

            // アセットバンドル名設定.
            if (importer.assetBundleName != assetBundleName)
            {
                importer.assetBundleName = assetBundleName;

                EditorUtility.SetDirty(importer);

                change = true;
            }

            return change;
        }

        #region GroupInfo

        public void AddGroupInfo(GroupInfo groupInfo)
        {
            if (groupInfos.Any(x => x.groupName == groupInfo.groupName))
            {
                return;
            }

            groupInfos.Add(groupInfo);

            SaveConfigAsset();
        }

        public void RenameGroupInfo(string from, string to)
        {
            foreach (var groupInfo in groupInfos)
            {
                if (groupInfo.groupName == from)
                {
                    groupInfo.groupName = to;
                }
            }

            SaveConfigAsset();
        }

        public void DeleteGroupInfo(GroupInfo groupInfo)
        {
            var manageInfos = GetGroupManageInfo(groupInfo.groupName);

            foreach (var manageInfo in manageInfos)
            {
                DeleteManageInfo(groupInfo.groupName, manageInfo);
            }

            groupInfos.RemoveAll(x => x.groupName == groupInfo.groupName);

            SaveConfigAsset();
        }

        public string[] GetGroupAssetPaths(GroupInfo groupInfo)
        {
            var assetPaths = new List<string>();

            var manageInfos = GetGroupManageInfo(groupInfo.groupName);

            if (manageInfos.Any())
            {
                var allAssetPaths = GetAllAssetInfos()
                    .Select(x => PathUtility.Combine(ExternalResourcesPath, x.ResourcePath))
                    .Distinct()
                    .ToArray();

                foreach (var manageInfo in manageInfos)
                {
                    var manageAssetPaths = allAssetPaths.Where(x => GetManageInfo(x) == manageInfo);

                    assetPaths.AddRange(manageAssetPaths);
                }
            }

            return assetPaths.ToArray();
        }

        private string GetAssetGroupName(ManageInfo manageInfo)
        {
            if (manageInfo == null) { return null; }

            var asset = manageInfo.assetObject;

            var groupInfo = groupInfos.FirstOrDefault(x => x.manageTargetAssets.Contains(asset));

            return groupInfo != null ? groupInfo.groupName : null;
        }

        #endregion

        #region ManageInfo

        public void UpdateManageInfo(ManageInfo manageInfo)
        {
            var count = manageInfos.RemoveAll(x => x.assetObject == manageInfo.assetObject);

            if (0 < count)
            {
                manageInfos.Add(new ManageInfo(manageInfo));

                SaveConfigAsset();

                manageInfoSearchCache = null;
            }
        }

        public ManageInfo AddManageInfo(string groupName, Object manageAsset)
        {
            if (manageAsset == null) { return null; }

            var manageAssetPath = AssetDatabase.GetAssetPath(manageAsset);

            var ignore = GetIgnoreType(manageAssetPath);
            
            var manageInfo = new ManageInfo(manageAsset)
            {
                isAssetBundle = ignore != IgnoreType.IgnoreAssetBundle,                
            };

            manageInfos.Add(manageInfo);

            var groupInfo = groupInfos.FirstOrDefault(x => x.groupName == groupName);

            if (!groupInfo.manageTargetAssets.Contains(manageInfo.assetObject))
            {
                groupInfo.manageTargetAssets.Add(manageInfo.assetObject);
            }

            SaveConfigAsset();

            manageInfoSearchCache = null;

            return manageInfo;
        }

        public void DeleteManageInfo(string groupName, ManageInfo manageInfo)
        {
            if (manageInfo == null) { return; }

            manageInfos.RemoveAll(x => x.assetObject == manageInfo.assetObject);

            var groupInfo = groupInfos.FirstOrDefault(x => x.groupName == groupName);

            if (groupInfo.manageTargetAssets.Contains(manageInfo.assetObject))
            {
                groupInfo.manageTargetAssets.Remove(manageInfo.assetObject);
            }

            SaveConfigAsset();

            manageInfoSearchCache = null;
        }

        public ManageInfo[] GetGroupManageInfo(string groupName)
        {
            var groupInfo = groupInfos.FirstOrDefault(x => x.groupName == groupName);

            if (groupInfo == null) { return new ManageInfo[0]; }

            return manageInfos
                .Where(x => groupInfo.manageTargetAssets.Any(y => y == x.assetObject))
                .ToArray();
        }

        public bool ValidateManageInfo(Object assetObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(assetObject);

            // 既に登録済み.
            var manageTargetAssets = groupInfos
                .SelectMany(x => x.manageTargetAssets)
                .ToArray();

            if (manageTargetAssets.Contains(assetObject))
            {
                Debug.Log("既に登録済みです.");
                return false;
            }

            // 除外対象.

            var ignoreType = GetIgnoreType(assetPath);

            if (ignoreType == IgnoreType.IgnoreManage)
            {
                Debug.Log("除外対象に設定されています.");
                return false;
            }

            if (ignoreType == IgnoreType.IgnoreExtension)
            {
                Debug.Log("除外対象の拡張子です.");
                return false;
            }


            return true;
        }

        private ManageInfo GetManageInfo(string assetPath)
        {
            // Tuple<管理アセットのパス, 管理情報>をパスの長い順に抽出.
            if (manageInfoSearchCache == null)
            {
                manageInfoSearchCache = manageInfos
                    .Select(x => Tuple.Create(AssetDatabase.GetAssetPath(x.assetObject), x))
                    .OrderByDescending(x => x.Item1.Length)
                    .ToArray();
            }

            // 除外対象.                
            var ignoreType = GetIgnoreType(assetPath);

            // 管理対象外 or 除外対象のフォルダ名が含まれている.
            var ignore = ignoreType.HasValue && (ignoreType == IgnoreType.IgnoreManage || ignoreType == IgnoreType.IgnoreFolder);

            if (!ignore)
            {
                // 最も深い階層まで一致する情報を使用.
                foreach (var info in manageInfoSearchCache)
                {
                    // フォルダ名の途中まで一致でも適合扱いされてしまう為フォルダの場合は/を追加.

                    var path1 = AssetDatabase.IsValidFolder(assetPath) ?
                        assetPath + PathUtility.PathSeparator :
                        assetPath;

                    var path2 = AssetDatabase.IsValidFolder(info.Item1) ?
                        info.Item1 + PathUtility.PathSeparator :
                        info.Item1;

                    if (path1.StartsWith(path2))
                    {
                        return info.Item2;
                    }
                }
            }

            return null;
        }

        #endregion

        #region AssetInfo

        public AssetInfo GetAssetInfo(string assetPath)
        {
            // フォルダは収集しない.
            if (AssetDatabase.IsValidFolder(assetPath)) { return null; }

            var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);

            // metaファイルがある場合は管理下のAsset.
            if (!string.IsNullOrEmpty(metaPath))
            {
                var manageInfo = GetManageInfo(assetPath);

                if (manageInfo != null)
                {
                    var loadPath = GetAssetLoadPath(assetPath);
                    var groupName = GetAssetGroupName(manageInfo);
                    var tag = manageInfo.tag;

                    var assetInfo = new AssetInfo(loadPath, groupName, tag);

                    if (manageInfo.isAssetBundle)
                    {
                        var assetBundleName = GetAssetBundleName(assetPath, manageInfo);

                        var assetBundleInfo = new AssetBundleInfo(assetBundleName);
                        
                        assetInfo.SetAssetBundleInfo(assetBundleInfo);
                    }

                    return assetInfo;
                }
            }

            return null;
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

            if (manageInfo.assetObject == null) { return string.Empty; }

            if (!manageInfo.isAssetBundle) { return string.Empty; }

            // グループ名設定.
            var assetGroupName = GetAssetGroupName(manageInfo);

            if (!string.IsNullOrEmpty(assetGroupName))
            {
                assetBundleName = assetGroupName + PathUtility.PathSeparator;
            }

            // 管理アセットの親フォルダパス.
            var managePath = AssetDatabase.GetAssetPath(manageInfo.assetObject);
            var parentDir = PathUtility.ConvertPathSeparator(Directory.GetParent(managePath).ToString() + PathUtility.PathSeparator);
            var externalResourcesDir = externalResourcesPath + PathUtility.PathSeparator;
            var folder = parentDir.Substring(externalResourcesDir.Length);

            assetBundleName += folder.Replace(PathUtility.PathSeparator.ToString(), AssetNameSeparator);

            switch (manageInfo.assetBundleNameType)
            {
                case ManageInfo.NameType.ManageAssetName:
                    assetBundleName += Path.GetFileNameWithoutExtension(managePath);
                    break;

                case ManageInfo.NameType.ChildAssetName:
                case ManageInfo.NameType.PrefixAndChildAssetName:
                    {
                        assetBundleName += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;

                        if (AssetDatabase.IsValidFolder(managePath))
                        {
                            folder += Path.GetFileNameWithoutExtension(managePath) + AssetNameSeparator;
                        }

                        var resourcePath = assetPath.Substring((externalResourcesDir + folder).Length);

                        switch (manageInfo.assetBundleNameType)
                        {
                            case ManageInfo.NameType.ChildAssetName:
                                assetBundleName += Path.GetFileNameWithoutExtension(resourcePath);
                                break;

                            case ManageInfo.NameType.PrefixAndChildAssetName:
                                assetBundleName += manageInfo.assetBundleNameStr + AssetNameSeparator + Path.GetFileNameWithoutExtension(resourcePath);
                                break;
                        }
                    }
                    break;

                case ManageInfo.NameType.Specified:
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

        #endregion

        #region IgnoreInfo

        public IgnoreType? GetIgnoreType(string assetPath)
        {
            return config.IgnoreInfo.GetIgnoreType(assetPath);
        }

        #endregion

        private void SaveConfigAsset()
        {
            // 後から除外設定などを書き換えた時の為に初期化時に更新を行う.
            foreach (var item in manageInfos)
            {
                if (item == null) { continue; }

                if (item.assetObject == null) { continue; }

                var assetPath = AssetDatabase.GetAssetPath(item.assetObject);

                var manageInfo = GetManageInfo(assetPath);
                var ignoreType = GetIgnoreType(assetPath);

                if (manageInfo == null)
                {
                    manageInfo = new ManageInfo(item.assetObject);
                }

                manageInfo.isAssetBundle = !ignoreType.HasValue || ignoreType != IgnoreType.IgnoreAssetBundle;
            }

            Reflection.SetPrivateField(config, "groupInfos", groupInfos);
            Reflection.SetPrivateField(config, "manageInfos", manageInfos);

            // 最適化.
            config.Optimisation();

            // 保存.
            UnityEditorUtility.SaveAsset(config);
        }
    }
}

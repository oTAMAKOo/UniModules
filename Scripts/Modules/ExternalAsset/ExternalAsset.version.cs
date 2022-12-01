
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Extensions;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;

#endif

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, string> versions = null;

        //----- property -----

        //----- method -----

        /// <summary>
        /// アセットバンドルのバージョンが最新か確認.
        /// (同梱された別アセットが更新された場合でもtrueを返す)
        /// </summary>
        private bool CheckAssetBundleVersion(AssetInfo assetInfo)
        {
            var filePath = assetBundleManager.GetFilePath(assetInfo);

            // ※ シュミレート時はpackageファイルをダウンロードしていないので常にファイルが存在しない.

            if (!simulateMode)
            {
                // ファイルがない.
                if (!File.Exists(filePath)) { return false; }
            }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var infos = assetInfosByAssetBundleName.FirstOrDefault(x => x.Key == assetInfo.AssetBundle.AssetBundleName);

            if (infos == null) { return false; }

            foreach (var info in infos)
            {
                var hash = versions.GetValueOrDefault(info.FileName);

                // ローカルにバージョンが存在しない.
                if (string.IsNullOrEmpty(hash)) { return false; }

                // アセットバンドル内のアセットが更新されている.
                if (hash != info.Hash) { return false; }
            }

            return true;
        }

        /// <summary> アセットバンドル以外のアセットの更新が必要か確認. </summary>
        private bool CheckAssetVersion(string resourcePath, string filePath)
        {
            // ファイルがない.
            if (!File.Exists(filePath)) { return false; }

            // バージョン情報が存在しない.
            if (versions.IsEmpty()) { return false; }

            var assetInfo = GetAssetInfo(resourcePath);

            // アセット管理情報内に存在しないので最新扱い.
            if (assetInfo == null) { return true; }

            // バージョン不一致.

            var hash = versions.GetValueOrDefault(assetInfo.FileName);
            
            return hash == assetInfo.Hash;
        }

        /// <summary> 更新が必要なアセット情報を取得. </summary>
        public IReadOnlyList<AssetInfo> GetRequireUpdateAssetInfos(string groupName = null)
        {
            if (simulateMode){ return new AssetInfo[0]; }

            var assetInfos = assetInfoManifest.GetAssetInfos(groupName);

            // バージョン情報読み込み.

            if (versions == null)
            {
                LoadVersion();
            }

            // バージョン情報が存在しないので全更新.
            if (versions.IsEmpty()) { return assetInfos.ToArray(); }

            return assetInfos.Where(x => IsRequireUpdate(x)).ToArray();
        }

		/// <summary> 更新が必要か. </summary>
		public bool IsRequireUpdate(AssetInfo assetInfo)
        {
            if (simulateMode){ return false; }

            // バージョン情報が存在しないので更新.
            if (versions.IsEmpty()) { return true; }

            var requireUpdate = true;

            #if ENABLE_CRIWARE_FILESYSTEM

            var extension = Path.GetExtension(assetInfo.ResourcePath);

            if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
            {
                var filePath = ConvertCriFilePath(assetInfo.ResourcePath);

                requireUpdate = !CheckAssetVersion(assetInfo.ResourcePath, filePath);
            }
            else

            #endif

            {
                requireUpdate = !CheckAssetBundleVersion(assetInfo);
            }            

            return requireUpdate;
        }

        private void UpdateVersion(string resourcePath)
        {
            if (simulateMode){ return; }

            // ※ 古いバージョン情報を破棄して最新のバージョン情報を追加.
                
            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                Debug.LogWarningFormat("AssetInfo not found.\n{0}", resourcePath);
                return;
            }

            var updateVersions = new Dictionary<string, string>();

            // アセットバンドル.
            if (assetInfo.IsAssetBundle)
            {
                var allAssetInfos = assetInfoManifest.GetAssetInfos();

                // 同じアセットバンドル内のバージョンも更新.
                var assetBundle = allAssetInfos
                    .Where(x => x.IsAssetBundle)
                    .Where(x => x.AssetBundle.AssetBundleName == assetInfo.AssetBundle.AssetBundleName)
                    .GroupBy(x => x.AssetBundle.AssetBundleName)
                    .FirstOrDefault();

                foreach (var item in assetBundle)
                {
                    if (string.IsNullOrEmpty(item.FileName)){ continue; }

                    if (string.IsNullOrEmpty(item.Hash)){ continue; }

                    versions[item.FileName] = item.Hash;

                    updateVersions[item.FileName] = item.Hash;
                }
            }
            // アセットバンドル以外.
            else
            {
                if (!string.IsNullOrEmpty(assetInfo.FileName) && !string.IsNullOrEmpty(assetInfo.Hash))
                {
                    versions[assetInfo.FileName] = assetInfo.Hash;

                    updateVersions[assetInfo.FileName] = assetInfo.Hash;
                }
            }

            // ※ バージョン文字列だけのデータなので暗号化は行わない.
            foreach (var item in updateVersions)
            {
                if (string.IsNullOrEmpty(item.Value)){ continue; }

                var filePath = PathUtility.Combine(InstallDirectory, item.Key);

                var versionFilePath = filePath + AssetInfoManifest.VersionFileExtension;

                try
                {
                    var bytes = Encoding.UTF8.GetBytes(item.Value);

                    File.WriteAllBytes(versionFilePath, bytes);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    if (File.Exists(versionFilePath))
                    {
                        File.Delete(versionFilePath);
                    }
                }
            }
        }

        public void LoadVersion()
        {
            if (simulateMode){ return; }

            versions = new Dictionary<string, string>();

            var versionFilePaths = GetAllVersionFilePaths();

            var versionFileExtensionLength = AssetInfoManifest.VersionFileExtension.Length;

            foreach (var versionFilePath in versionFilePaths)
            {
                try
                {
                    var bytes = File.ReadAllBytes(versionFilePath);

                    var hash = Encoding.UTF8.GetString(bytes);

                    var versionFileName = Path.GetFileName(versionFilePath);

                    var fileName = versionFileName.SafeSubstring(0, versionFileName.Length - versionFileExtensionLength);

                    if (string.IsNullOrEmpty(fileName)){ continue; }

                    versions[fileName] = hash;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    if (File.Exists(versionFilePath))
                    {
                        File.Delete(versionFilePath);
                    }
                }
            }
        }

        private void ClearVersion()
        {
            if (simulateMode){ return; }

            if (versions != null)
            {
                versions.Clear();
            }

            var versionFilePaths = GetAllVersionFilePaths();

            foreach (var versionFilePath in versionFilePaths)
            {
                try
                {
                    var cFileInfo = new FileInfo(versionFilePath);

                    // 読み取り専用属性がある場合は、読み取り専用属性を解除.
                    if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        cFileInfo.Attributes = FileAttributes.Normal;
                    }

                    File.Delete(versionFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private string[] GetAllVersionFilePaths()
        {
			if (!Directory.Exists(InstallDirectory)){ return new string[0]; }

            var directoryInfo = new DirectoryInfo(InstallDirectory);

            var files = directoryInfo.GetFiles("*" + AssetInfoManifest.VersionFileExtension, SearchOption.AllDirectories);

            return files.Select(x => PathUtility.ConvertPathSeparator(x.FullName)).ToArray();
        }
    }
}

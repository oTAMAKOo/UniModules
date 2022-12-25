
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions;
using Modules.Devkit.Project;
using Modules.ExternalAssets;

namespace Modules.AssetBundles.Editor
{
    public sealed class BuildAssetBundle
    {
        //----- params -----
        
        private const string AssetBundleCacheFolder = "AssetBundleBuildCache";

        //----- field -----

        private IBuildAssetBundlePipeline bundlePipeline = null;

        //----- property -----

        //----- method -----

        public BuildAssetBundle(IBuildAssetBundlePipeline bundlePipeline)
        {
            this.bundlePipeline = bundlePipeline;
        }

		/// <summary> 全てのアセットバンドルをビルド </summary>
        public BuildResult BuildAllAssetBundles()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            if (!Directory.Exists(assetBundlePath))
            {
                Directory.CreateDirectory(assetBundlePath);
            }
            
            return bundlePipeline.Build(assetBundlePath);
        }

        /// <summary> 未登録のアセットバンドル情報追加 </summary>
        public void AddUnregisteredAssetInfos(AssetInfoManifest assetInfoManifest, BuildResult buildResult)
        {
            var assetBundleAssetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .ToArray();

            foreach (var bundleInfo in buildResult.BundleBuildResults.BundleInfos)
            {
                var assetBundleName = bundleInfo.Key;

                // マニフェストファイルは登録しない.
                if (AssetInfoManifest.AssetBundleName == assetBundleName){ continue; }

                // 既に登録済み.
                if (assetBundleAssetInfos.Any(x => x.Key == assetBundleName)){ continue; }
                
                var fileName = Path.GetFileName(bundleInfo.Value.FileName);

                var assetInfo = new AssetInfo(string.Empty, fileName, "(undefined)", null);

                var assetBundleInfo = new AssetBundleInfo(assetBundleName);

                assetInfo.SetAssetBundleInfo(assetBundleInfo);
                
                assetInfoManifest.AddAssetInfo(assetInfo);
            }
        }

        /// <summary> アセットバンドルの参照情報を書き込み </summary>
        public void SetDependencies(AssetInfoManifest assetInfoManifest, BuildResult buildResult)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos().Where(x => x.IsAssetBundle);

            foreach (var assetInfo in assetInfos)
            {
                if (!assetInfo.IsAssetBundle){ continue; }

                var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                var detail = buildResult.GetDetails(assetBundleName);
                
                if (!detail.HasValue)
                {
                    throw new InvalidDataException("AssetBundle build info not found. : " + assetBundleName);
                }

                assetInfo.AssetBundle.SetDependencies(detail.Value.Dependencies);
            }
        }

        /// <summary> アセットのルートハッシュ情報を書き込み </summary>
        public void SetAssetInfoHash(AssetInfoManifest assetInfoManifest)
        {
            // 文字数が大きくなりすぎないように300ファイル分毎に分割.
            var chunkInfos = assetInfoManifest.GetAssetInfos().Chunk(300);

            var versionHashBuilder = new StringBuilder();

            foreach (var assetInfos in chunkInfos)
            {
                var hashBuilder = new StringBuilder();

                foreach (var assetInfo in assetInfos)
                {
                    hashBuilder.AppendLine(assetInfo.Hash);
                }

                var hash = hashBuilder.ToString().GetHash();

                versionHashBuilder.AppendLine(hash);
            }

            var versionHash = versionHashBuilder.ToString().GetHash();

            Reflection.SetPrivateField(assetInfoManifest, "versionHash", versionHash);
        }

        /// <summary> 情報書き込み後のAssetInfoManifestをビルド </summary>
        public void BuildAssetInfoManifest()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return; }

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            var assetBundlePath = GetAssetBundleOutputPath();

            var manifestPath = PathUtility.Combine(externalAssetPath, AssetInfoManifest.ManifestFileName);

            // 必ず更新するのでパッケージファイルを削除.            
            var manifestFullPath = PathUtility.Combine(assetBundlePath, AssetInfoManifest.ManifestFileName);
            var manifestFilePackage = Path.ChangeExtension(manifestFullPath, AssetBundleManager.PackageExtension);

            if (File.Exists(manifestFilePackage))
            {
                File.Delete(manifestFilePackage);
            }
            
            var buildMap = new AssetBundleBuild[]
            {
                new AssetBundleBuild()
                {
                    assetNames = new string[] { manifestPath },
                    assetBundleName = AssetInfoManifest.AssetBundleName,
                },
            };
            
            bundlePipeline.Build(assetBundlePath, buildMap);
        }

        /// <summary> 不要になったアセットバンドルを削除 </summary>
        public void CleanUnUseAssetBundleFiles(BuildResult buildResult)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = buildResult.BundleBuildResults.BundleInfos
                .Select(x => x.Key)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file);

                if (extension == AssetBundleManager.PackageExtension) { continue; }

                if (assetBundleNames.Any(x => file.EndsWith(x))) { continue; }

                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                var packageFilePath = file + AssetBundleManager.PackageExtension;

                if (File.Exists(packageFilePath))
                {
                    File.Delete(packageFilePath);
                }
            }

            // 空フォルダ削除.
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                DirectoryUtility.DeleteEmpty(assetBundlePath);
            }
        }

        /// <summary> キャッシュ済みアセットバンドルファイルの最終更新日テーブルを取得 </summary>
        public Dictionary<string, DateTime> GetCachedFileLastWriteTimeTable()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames()
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == string.Empty)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var dictionary = new Dictionary<string, DateTime>();

            foreach (var file in allFiles)
            {
                var path = PathUtility.ConvertPathSeparator(file);

				if (assetBundleNames.All(x => !path.EndsWith(x))) { continue; }

				if (File.Exists(path))
				{
					var lastWriteTime = File.GetLastWriteTime(path);

					lock (dictionary)
					{
						dictionary.Add(path, lastWriteTime);
					}
				}
            }

            return dictionary;
        }

        public AssetInfo[] GetAllTargetAssetInfo(AssetInfoManifest assetInfoManifest)
        {
			var assetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .Where(x => !string.IsNullOrEmpty(x.AssetBundle.AssetBundleName))
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToList();

            assetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

            return assetInfos.ToArray();
        }

        /// <summary> 更新されたアセット情報取得 </summary>
        public AssetInfo[] GetUpdateTargetAssetInfo(AssetInfoManifest assetInfoManifest, Dictionary<string, DateTime> lastWriteTimeTable)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetInfos = GetAllTargetAssetInfo(assetInfoManifest);

            var list = new List<AssetInfo>();
			
            foreach (var item in assetInfos)
            {
                var assetInfo = item;

				// アセットバンドルファイルパス.
				var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);
                    
				if (File.Exists(assetBundleFilePath))
				{
					// 最終更新日を比較.

					var prevLastWriteTime = lastWriteTimeTable.GetValueOrDefault(assetBundleFilePath, DateTime.MinValue);

					var currentLastWriteTime = File.GetLastWriteTime(assetBundleFilePath);

					// ビルド前と後で更新日時が変わっていたら更新対象.
					if (currentLastWriteTime != prevLastWriteTime)
					{
						lock (list)
						{
							list.Add(assetInfo);
						}
					}
				}
            }

            return list.ToArray();
        }

		public static string GetAssetBundleOutputPath()
		{
			var projectPath = UnityPathUtility.GetProjectFolderPath();
			var folderName = PlatformUtility.GetPlatformTypeName();

			var paths = new string[] { projectPath, UnityPathUtility.LibraryFolder, AssetBundleCacheFolder, folderName };

			var assetBundlePath = PathUtility.Combine(paths);

			if (!Directory.Exists(assetBundlePath))
			{
				Directory.CreateDirectory(assetBundlePath);
			}

			return assetBundlePath;
		}
    }
}


using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Amazon.S3;
using Modules.AssetBundles;
using Modules.Devkit.Project;

namespace Modules.ExternalResource
{
    public abstract class S3Uploader : S3UploaderBase
    {
        //----- params -----

		/// <summary> ローカルにあるファイル情報 </summary>
        protected sealed class FileInfo
        {
            public string FilePath { get; set; }

            public string ObjectPath { get; set; }
		}

		//----- field -----

        private string folderPath = null;
        private string bucketFolder = null;

        private string[] files = null;

        private AssetInfoManifest assetInfoManifest = null;
        private string assetInfoManifestFilePath = null;

        //----- property -----

		public abstract IAssetBundleFileHandler FileHandler { get; }

        //----- method -----

        public async UniTask<string> Execute(string folderPath, string bucketFolder)
        {
            var versionHash = string.Empty;

            this.folderPath = folderPath;

            // ファイルパス : オブジェクトパスのディクショナリ作成.
            files = Directory.GetFiles(folderPath)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            using (new DisableStackTraceScope(LogType.Log))
            {
                AssetBundle.UnloadAllAssetBundles(true);

                try
                {
                    //------- AssetInfoManifest.package読み込み -------

                    await LoadAssetInfoManifest();

                    if (assetInfoManifest == null)
                    {
                        Debug.LogError("AssetInfoManifest.package file load error.");

                        return null;
                    }

                    versionHash = assetInfoManifest.VersionHash;

                    if (string.IsNullOrEmpty(versionHash))
                    {
                        throw new InvalidDataException("VersionHash is empty.");
                    }

					var folder = PathUtility.Combine(bucketFolder, versionHash);

                    this.bucketFolder = BucketFolderOverride(folder);

                    //------- アップロードファイル情報作成 -------

                    var fileInfos = BuildUploadFileInfos();

                    //------- S3クライアント作成 -------

                    CreateS3Client();

                    //------- S3のファイル一覧取得 -------

                    var s3Objects = await GetUploadedObjects();

					//------- アップロード先のフォルダ内ファイル削除 -------

                    await DeleteDeletedPackageFromS3(s3Objects);

                    //------- ファイルをS3にアップロード -------

                    await UploadPackagesToS3(fileInfos);
                }
                catch (Exception e)
                {
					Debug.LogErrorFormat("S3 upload process error. \n{0}", e);

                    return null;
                }
            }

            return versionHash;
        }

		private string GetAssetInfoManifestFilePath(string[] files)
        {
            var assetInfoManifestFileName = Path.ChangeExtension(AssetInfoManifest.ManifestFileName, AssetBundleManager.PackageExtension);
            var assetInfoManifestFilePath = files.FirstOrDefault(x => Path.GetFileName(x) == assetInfoManifestFileName);

            return assetInfoManifestFilePath;
        }

        /// <summary> AssetInfoManifest読み込み </summary>
        private async UniTask LoadAssetInfoManifest()
        {
            Debug.Log("Load AssetInfoManifest.package.");

            var projectResourceFolders = ProjectResourceFolders.Instance;

            assetInfoManifestFilePath = GetAssetInfoManifestFilePath(files);

			if (!File.Exists(assetInfoManifestFilePath))
			{
				throw new FileNotFoundException(assetInfoManifestFilePath);
			}

			var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

            var assetPath = PathUtility.Combine(projectResourceFolders.ExternalResourcesPath, manifestAssetInfo.ResourcePath);

			var bytes = new byte[0];

            using (var fileStream = new FileStream(assetInfoManifestFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
				bytes = new byte[fileStream.Length];

				fileStream.Read(bytes, 0, bytes.Length);
			}

			bytes = FileHandler.Decode(bytes);

			var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes);

			while (!bundleLoadRequest.isDone)
			{
				await UniTask.Delay(25);
			}

			var assetBundle = bundleLoadRequest.assetBundle;

			var loadAssetAsync = assetBundle.LoadAssetAsync(assetPath, typeof(AssetInfoManifest));

			assetInfoManifest = loadAssetAsync.asset as AssetInfoManifest;

			assetBundle.Unload(false);
        }
        
        /// <summary> アップロードするファイルデータ構築. </summary>
        private FileInfo[] BuildUploadFileInfos()
        {
            Debug.Log("Build upload file infos.");

            var fileInfos = new List<FileInfo>();

            var folderPathLength = folderPath.Length;

            var allAssetInfos = assetInfoManifest.GetAssetInfos().ToArray();

            var fileHashTable = new Dictionary<string, string>();

            var assetInfoManifestFileName = Path.GetFileNameWithoutExtension(AssetInfoManifest.ManifestFileName);

            foreach (var assetInfo in allAssetInfos)
            {
                var fileName = Path.GetFileNameWithoutExtension(assetInfo.FileName);

                if (fileHashTable.ContainsKey(fileName)){ continue; }

                fileHashTable.Add(fileName, assetInfo.Hash);
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

				var path = file.SafeSubstring(folderPathLength);

                var objectPath = PathUtility.Combine(bucketFolder, path);

                var fileInfo = new FileInfo()
                {
                    FilePath = file,
                    ObjectPath = objectPath,
                };

                fileInfos.Add(fileInfo);
            }

            return fileInfos.ToArray();
        }

        /// <summary> アップロード済みのオブジェクト一覧取得. </summary>
        private async UniTask<S3Object[]> GetUploadedObjects()
        {
            Debug.Log("Get s3 object list.");

            var allBucketObjects = await s3Client.GetObjectList(bucketFolder);

            var s3Objects = allBucketObjects
                // アップロード先フォルダ内の一覧.
                .Where(x => x.Key.StartsWith(bucketFolder))
                // アップロード先フォルダは除外.
                .Where(x => x.Key != bucketFolder)
                .ToArray();

            return s3Objects;
        }
		
		/// <summary> ファイルをS3にアップロード </summary>
        private async UniTask UploadPackagesToS3(FileInfo[] fileInfos)
        {
			// 重複除外・順番をランダム化.

            var uploadTargets = fileInfos.Distinct().Shuffle().ToList();

            // アップロード.

            if (uploadTargets.Any())
            {
                var isBatchMode = Application.isBatchMode;
				
                var logBuilder = new StringBuilder();

                using (new DisableStackTraceScope(LogType.Log))
                {
                    Debug.LogFormat("Uploading {0} files to s3 {1}", uploadTargets.Count, bucketFolder);

                    // ファイルをアップロード.

                    const long PartSize = 5 * 1024 * 1024; // 5MB単位.

					var chunkedUploadTargets = uploadTargets.Chunk(50);
					
                    foreach (var items in chunkedUploadTargets)
                    {
						var tasks = new List<UniTask>();

						logBuilder.Clear();

						foreach (var item in items)
						{
							var uploadTarget = item;

	                        var task = UniTask.RunOnThreadPool(async () =>
	                        {
	                            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
	                            {
	                                FilePath = uploadTarget.FilePath,
	                                StorageClass = S3StorageClass.StandardInfrequentAccess,
	                                PartSize = PartSize,
	                                Key = uploadTarget.ObjectPath,
	                                CannedACL = UploadFileCannedACL,
	                            };

								await s3Client.Upload(fileTransferUtilityRequest);
	                            
	                            if (isBatchMode)
	                            {
	                                Debug.LogFormat(uploadTarget.FilePath);
	                            }
	                            else
	                            {
	                                lock (logBuilder)
	                                {
	                                    logBuilder.AppendLine(uploadTarget.FilePath);
									}
	                            }
	                        });

	                        tasks.Add(task);
						}

						await UniTask.WhenAll(tasks.ToArray());

						if (!isBatchMode)
						{
							Debug.Log(logBuilder.ToString());
						}
                    }
				}
			}
        }

        /// <summary> 削除対象のファイルをS3から削除 </summary>
        private async UniTask DeleteDeletedPackageFromS3(S3Object[] s3Objects)
        {
			// 削除.

            var deleteObjectPaths = s3Objects.Select(x => x.Key).ToArray();

            if (deleteObjectPaths.Any())
            {
                Debug.LogFormat("Delete deleted {0} objects from s3 {1}.", bucketFolder, deleteObjectPaths.Length);

                await s3Client.DeleteObjects(deleteObjectPaths);
            }

            // ログ.

            if (deleteObjectPaths.Any())
            {
                var chunk = deleteObjectPaths.Chunk(100).ToArray();

                var num = chunk.Length;

                for (var i = 0; i < num; i++)
                {
                    var builder = new StringBuilder();

                    chunk[i].ForEach(x => builder.AppendLine(x));

                    Debug.LogFormat("Delete S3 objects. [{0}/{1}]\n{2}", i + 1, num, builder.ToString());
                }
            }
        }
	}
}

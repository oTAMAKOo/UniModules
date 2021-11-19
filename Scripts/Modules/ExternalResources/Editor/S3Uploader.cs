
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Extensions;
using Modules.Amazon.S3;
using Modules.AssetBundles;
using Amazon.S3.Transfer;

namespace Modules.ExternalResource.Editor
{
    public abstract class S3Uploader
    {
        //----- params -----

        protected const string MetaDataHashKey = "x-amz-meta-hash";

        /// <summary> ローカルにあるファイル情報 </summary>
        protected sealed class FileInfo
        {
            public string FilePath { get; set; }

            public string ObjectPath { get; set; }

            public string Hash { get; set; }
        }

        /// <summary> S3にアップロード済みのファイル情報 </summary>
        protected sealed class S3FileInfo
        {
            public string ObjectPath { get; set; }

            public string Hash { get; set; }
        }

        //----- field -----

        private S3Client s3Client = null;

        private string folderPath = null;
        private string bucketFolder = null;

        private string[] files = null;

        private AssetInfoManifest assetInfoManifest = null;
        private string assetInfoManifestFilePath = null;

        //----- property -----

        /// <summary> アップロードファイルのアクセス権 </summary>
        protected S3CannedACL UploadFileCannedACL { get; set; } = S3CannedACL.PublicRead;

        //----- method -----

        public async Task<string> Execute(string folderPath, string bucketFolder)
        {
            var versionHash = string.Empty;

            this.folderPath = folderPath;

            // ファイルパス : オブジェクトパスのディクショナリ作成.
            files = Directory.GetFiles(folderPath)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            using (new DisableStackTraceScope())
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

                    this.bucketFolder = PathUtility.Combine(bucketFolder, versionHash);

                    //------- アップロードファイル情報作成 -------

                    var fileInfos = BuildUploadFileInfos();

                    //------- S3クライアント作成 -------

                    s3Client = CreateS3Client();

                    //------- 新規・更新対象のファイルをS3にアップロード -------

                    await UploadPackagesToS3(fileInfos);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("S3 upload process error. \n{0}", e.Message);

                    return null;
                }
            }

            return versionHash;
        }

        private S3Client CreateS3Client()
        {
            var identityPoolId = GetIdentityPoolId();
            var bucketName = GetBucketName();
            var bucketRegion = GetBucketRegion();
            var credentialsRegion = GetCredentialsRegion();

            var s3Client = new S3Client(identityPoolId, bucketName, bucketRegion, credentialsRegion);

            return s3Client;
        }

        private string GetAssetInfoManifestFilePath(string[] files)
        {
            var assetInfoManifestFileName = Path.ChangeExtension(AssetInfoManifest.ManifestFileName, AssetBundleManager.PackageExtension);
            var assetInfoManifestFilePath = files.FirstOrDefault(x => Path.GetFileName(x) == assetInfoManifestFileName);

            return assetInfoManifestFilePath;
        }

        /// <summary> AssetInfoManifest読み込み </summary>
        private async Task LoadAssetInfoManifest()
        {
            Debug.Log("Load AssetInfoManifest.package.");

            assetInfoManifestFilePath = GetAssetInfoManifestFilePath(files);

            var aesCryptoKey = GetCryptoKey();

            var bytes = new byte[0];

            using (var fileStream = new FileStream(assetInfoManifestFilePath, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[fileStream.Length];

                fileStream.Read(bytes, 0, bytes.Length);
            }

            bytes = bytes.Decrypt(aesCryptoKey);

            var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bytes);

            while (!bundleLoadRequest.isDone)
            {
                await Task.Delay(25);
            }

            var assetBundle = bundleLoadRequest.assetBundle;

            var loadAssetAsync = assetBundle.LoadAssetAsync(AssetInfoManifest.ManifestFileName, typeof(AssetInfoManifest));

            while (!loadAssetAsync.isDone)
            {
                await Task.Delay(25);
            }

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

                fileHashTable.Add(fileName, assetInfo.FileHash);
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                var hash = string.Empty;

                if (assetInfoManifestFileName == fileName)
                {
                    hash = FileUtility.GetHash(file);
                }
                else
                {
                    hash = fileHashTable.GetValueOrDefault(fileName);
                }

                var path = file.SafeSubstring(folderPathLength);
                var objectPath = PathUtility.Combine(bucketFolder, path);

                var fileInfo = new FileInfo()
                {
                    FilePath = file,
                    ObjectPath = objectPath,
                    Hash = hash,
                };

                fileInfos.Add(fileInfo);
            }

            return fileInfos.ToArray();
        }

        /// <summary> ファイルをS3にアップロード </summary>
        private async Task UploadPackagesToS3(FileInfo[] fileInfos)
        {
            // 重複除外・パスが短い順にソート.

            Func<string, int> sortFunc = x =>
            {
                var path = PathUtility.ConvertPathSeparator(x);

                var separatorCount = path.Split(PathUtility.PathSeparator).Length;

                return separatorCount;
            };

            var uploadTargets = fileInfos
                .Distinct()
                .OrderBy(x => sortFunc.Invoke(x.ObjectPath))
                .ThenBy(x => x.ObjectPath.Length)
                .ToList();

            // アップロード.

            if (uploadTargets.Any())
            {
                Debug.LogFormat("Uploading {0} files to s3 {1}", uploadTargets.Count, bucketFolder);

                // ファイルをアップロード.

                const long PartSize = 5 * 1024 * 1024; // 5MB単位.

                var tasks = new List<Task>();

                foreach (var uploadTarget in uploadTargets)
                {
                    var task = Task.Run(async () =>
                    {
                        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                        {
                            FilePath = uploadTarget.FilePath,
                            StorageClass = S3StorageClass.StandardInfrequentAccess,
                            PartSize = PartSize,
                            Key = uploadTarget.ObjectPath,
                            CannedACL = UploadFileCannedACL,
                        };

                        if (!string.IsNullOrEmpty(uploadTarget.Hash))
                        {
                            fileTransferUtilityRequest.Metadata.Add(MetaDataHashKey, uploadTarget.Hash);
                        }

                        await s3Client.Upload(fileTransferUtilityRequest);
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks.ToArray());
            }

            // ログ.

            if (uploadTargets.Any())
            {
                var uploadObjectPaths = uploadTargets.Select(x => x.ObjectPath).ToArray();

                Action<int, int, string[]> logOutput = (index, num, targets) =>
                {
                    var builder = new StringBuilder();

                    targets.ForEach(x => builder.AppendLine(x));

                    Debug.LogFormat("Uploaded S3 objects. [{0}/{1}]\n{2}", index, num, builder.ToString());
                };

                ChunkAction(uploadObjectPaths, logOutput);
            }
        }

        private void ChunkAction<T>(IEnumerable<T> targets, Action<int, int, T[]> action)
        {
            // 100要素ずつに分割.
            var chunk = targets.Chunk(100).ToArray();

            var num = chunk.Length;

            for (var i = 0; i < num; i++)
            {
                action.Invoke(i + 1, num, chunk[i].ToArray());
            }
        }

        public abstract string GetIdentityPoolId();
        
        public abstract string GetBucketName();

        public abstract RegionEndpoint GetBucketRegion();

        public abstract RegionEndpoint GetCredentialsRegion();

        public abstract AesCryptoKey GetCryptoKey();
    }
}


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
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Extensions;
using Modules.Amazon.S3;
using Modules.AssetBundles;

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

                    this.bucketFolder = PathUtility.Combine(bucketFolder, versionHash);

                    //------- アップロードファイル情報作成 -------

                    var fileInfos = BuildUploadFileInfos();

                    //------- S3クライアント作成 -------

                    s3Client = CreateS3Client();

                    //------- S3のファイル一覧取得 -------

                    var s3Objects = await GetUploadedObjects();

                    //------- S3ファイル情報作成 -------

                    var s3FileInfos = await BuildUploadedObjectInfos(s3Objects);

                    //------- 削除対象のファイルをS3から削除 -------

                    await DeleteDeletedPackageFromS3(fileInfos, s3FileInfos);

                    //------- 新規・更新対象のファイルをS3にアップロード -------

                    await UploadPackagesToS3(fileInfos, s3FileInfos);
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

            var cryptoKey = GetCryptoKey();

            using (var fileStream = new FileStream(assetInfoManifestFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var aesStream = new SeekableCryptoStream(fileStream, cryptoKey))
                {
                    var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(aesStream);

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
            }
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

        /// <summary> アップロード済みのオブジェクト一覧取得. </summary>
        private async Task<S3Object[]> GetUploadedObjects()
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

        /// <summary> アップロード済みのファイルデータ構築. </summary>
        private async Task<S3FileInfo[]> BuildUploadedObjectInfos(S3Object[] s3Objects)
        {
            Debug.Log("Build uploaded objects info.");

            var s3FileInfos = new List<S3FileInfo>();

            // ハッシュデータ取得.
            var hashTable = await GetUploadedObjectHashTable(s3Client, s3Objects);

            // データ情報構築.

            foreach (var s3Object in s3Objects)
            {
                var element = hashTable.FirstOrDefault(x => x.Key == s3Object.Key);
                
                var s3FileInfo = new S3FileInfo()
                {
                    ObjectPath = s3Object.Key,
                    Hash = element.Value,
                };

                s3FileInfos.Add(s3FileInfo);
            }

            return s3FileInfos.ToArray();
        }

        /// <summary> アップロード済みのファイルのハッシュデータ取得. </summary>
        private async Task<Dictionary<string, string>> GetUploadedObjectHashTable(S3Client s3Client, S3Object[] s3Objects)
        {
            var hashTable = new Dictionary<string, string>();

            var tasks = new List<Task>();

            foreach (var s3Object in s3Objects)
            {
                var task = Task.Run(async () =>
                {
                    var metaDataResponse = await s3Client.GetObjectMetaData(s3Object.Key);

                    var fileHash = metaDataResponse.Metadata[MetaDataHashKey];

                    lock (hashTable)
                    {
                        hashTable[s3Object.Key] = fileHash;
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks.ToArray());

            return hashTable;
        }

        /// <summary> ファイルをS3にアップロード </summary>
        private async Task UploadPackagesToS3(FileInfo[] fileInfos, S3FileInfo[] s3FileInfos)
        {
            var assetInfoManifestFilePath = GetAssetInfoManifestFilePath(files);

            var uploadTargets = new List<FileInfo>();

            // S3に存在しない / ファイルハッシュが違うファイルが対象.

            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.ObjectPath == assetInfoManifestFilePath){ continue; }

                var s3FileInfo = s3FileInfos.FirstOrDefault(x => x.ObjectPath == fileInfo.ObjectPath);

                if (s3FileInfo != null)
                {
                    if (fileInfo.Hash == s3FileInfo.Hash) { continue; }
                }

                uploadTargets.Add(fileInfo);
            }

            // 重複除外・パス順にソート.

            uploadTargets = uploadTargets
                .Distinct()
                .OrderBy(x => x.ObjectPath, new NaturalComparer())
                .ToList();

            // アップロード.

            if (uploadTargets.Any())
            {
                var isBatchMode = Application.isBatchMode;

                var count = 0;
                var logBuilder = new StringBuilder();

                using (new DisableStackTraceScope(LogType.Log))
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
                            
                            if (isBatchMode)
                            {
                                Debug.LogFormat(uploadTarget.FilePath);
                            }
                            else
                            {
                                lock (logBuilder)
                                {
                                    logBuilder.AppendLine(uploadTarget.FilePath);

                                    count++;

                                    if (100 < count)
                                    {
                                        Debug.Log(logBuilder.ToString());
     
                                        logBuilder.Clear();
                                        count = 0;
                                    }
                                }
                            }
                        });

                        tasks.Add(task);
                    }
                    
                    await Task.WhenAll(tasks.ToArray());
                }

                if (!isBatchMode && count != 0)
                {
                    Debug.Log(logBuilder.ToString());
                }
            }
        }

        /// <summary> 削除対象のファイルをS3から削除 </summary>
        private async Task DeleteDeletedPackageFromS3(FileInfo[] fileInfos,  S3FileInfo[] s3FileInfos)
        {
            var deleteTargets = new List<S3FileInfo>();

            foreach (var s3FileInfo in s3FileInfos)
            {
                var fileInfo = fileInfos.FirstOrDefault(x => x.ObjectPath == s3FileInfo.ObjectPath);

                if (fileInfo != null)
                {
                    if (fileInfo.Hash == s3FileInfo.Hash){ continue; }
                }

                deleteTargets.Add(s3FileInfo);
            }

            // 削除.

            var deleteObjectPaths = deleteTargets.Select(x => x.ObjectPath).ToArray();

            if (deleteObjectPaths.Any())
            {
                Debug.LogFormat("Delete deleted {0} objects from s3 {1}.", bucketFolder, deleteObjectPaths.Length);

                await s3Client.DeleteObjects(deleteObjectPaths);
            }

            // ログ.

            if (deleteObjectPaths.Any())
            {
                var chunk = deleteTargets.Chunk(100).ToArray();

                var num = chunk.Length;

                for (var i = 0; i < num; i++)
                {
                    var builder = new StringBuilder();

                    chunk[i].ForEach(x => builder.AppendLine(x.ObjectPath));

                    Debug.LogFormat("Delete S3 objects. [{0}/{1}]\n{2}", i + 1, num, builder.ToString());
                }
            }
        }

        public abstract string GetIdentityPoolId();
        
        public abstract string GetBucketName();

        public abstract RegionEndpoint GetBucketRegion();

        public abstract RegionEndpoint GetCredentialsRegion();

        public abstract AesCryptoStreamKey GetCryptoKey();
    }
}

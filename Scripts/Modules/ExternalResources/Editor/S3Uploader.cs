
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

        public enum UploadType
        {
            /// <summary> 全オブジェクトを削除後に全アップロード </summary>
            Full,

            /// <summary>
            /// アップロード先のmetadataのハッシュ値と一致しないオブジェクトをアップロード.
            /// ※ AssetInfoManifestは必ずアップロードされる.
            /// </summary>
            Incremental,
        }

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

        public async Task<bool> Execute(UploadType uploadType, string folderPath, string bucketFolder)
        {
            this.folderPath = folderPath;
            this.bucketFolder = bucketFolder;

            // ファイルパス : オブジェクトパスのディクショナリ作成.
            files = Directory.GetFiles(folderPath)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            using (new DisableStackTraceScope())
            {
                s3Client = CreateS3Client();

                AssetBundle.UnloadAllAssetBundles(true);

                try
                {
                    //------- AssetInfoManifest.package読み込み -------

                    await LoadAssetInfoManifest();

                    if (assetInfoManifest == null)
                    {
                        Debug.LogError("AssetInfoManifest.package file load error.");
                        return false;
                    }

                    //------- アップロードファイル情報作成 -------

                    var fileInfos = BuildUploadFileInfos();

                    //------- S3のファイル一覧取得 -------

                    var s3Objects = await GetUploadedObjects();

                    //------- S3内の全ファイル削除 -------

                    if (uploadType == UploadType.Full)
                    {
                        // 対象のパス以下のファイル削除.
                        await DeleteAllPackageFromS3(s3Objects);

                        // 全削除したので空にする.
                        s3Objects = new S3Object[0];
                    }

                    //------- S3ファイル情報作成 -------

                    var s3FileInfos = await BuildUploadedObjectInfos(s3Objects);

                    //------- 削除対象のファイルをS3から削除 -------

                    if (uploadType == UploadType.Incremental)
                    {
                        await DeleteDeletedPackageFromS3(fileInfos, s3FileInfos);
                    }

                    //------- 新規・更新対象のファイルをS3にアップロード -------

                    await UploadPackagesToS3(fileInfos, s3FileInfos);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);

                    return false;
                }
            }

            return true;
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

                if (string.IsNullOrEmpty(hash))
                {
                    Debug.LogErrorFormat("File hash not found.\n{0}", fileName);
                    continue;
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

        /// <summary> 新規・更新対象のファイルをS3にアップロード </summary>
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

            // 重複除外・パスが短い順にソート.

            uploadTargets = uploadTargets.Distinct()
                .OrderBy(x => x.ObjectPath.Length)
                .ToList();

            // アップロード.

            if (uploadTargets.Any())
            {
                Debug.LogFormat("Uploading {0} files to s3 {1}.", uploadTargets.Count, bucketFolder);

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

                        fileTransferUtilityRequest.Metadata.Add(MetaDataHashKey, uploadTarget.Hash);

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
                Action<int, int, S3FileInfo[]> logOutput = (index, num, targets) =>
                {
                    var builder = new StringBuilder();

                    targets.ForEach(x => builder.AppendLine(x.ObjectPath));

                    Debug.LogFormat("Delete S3 objects. [{0}/{1}]\n{2}", index, num, builder.ToString());
                };

                ChunkAction(deleteTargets, logOutput);
            }
        }

        /// <summary> S3内の全ファイル削除 </summary>
        private async Task DeleteAllPackageFromS3(S3Object[] s3Objects)
        {
            // 削除.

            var deleteTargets = s3Objects.Select(x => x.Key).ToArray();

            if (deleteTargets.Any())
            {
                Debug.LogFormat("Delete s3 {0} all objects.", bucketFolder);

                await s3Client.DeleteObjects(deleteTargets);
            }

            // ログ.
            if (deleteTargets.Any())
            {
                Action<int, int, string[]> logOutput = (index, num, targets) =>
                {
                    var builder = new StringBuilder();

                    targets.ForEach(x => builder.AppendLine(x));

                    Debug.LogFormat("Delete S3 objects. [{0}/{1}]\n{2}", index, num, builder.ToString());
                };

                ChunkAction(deleteTargets, logOutput);
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

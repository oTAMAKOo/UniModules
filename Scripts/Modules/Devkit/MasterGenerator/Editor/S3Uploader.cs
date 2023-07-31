
#if ENABLE_AMAZON_WEB_SERVICE

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Amazon.S3;

namespace Modules.Master.Editor
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

        /// <summary> S3にアップロード済みのファイル情報 </summary>
        protected sealed class S3FileInfo
        {
            public string ObjectPath { get; set; }

            public string Hash { get; set; }
        }

        //----- field -----

        private string folderPath = null;
        private string bucketFolder = null;

        private string[] files = null;
        
        //----- property -----

        //----- method -----

        public async UniTask<bool> Execute(string folderPath, string bucketFolder)
        {
            this.folderPath = folderPath;
            this.bucketFolder = BucketFolderOverride(bucketFolder);

            // ファイルパス : オブジェクトパスのディクショナリ作成.
            files = Directory.GetFiles(folderPath)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            using (new DisableStackTraceScope())
            {
                CreateS3Client();

                try
                {
                    //------- アップロードファイル情報作成 -------

                    var fileInfos = BuildUploadFileInfos();

                    //------- S3のファイル一覧取得 -------

                    var s3Objects = await GetUploadedObjects();

                    //------- S3内の全ファイル削除 -------

                    // 対象のパス以下のファイル削除.
                    await DeleteAllPackageFromS3(s3Objects);

                    // 全削除したので空にする.
                    s3Objects = new S3Object[0];
                    
                    //------- S3にアップロード -------

                    await UploadMasterFilesToS3(fileInfos);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("S3 upload process error. \n{0}", e);

                    return false;
                }
            }

            return true;
        }

        /// <summary> アップロードするファイルデータ構築. </summary>
        private FileInfo[] BuildUploadFileInfos()
        {
            Debug.Log("Build upload file infos.");

            var fileInfos = new List<FileInfo>();

            var folderPathLength = folderPath.Length;
            
            foreach (var file in files)
            {
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

        /// <summary> 対象のファイルをS3にアップロード </summary>
        private async UniTask UploadMasterFilesToS3(FileInfo[] fileInfos)
        {
            // アップロード.

            if (fileInfos.Any())
            {
                Debug.LogFormat("Uploading {0} files to s3 {1}", fileInfos.Length, bucketFolder);

                // ファイルをアップロード.

                const long PartSize = 5 * 1024 * 1024; // 5MB単位.
                
                var isBatchMode = Application.isBatchMode;

                var chunkedFileInfos = fileInfos.Chunk(50);

                foreach (var items in chunkedFileInfos)
                {
                    var tasks = new List<UniTask>();

                    var logBuilder = new StringBuilder();

                    foreach (var item in items)
                    {
                        var info = item;

                        var task = UniTask.RunOnThreadPool(async () =>
                        {
                            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                            {
                                FilePath = info.FilePath,
                                StorageClass = S3StorageClass.StandardInfrequentAccess,
                                PartSize = PartSize,
                                Key = info.ObjectPath,
                                CannedACL = UploadFileCannedACL,
                            };

                            await s3Client.Upload(fileTransferUtilityRequest);

                            if (isBatchMode)
                            {
                                Debug.LogFormat(info.ObjectPath);
                            }
                            else
                            {
                                lock (logBuilder)
                                {
                                    logBuilder.AppendLine(info.ObjectPath);
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

        /// <summary> S3内の全ファイル削除 </summary>
        private async UniTask DeleteAllPackageFromS3(S3Object[] s3Objects)
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
    }
}

#endif
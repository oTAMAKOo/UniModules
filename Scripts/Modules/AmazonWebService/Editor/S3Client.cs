
#if ENABLE_AMAZON_WEB_SERVICE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Extensions;

namespace Modules.Amazon.S3
{
    public sealed class S3Client
    {
        //----- params -----

        //----- field -----
        
        private AmazonS3Client client = null;

        //----- property -----

        public string IdentityPoolId { get; private set; }

        public string BucketName { get; private set; }

        //----- method -----

        public S3Client(string identityPoolId, string bucketName, RegionEndpoint bucketRegion, RegionEndpoint credentialsRegion)
        {
            this.IdentityPoolId = identityPoolId;
            this.BucketName = bucketName;

            var credentialsRegionSystemName = RegionEndpoint.GetBySystemName(credentialsRegion.SystemName);

            var credentials = new CognitoAWSCredentials(identityPoolId, credentialsRegionSystemName);

            var bucketRegionEndpoint = RegionEndpoint.GetBySystemName(bucketRegion.SystemName);

            client = new AmazonS3Client(credentials, bucketRegionEndpoint);
        }

        #region Get

        public async Task<S3Object[]> GetObjectList(string prefix = null, int? maxKeys = null)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = BucketName,
            };

            if (!string.IsNullOrEmpty(prefix))
            {
                if (prefix.EndsWith(PathUtility.PathSeparator.ToString()))
                {
                    prefix += PathUtility.PathSeparator;
                }

                request.Prefix = prefix;
            }

            if (maxKeys.HasValue)
            {
                request.MaxKeys = maxKeys.Value;
            }

            return await GetObjectList(request);
        }

        public async Task<S3Object[]> GetObjectList(ListObjectsV2Request request)
        {
            ListObjectsV2Response response = null;

            var s3Objects = new List<S3Object>();

            request.BucketName = BucketName;

            do
            {
                response = await client.ListObjectsV2Async(request);

                s3Objects.AddRange(response.S3Objects);

                request.ContinuationToken = response.NextContinuationToken;
            }
            while (response.IsTruncated);

            return s3Objects.ToArray();
        }

        public async Task<GetObjectMetadataResponse> GetObjectMetaData(string objectPath)
        {
            var request = new GetObjectMetadataRequest()
            {
                BucketName = BucketName,
                Key = objectPath,
            };

            return await GetObjectMetaData(request);
        }

        public async Task<GetObjectMetadataResponse> GetObjectMetaData(GetObjectMetadataRequest request)
        {
            request.BucketName = BucketName;

            var meta = await client.GetObjectMetadataAsync(request);

            return meta;
        }

        public async Task<byte[]> GetObject(string objectPath, Action<GetObjectResponse> onComplete = null)
        {
            byte[] bytes = null;

            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = objectPath,
            };

            using (var response = await client.GetObjectAsync(request))
            {
                if (onComplete != null)
                {
                    onComplete.Invoke(response);
                }

                using (var responseStream = response.ResponseStream)
                {
                    using (var binaryReader = new BinaryReader(responseStream))
                    {
                        bytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
                    }
                }
            }

            return bytes;
        }

        #endregion

        #region Upload

        public async Task Upload(string uploadFilePath, string objectPath = null)
        {
            using (var fileTransferUtility = new TransferUtility(client))
            {
                if (string.IsNullOrEmpty(objectPath))
                {
                    // Upload a file. The file name is used as the object key name.
                    await fileTransferUtility.UploadAsync(uploadFilePath, BucketName);
                }
                else
                {
                    // Specify object key name explicitly.
                    await fileTransferUtility.UploadAsync(uploadFilePath, BucketName, objectPath);
                }
            }
        }

        public async Task Upload(TransferUtilityUploadRequest uploadRequest)
        {
            using (var fileTransferUtility = new TransferUtility(client))
            {
                uploadRequest.BucketName = BucketName;

                await fileTransferUtility.UploadAsync(uploadRequest);
            }
        }

        public async Task UploadDirectory(string uploadDirectoryPath)
        {
            using (var fileTransferUtility = new TransferUtility(client))
            {
                await fileTransferUtility.UploadDirectoryAsync(uploadDirectoryPath, BucketName);
            }
        }

        #endregion

        #region Put

        public async Task<PutObjectResponse> Put(PutObjectRequest request)
        {
            request.BucketName = BucketName;

            var response = await client.PutObjectAsync(request);

            return response;
        }

        #endregion

        #region Delete

        public async Task<DeleteObjectResponse> DeleteObject(string objectPath)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = objectPath,
            };

            var response = await client.DeleteObjectAsync(request);

            return response;
        }

        public async Task<DeleteObjectResponse> DeleteObject(DeleteObjectRequest request)
        {
            request.BucketName = BucketName;

            var response = await client.DeleteObjectAsync(request);

            return response;
        }

        public async Task<DeleteObjectsResponse> DeleteObjects(string[] objectPaths, string[] versionIds = null)
        {
            var keyVersions = new List<KeyVersion>();

            for (var i = 0; i < objectPaths.Length; i++)
            {
                var objectPath = objectPaths[i];

                var keyVersion = new KeyVersion()
                {
                    Key = objectPath,
                };

                if (versionIds != null)
                {
                    var versionId = versionIds.ElementAtOrDefault(i);

                    if (!string.IsNullOrEmpty(versionId))
                    {
                        keyVersion.VersionId = versionId;
                    }
                }

                keyVersions.Add(keyVersion);
            }

            var request = new DeleteObjectsRequest
            {
                BucketName = BucketName,
                Objects = keyVersions,
            };

            var response = await client.DeleteObjectsAsync(request);

            return response;
        }

        public async Task<DeleteObjectsResponse> DeleteObjects(DeleteObjectsRequest request)
        {
            request.BucketName = BucketName;

            var response = await client.DeleteObjectsAsync(request);

            return response;
        }

        #endregion
    }
}

#endif


#if ENABLE_AMAZON_WEB_SERVICE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Modules.Amazon.S3
{
    public class S3Client
    {
        //----- params -----

        //----- field -----

        private AmazonS3Client client = null;

        //----- property -----

        public string IdentityPoolId { get; protected set; }

        public string BucketName { get; protected set; }

        //----- method -----

        public S3Client(string identityPoolId, string bucketName, RegionEndpoint bucketRegion, RegionEndpoint credentialsRegion)
        {
            this.IdentityPoolId = identityPoolId;
            this.BucketName = bucketName;

            var credentialsRegionSystemName = RegionEndpoint.GetBySystemName(credentialsRegion.SystemName);

            var credentials = new CognitoAWSCredentials(identityPoolId, credentialsRegionSystemName);

            var bucketRegionSystemName = RegionEndpoint.GetBySystemName(bucketRegion.SystemName);

            client = new AmazonS3Client(credentials, bucketRegionSystemName);
        }

        #region Get

        public async Task<S3Object[]> GetObjectList(int? maxKeys = null)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = BucketName,
            };

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
            var fileTransferUtility = new TransferUtility(client);

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

        public async Task Upload(TransferUtilityUploadRequest uploadRequest)
        {
            var fileTransferUtility = new TransferUtility(client);

            uploadRequest.BucketName = BucketName;

            await fileTransferUtility.UploadAsync(uploadRequest);
        }

        #endregion

        #region Delete

        public async Task DeleteObject(string objectPath)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = objectPath,
            };

            await client.DeleteObjectAsync(request);
        }

        public async Task DeleteObject(DeleteObjectRequest request)
        {
            request.BucketName = BucketName;

            await client.DeleteObjectAsync(request);
        }

        #endregion
    }
}

#endif

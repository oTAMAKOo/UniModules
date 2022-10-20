
using System;
using Extensions;
using Amazon;
using Amazon.S3;

namespace Modules.Amazon.S3
{
    public abstract class S3UploaderBase
    {
        //----- params -----

        //----- field -----

		protected S3Client s3Client = null;

        //----- property -----

		/// <summary> アップロードファイルのアクセス権 </summary>
		protected virtual S3CannedACL UploadFileCannedACL { get { return S3CannedACL.PublicRead; } }

        //----- method -----

		protected void CreateS3Client()
        {
			var bucketName = GetBucketName();
			var bucketRegion = GetBucketRegion();

			if (this is IBasicCredentials)
			{
				s3Client = new S3Client(bucketName, bucketRegion, this as IBasicCredentials);
			}
			else if (this is ICognitoCredentials)
			{
				s3Client = new S3Client(bucketName, bucketRegion, this as ICognitoCredentials);
			}
			else
			{
				throw new Exception("Credentials not found.");
			}
        }
		
		protected virtual string BucketFolderOverride(string bucketFolder)
		{
			return bucketFolder;
		}

		public abstract string GetBucketName();

        public abstract RegionEndpoint GetBucketRegion();
    }
}
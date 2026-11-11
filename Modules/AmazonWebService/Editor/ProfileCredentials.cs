
#if ENABLE_AMAZON_WEB_SERVICE

using System;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Modules.Amazon.S3
{
    /// <summary> 環境変数とAWS共有認証情報ファイルからアクセスキーを解決する </summary>
    /// <remarks>
    /// アクセスキーをソースコードへ書かずに済ませるための実装。
    /// 解決順は「環境変数 → 共有認証情報ファイル（~/.aws/credentials）のプロファイル」で、
    /// どちらも取得できない場合は例外を送出する。
    /// </remarks>
    public sealed class ProfileCredentials : IBasicCredentials
    {
        //----- params -----

        //----- field -----

        private string profileName = null;

        private string accessKeyEnvironmentName = null;

        private string secretKeyEnvironmentName = null;

        private ImmutableCredentials credentials = null;

        //----- property -----

        //----- method -----

        public ProfileCredentials(string profileName) : this(profileName, null, null)
        {
        }

        public ProfileCredentials(string profileName, string accessKeyEnvironmentName, string secretKeyEnvironmentName)
        {
            this.profileName = profileName;
            this.accessKeyEnvironmentName = accessKeyEnvironmentName;
            this.secretKeyEnvironmentName = secretKeyEnvironmentName;
        }

        public string GetAccessKey()
        {
            return GetCredentials().AccessKey;
        }

        public string GetSecretKey()
        {
            return GetCredentials().SecretKey;
        }

        private ImmutableCredentials GetCredentials()
        {
            if (credentials != null){ return credentials; }

            credentials = GetEnvironmentCredentials();

            if (credentials != null){ return credentials; }

            credentials = GetProfileCredentials();

            return credentials;
        }

        private ImmutableCredentials GetEnvironmentCredentials()
        {
            if (string.IsNullOrEmpty(accessKeyEnvironmentName)){ return null; }
            if (string.IsNullOrEmpty(secretKeyEnvironmentName)){ return null; }

            var accessKey = Environment.GetEnvironmentVariable(accessKeyEnvironmentName);
            var secretKey = Environment.GetEnvironmentVariable(secretKeyEnvironmentName);

            if (string.IsNullOrEmpty(accessKey)){ return null; }
            if (string.IsNullOrEmpty(secretKey)){ return null; }

            return new ImmutableCredentials(accessKey, secretKey, null);
        }

        private ImmutableCredentials GetProfileCredentials()
        {
            var sharedCredentialsFile = new SharedCredentialsFile();

            CredentialProfile profile = null;

            if (!sharedCredentialsFile.TryGetProfile(profileName, out profile))
            {
                throw new InvalidOperationException($"AWS credentials not found. Run [ aws configure --profile {profileName} ].");
            }

            AWSCredentials awsCredentials = null;

            if (!AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedCredentialsFile, out awsCredentials))
            {
                throw new InvalidOperationException($"Failed to resolve AWS credentials from profile. ({profileName})");
            }

            return awsCredentials.GetCredentials();
        }
    }
}

#endif


using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Extensions;

namespace Modules.ExternalAssets
{
    public sealed class ExternalAssetFileNameManager : Singleton<ExternalAssetFileNameManager>
    {
        //----- params -----

        /// <summary> ファイル名に使うハッシュ文字数 (SHA256先頭32文字=128bit). </summary>
        public const int HashedFileNameLength = 32;

        //----- field -----

        // ResourcePathベースのハッシュキャッシュ (スレッドプールから参照されるためスレッドセーフな実装を使う).
        private ConcurrentDictionary<string, string> resourcePathHashCache = null;

        //----- property -----

        //----- method -----

        private ExternalAssetFileNameManager() { }

        protected override void OnCreate()
        {
            resourcePathHashCache = new ConcurrentDictionary<string, string>();
        }

        protected override void OnRelease()
        {
            if (resourcePathHashCache != null)
            {
                resourcePathHashCache.Clear();
                resourcePathHashCache = null;
            }
        }

        protected override void OnRefresh()
        {
            if (resourcePathHashCache != null)
            {
                resourcePathHashCache.Clear();
            }
        }

        /// <summary> コンテンツハッシュに基づくファイル名を構築. </summary>
        /// <param name="assetInfo"> アセット情報. </param>
        /// <param name="fixedExtension"> 固定拡張子 (指定時はAssetInfoの拡張子を無視). </param>
        public string BuildHashedFileName(AssetInfo assetInfo, string fixedExtension = null)
        {
            if (assetInfo == null){ return null; }

            var hash = assetInfo.Hash;

            var extension = string.IsNullOrEmpty(fixedExtension)
                ? Path.GetExtension(assetInfo.FileName)
                : fixedExtension;

            // ハッシュ未設定のアセット (AssetInfoManifest本体等) は元のファイル名のまま.
            if (string.IsNullOrEmpty(hash))
            {
                if (string.IsNullOrEmpty(fixedExtension))
                {
                    return assetInfo.FileName;
                }

                return Path.ChangeExtension(assetInfo.FileName, fixedExtension);
            }

            // ファイル名からアセット内容が推測されないようハッシュのみで命名.
            var length = HashedFileNameLength < hash.Length ? HashedFileNameLength : hash.Length;

            return hash.Substring(0, length) + extension;
        }

        /// <summary>
        /// ResourcePathのベース部分 (拡張子除く) をキーにハッシュを生成してファイル名を構築.
        /// CRIのACB/AWBペアのように同一ベース名でペアリングが必要なアセット向け.
        /// </summary>
        /// <param name="assetInfo"> アセット情報. </param>
        public string BuildPairedHashedFileName(AssetInfo assetInfo)
        {
            if (assetInfo == null){ return null; }

            if (string.IsNullOrEmpty(assetInfo.ResourcePath)){ return assetInfo.FileName; }

            var extension = Path.GetExtension(assetInfo.FileName);

            // 拡張子を除いたResourcePathをキーとしてハッシュ化.
            var baseKey = Path.ChangeExtension(assetInfo.ResourcePath, null);

            var hashedBaseName = GetCachedResourcePathHash(baseKey);

            return hashedBaseName + extension;
        }

        private string GetCachedResourcePathHash(string baseKey)
        {
            return resourcePathHashCache.GetOrAdd(baseKey, ComputeResourcePathHash);
        }

        private static string ComputeResourcePathHash(string baseKey)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseKey));

                var builder = new StringBuilder(bytes.Length * 2);

                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString(0, HashedFileNameLength);
            }
        }
    }
}

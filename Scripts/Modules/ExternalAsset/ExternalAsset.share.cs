
namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        public const string ShareGroupName = "Share";

        public const string ShareGroupPrefix = ShareGroupName + ":";

        //----- field -----

        /// <summary> 共有外部アセットディレクトリ. </summary>
        public string shareAssetDirectory = null;

        //----- property -----

        //----- method -----

        private static bool HasSharePrefix(string resourcePath)
        {
            return resourcePath.StartsWith(ShareGroupPrefix);
        }

        private static string ConvertToShareResourcePath(string resourcePath)
        {
            if (HasSharePrefix(resourcePath))
            {
                resourcePath = resourcePath.Substring(ShareGroupPrefix.Length);
            }

            return resourcePath;
        }
    }
}

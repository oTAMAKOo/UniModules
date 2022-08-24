
namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        public const string ShareGroupName = "Share";

        public const string ShareGroupPrefix = ShareGroupName + ":";

        //----- field -----

        /// <summary> 共有外部アセットディレクトリ. </summary>
        public string shareDirectory = null;

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

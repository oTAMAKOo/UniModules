
namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        public const string ShareCategoryName = "Share";

        public const string ShareCategoryPrefix = ShareCategoryName + ":";

        //----- field -----

        /// <summary> 共有外部アセットディレクトリ. </summary>
        public string shareDirectory = null;

        //----- property -----

        //----- method -----

        private static bool HasSharePrefix(string resourcePath)
        {
            return resourcePath.StartsWith(ShareCategoryPrefix);
        }

        private static string ConvertToShareResourcePath(string resourcePath)
        {
            if (HasSharePrefix(resourcePath))
            {
                resourcePath = resourcePath.Substring(ShareCategoryPrefix.Length);
            }

            return resourcePath;
        }
    }
}


namespace Modules.ExternalAssets
{
    public sealed class DownloadProgressInfo
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public AssetInfo AssetInfo { get; private set; }

        public float Progress { get; private set; }

        //----- method -----

        public DownloadProgressInfo(AssetInfo assetInfo)
        {
            AssetInfo = assetInfo;
        }

        public void SetProgress(float progress)
        {
            Progress = progress;
        }
    }
}
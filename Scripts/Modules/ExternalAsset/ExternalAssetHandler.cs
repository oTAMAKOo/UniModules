
using Cysharp.Threading.Tasks;

namespace Modules.ExternalAssets
{
    /// <summary> 更新時処理を実行する為のインターフェース </summary>
    public interface IUpdateAssetHandler
    {
        UniTask OnUpdateRequest(AssetInfo assetInfo);

		UniTask OnUpdateFinish(AssetInfo assetInfo);
    }

    /// <summary> 読み込み時処理を実行する為のインターフェース </summary>
    public interface ILoadAssetHandler
    {
		UniTask OnLoadRequest(AssetInfo assetInfo);

		UniTask OnLoadFinish(AssetInfo assetInfo);
    }
}

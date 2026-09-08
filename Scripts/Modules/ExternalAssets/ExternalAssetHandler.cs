
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.ExternalAssets
{
    /// <summary> 更新時処理を実行する為のインターフェース </summary>
    public interface IUpdateAssetHandler
    {
        UniTask OnUpdateRequest(AssetInfo assetInfo, CancellationToken cancelToken = default);

		UniTask OnUpdateFinish(AssetInfo assetInfo, CancellationToken cancelToken = default);
    }

    /// <summary> 読み込み時処理を実行する為のインターフェース </summary>
    public interface ILoadAssetHandler
    {
		UniTask OnLoadRequest(AssetInfo assetInfo, CancellationToken cancelToken = default);

		UniTask OnLoadFinish(AssetInfo assetInfo, CancellationToken cancelToken = default);
    }
}

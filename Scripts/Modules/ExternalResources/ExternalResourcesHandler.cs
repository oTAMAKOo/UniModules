
using System;
using UniRx;

namespace Modules.ExternalResource
{
    /// <summary> 更新時処理を実行する為のインターフェース </summary>
    public interface IUpdateAssetHandler
    {
        IObservable<Unit> OnUpdateRequest(AssetInfo assetInfo);

        IObservable<Unit> OnUpdateFinish(AssetInfo assetInfo);
    }

    /// <summary> 読み込み時処理を実行する為のインターフェース </summary>
    public interface ILoadAssetHandler
    {
        IObservable<Unit> OnLoadRequest(AssetInfo assetInfo);

        IObservable<Unit> OnLoadFinish(AssetInfo assetInfo);
    }
}

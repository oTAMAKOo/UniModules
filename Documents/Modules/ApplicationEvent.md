# ApplicationEvent

> **namespace**: `Modules.ApplicationEvent`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ApplicationEvent/`
> **Client側使用**: 約5ファイル（2026-07時点）
> **依存**: R3 / Extensions（`SingletonMonoBehaviour<T>`, `UnityUtility`）

## 概要

アプリのサスペンド（バックグラウンド移行）/ レジューム復帰 / 低メモリ警告 / 終了を **R3 Observable として配信**する基盤。
各クラスが Unity の `OnApplicationPause` / `OnApplicationQuit` / `Application.lowMemory` を個別に実装する必要はなく、**static メソッドの購読だけ**でイベントを受けられる（MonoBehaviour でないクラスからも購読可能）。
ハンドラ実体（GameObject）は起動時に InitializeObject が1つだけ常駐生成する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| バックグラウンド移行（サスペンド）を検知したい | `ApplicationEventHandler.OnSuspendAsObservable()` |
| 復帰（レジューム）を検知したい | `ApplicationEventHandler.OnResumeAsObservable()`（中断していた秒数 double が流れる） |
| アプリ終了を検知したい | `ApplicationEventHandler.OnQuitAsObservable()` |
| メモリ不足警告を受けたい | `ApplicationEventHandler.OnLowMemoryAsObservable()` |
| 中断時にセーブデータを確実に書き込みたい | **OnSuspend と OnQuit の両方**を購読して Flush（SaveDataManager が実例） |
| 長時間中断後にサーバー再同期したい | OnResume の経過秒数で判定（SystemModel が実例） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `ApplicationEventHandler` | `SingletonMonoBehaviour<T>` 継承 | Unity コールバック（OnApplicationPause / OnApplicationQuit / Application.lowMemory）を static な R3 Observable に変換する常駐オブジェクト |

### 仕組み

- Subject はすべて **static** のため、`ApplicationEventHandler.OnSuspendAsObservable()` 等は**インスタンス参照なしに購読できる**
- イベントの発火元は MonoBehaviour の Unity コールバック。**GameObject 実体が存在して初めて発火する**（起動時に `InitializeObject.CreateApplicationEventHandler()` が生成済みのため、Client実装では購読するだけでよい）
- サスペンド重複ガードあり: サスペンド中の再サスペンド通知は無視、レジュームはサスペンド済みの時だけ通知
- `OnResume` はサスペンド開始からの経過秒数（`DateTime.Now` 差分の `TotalSeconds`）を値として流す

## 使い方(実例)

### 中断・終了時にセーブを即時書き込み（購読の定番形）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/SaveData/SaveDataManager.cs
using Modules.ApplicationEvent;

// サスペンド/終了時は保留中のSaveを強制的に即時書き込み（バッチSave中でも）.
ApplicationEventHandler.OnSuspendAsObservable()
    .Subscribe(_ => Flush(true))
    .AddTo(Disposable);

ApplicationEventHandler.OnQuitAsObservable()
    .Subscribe(_ => Flush(true))
    .AddTo(Disposable);
```

### 復帰時のサーバー再同期

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/SystemModel.cs
ApplicationEventHandler.OnResumeAsObservable()
    .Subscribe(x => OnApplicationResume(x).Forget())
    .AddTo(Disposable);

private async UniTask OnApplicationResume(double suspendTime)
{
    // サーバー同期.
    var requireServerSync = RequireServerSync();

    if (requireServerSync)
    {
        var result = await GetServerData();
        // ...エラー時はタイトルへ.
    }
}
```

### 終了時のリソース解放

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
// アプリ終了時に全サウンド解放.
ApplicationEventHandler.OnQuitAsObservable().Subscribe(_ => soundManagement.ReleaseAll());
```

### 実体の生成（起動時に1回だけ・通常は書かない）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
private void CreateApplicationEventHandler()
{
    if (ApplicationEventHandler.Instance == null)
    {
        UnityUtility.CreateGameObject<ApplicationEventHandler>(null, "ApplicationEventHandler");
    }
}
```

## API(主要公開メンバー)

### ApplicationEventHandler（すべて static）

| メンバー | 説明 |
|---|---|
| `static Observable<Unit> OnSuspendAsObservable()` | サスペンド時（`OnApplicationPause(true)`）。サスペンド中の重複通知はなし |
| `static Observable<double> OnResumeAsObservable()` | レジューム時。**サスペンドしてからの経過秒数**が流れる |
| `static Observable<Unit> OnLowMemoryAsObservable()` | メモリ不足警告（`Application.lowMemory`） |
| `static Observable<Unit> OnQuitAsObservable()` | アプリ終了時（`OnApplicationQuit`） |
| `static ApplicationEventHandler Instance` / `static T CreateInstance()`（基底） | 常駐インスタンス参照 / 生成（起動時に InitializeObject が実施済み） |

## 注意点・罠

- **`OnApplicationPause` / `OnApplicationQuit` を各クラスに直書きしない**。本モジュールの購読に統一する（プロジェクトの「Unity ライフサイクルメソッド原則禁止」ルールとも整合。ハンドラ自身の Unity コールバック実装は既存基盤として例外）
- Subject は static のため購読自体はいつでも可能だが、**GameObject 実体が無いとイベントは発火しない**。実体は起動時に InitializeObject が生成済みなので、Client 実装で `CreateInstance()` を呼ぶ必要はない
- モバイルではサスペンドのまま OS にプロセスを kill され **`OnQuit` が来ないことがある**。永続化・確定処理は OnQuit だけでなく **OnSuspend でも**行う（SaveDataManager の Flush が実例）
- `OnResume` の経過秒数は `DateTime.Now`（端末時計）ベース。サーバー時間ではないので、厳密な時刻判定には `systemModel.LocalTime` 側の仕組みを使う
- static Subject は解放されないため、購読側は必ず `.AddTo(Disposable)` / `.AddTo(this)` で寿命管理する
- エディタ実行ではモバイル実機とコールバックの発火タイミングが異なる場合がある（実機で要確認）。SystemModel の復帰処理も `#if !UNITY_EDITOR` でエディタを除外している
- 常駐オブジェクトは `SceneManager`（Client側）の重複管理対象に登録されており、シーンを跨いで1つに保たれる

## 関連

- [Scene](Scene.md) — シーン遷移基盤。Client側 SceneManager が本ハンドラを重複管理対象に登録
- [LocalData](LocalData.md) — サスペンド/終了時に Flush されるローカル永続データ（SaveDataManager 経由）
- [Sound](Sound.md) — 終了時の全サウンド解放（InitializeObject.manager 参照）
- [Extensions/Core.md](../Extensions/Core.md) — `SingletonMonoBehaviour<T>`

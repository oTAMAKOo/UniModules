# R3Extension

> **namespace**: `Modules.R3Extension`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/R3Extension/`
> **Client側使用**: 約15ファイル（2026-07時点）
> **依存**: R3 / UniTask / Extensions（`Scope`）

## Rx は R3 を使う（UniRx は不在）

**本プロジェクトの Rx 実装は R3（Cysharp）であり、UniRx は完全に不在**。ルート `CLAUDE.md` の「UniRx パターン」という見出しは歴史的名残で、記載されているパターン自体（Subject 遅延初期化・`AddTo` 自動解除）は R3 でそのまま有効。

### 使用実態（grep 調査結果）

| 対象 | `using R3;` | `using UniRx` |
|---|---|---|
| `Client/Assets/Scripts`（Client側） | **558ファイル** | **0ファイル** |
| `Client/Assets/UniModules/Scripts`（基盤側） | **202ファイル** | **0ファイル** |

- UniRx ライブラリ本体はプロジェクトに存在しない（`Assets/ThirdParty` にも `Packages` にも無し。`namespace UniRx` を含むコードもゼロ）
- R3 は UPM 導入: `com.cysharp.r3` **v1.3.0**（`Packages/manifest.json` → `https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity`）

### 結論: Claude が新規コードを書く時

**必ず `using R3;` を使う**。`using UniRx;` はコンパイルエラーになる。プロジェクト定型は以下（R3 型で書いた「UniRx パターン」）:

```csharp
// 引用元: Client/Assets/Scripts/Client/Battle/Core/Manager/BattleEventBridge.cs
using R3;

private Subject<int> onTurnStart = null;

public Observable<int> OnTurnStartAsObservable()
{
    return onTurnStart ?? (onTurnStart = new Subject<int>());
}
```

購読側の解除は `.AddTo(this)`（Component/GameObject、Client側410箇所）または `.AddTo(Disposable)`（`LifetimeDisposable.Disposable` = R3 の `CompositeDisposable`、136箇所）。

### UniRx → R3 主な書き換え対応（実コードから確認できた範囲）

| UniRx | R3（本プロジェクトでの書き方） |
|---|---|
| `IObservable<T>` | `Observable<T>`（R3独自の抽象クラス。`System.IObservable` ではない） |
| `Subject<T>` / `CompositeDisposable` / `Unit.Default` | 同名のまま存在（`using R3;` のみで可） |
| `.AddTo(this)`（Component/GameObject） | R3.Unity 提供で同様に使用可 |
| `.AddTo(compositeDisposable)` | 同様（`LifetimeDisposable.Disposable` 経由が定型） |
| `Subscribe(onNext, onError, onCompleted)` | `Subscribe(onNext, onErrorResume, onCompleted)`。`onErrorResume` は**購読を終了させない**。終端は `onCompleted` に `Result`（`IsSuccess`/`IsFailure`）が渡る（実例: `Extensions/Methods/UniTaskExtensions.cs` の `DoOnError`/`OnErrorRetry` 実装） |
| `Observable.Timer/Interval(TimeSpan)` | 第2引数に `TimeProvider` を指定可（例: `Observable.Interval(TimeSpan.FromSeconds(1), UnityTimeProvider.Update)` — `SystemModel.cs`）。未指定でも R3.Unity が起動時に `UnityTimeProvider.Update` をデフォルト登録するため Unity 上では等価 |
| `ObserveEveryValueChanged` / `TakeUntilDestroy` / `DoOnError` / `DoOnCompleted` / `Finally` / `OnErrorRetry` / `AsUnitObservable` | `Extensions.UniTaskExtensions` に**UniRx互換シム**あり（[Extensions/Methods.md](../Extensions/Methods.md) 参照）。R3 標準に無くてもまずここを確認 |
| `Observable.FromCoroutine` | 廃止 → **本モジュールの `ObservableEx.FromUniTask`** を使う |
| `UniRx.Triggers` | `R3.Triggers`（`OnDestroyAsObservable()` 等。`TakeUntilDestroy` シムの内部で使用） |
| `button.OnClickAsObservable()` | R3.Unity 提供 + UniModules の `UIButton.OnClickAsObservable()` ラッパー（[UI](UI.md)） |
| `ReactiveProperty<T>` | R3 に存在するが **Client側使用0**。プロジェクトでは Subject + `XxxAsObservable()` 命名パターンで代替する |
| `Subscribe().AddTo(ct)` 相当 | R3 新API `RegisterTo(CancellationToken)` も使用可（基盤内で使用例あり） |

## 概要

R3 の Observable と UniTask を橋渡しする小規模基盤。機能は2系統:
(1) **AsyncHandler / AsyncHandlerScope** — `Subject.OnNext` で発火したイベントに対し、**発火側が全購読者の非同期処理（演出等）の完了を待ち合わせる**仕組み。戦闘のロジック層⇔View層の同期に多用される。
(2) **ObservableEx** — UniTask から `Observable<T>` を作る変換（`Observable.FromCoroutine` の代替）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| イベント発火側で、購読者全員の非同期処理完了を待ちたい | `var handler = new AsyncHandler();` → `subject.OnNext(handler)` → `await handler.Wait()` |
| 購読側で「処理中」を発火側に伝えたい（例外安全に） | `using (new AsyncHandlerScope(handler)) { await ...; }` |
| イベントにペイロードや購読側からの戻り値を持たせたい | `AsyncHandler` を継承した派生クラスに public フィールドを追加 |
| UniTask を Observable 化したい（キャンセル対応） | `ObservableEx.FromUniTask(ct => TaskAsync(ct))` |
| 非同期処理の多重実行防止・進捗共有をしたい | `ObservableEx.FromUniTask(...).Share()` を保持して使い回す |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `AsyncHandler` | class（継承可） | `Begin`/`End` の参照カウントで購読者の処理中状態を管理し、`Wait()` で全完了を待つ待ち合わせ機構。発火ごとに new する使い捨て |
| `AsyncHandlerScope` | sealed class / `Extensions.Scope`（IDisposable）継承 | `using` で `Begin`/`End` を自動対応付けするスコープ。null ハンドラ許容 |
| `ObservableEx` | static class | `UniTask` → `Observable<T>` 変換（`Observable.FromAsync` の薄いラッパー） |

## 使い方(実例)

### 発火側: 全購読者の演出完了を待つ（戦闘イベント）

```csharp
// 引用元: Client/Assets/Scripts/Client/Battle/Core/Manager/BattleEventBridge.cs
private Subject<AsyncHandler> onTaskExecuted = null;

/// <summary> 1タスク実行後の汎用イベントを発火し、購読側 (View) の演出完了を待つ. </summary>
public async UniTask FireTaskExecuted()
{
    if (onTaskExecuted == null){ return; }   // 購読者ゼロなら即戻る定型.

    var handler = new AsyncHandler();

    onTaskExecuted.OnNext(handler);

    await handler.Wait();
}

public Observable<AsyncHandler> OnTaskExecutedAsObservable()
{
    return onTaskExecuted ?? (onTaskExecuted = new Subject<AsyncHandler>());
}
```

### 購読側: AsyncHandlerScope で完了を伝える

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/BattleView.cs
battleEventBridge.OnUnitsBuiltAsObservable()
    .Subscribe(x => OnUnitsBuilt(x).Forget())
    .AddTo(this);

/// <summary> Unit 構築完了通知ハンドラ.</summary>
private async UniTask OnUnitsBuilt(AsyncHandler handler)
{
    using (new AsyncHandlerScope(handler))   // Begin → (処理) → Dispose 時に End.
    {
        await BuildUnits();
    }
}

// スコープを同期処理だけに掛ければ、裏で続くアニメは発火側を待たせない.
private UniTask OnTaskExecuted(AsyncHandler handler)
{
    using (new AsyncHandlerScope(handler))
    {
        allyPartyView.UpdateUnits();   // 数値/ゲージの増減アニメは各View側で裏で継続.
        enemyPartyView.UpdateUnits();
    }

    return UniTask.CompletedTask;
}
```

### 派生ハンドラで双方向にデータを受け渡す（リトライ確認）

```csharp
// 引用元: Client/Assets/Scripts/PlayFab/PlayFabCloudScript.cs
public sealed class RetryLimitAsyncHandler : AsyncHandler
{
    public string functionName = null;   // 発火側 → 購読側への入力.

    public bool requestRetry = false;    // 購読側 → 発火側への戻り値.
}

var asyncHandler = new RetryLimitAsyncHandler()
{
    functionName = functionName,
    requestRetry = false,
};

onRetryLimit.OnNext(asyncHandler);

await asyncHandler.Wait();

if (asyncHandler.requestRetry)   // 購読側(ダイアログ)の応答を読む.
{
    retryCount = 0;
}
```

同型の実例: `TileRequestAsyncHandler`（`viewRect` を持つ。`Client/Assets/Scripts/Client/Scene/Citadel/TileMap/MapCameraController.cs`）、`BuildManager.PreBuildAsyncHandler`（基盤エディタ、ビルド前処理差し込み）。

### ObservableEx: UniTask の Observable 化 + 多重実行防止（Client側未使用・基盤内実例）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/Particle/ParticlePlayer.cs
// 再生中なら同じ Observable を返し、多重再生を防ぐ.
playObservable = ObservableEx.FromUniTask(ct => PlayInternal(ct)).Share();

// 引用元: Client/Assets/UniModules/Scripts/Modules/ExternalAsset/AssetBundle/AssetBundleManager.cs
// Observable 化して Timeout 等のオペレータを適用する用途.
await ObservableEx.FromUniTask(ct => FileDownload(installPath, assetInfo, progress, ct).Timeout(DownloadTimeout))
```

## API(主要公開メンバー)

### AsyncHandler

| メンバー | 説明 |
|---|---|
| `void Begin()` | 処理中カウントを +1（Interlocked） |
| `void End()` | カウントを -1。0 以下になったら `Wait()` を解放（completionSource をクリアしてから発火） |
| `UniTask Wait()` | 全購読者の `End` まで待機。既に完了済み（カウント0）なら即 return。複数箇所からの同時 Wait 可 |
| `bool IsComplete` | カウントが 0 以下か |

### AsyncHandlerScope（Extensions.Scope 継承）

| メンバー | 説明 |
|---|---|
| `AsyncHandlerScope(AsyncHandler asyncHandler)` | コンストラクタで `Begin()`。**null を渡しても安全**（何もしない） |
| `void Dispose()`（基底 `Scope`） | `End()` を呼ぶ。`using` で例外時も確実に End される |

### ObservableEx（static）

| メンバー | 説明 |
|---|---|
| `Observable<T> FromUniTask<T>(Func<CancellationToken, UniTask<T>> taskFactory)` | 値を返す UniTask を Observable 化。ct は購読 Dispose で発火 |
| `Observable<Unit> FromUniTask(Func<CancellationToken, UniTask> taskFactory)` | 値なし版（`Unit` を流す） |

## 注意点・罠

- **AsyncHandler は発火ごとに new する使い捨て**。使い回すとカウントと `Wait` の整合が壊れる
- `Begin`/`End` は必ず対で呼ぶ。手動で書かず **`AsyncHandlerScope` + `using` を使う**（例外時の End 漏れ = 発火側が永久に待つ事故を防ぐ）
- 購読者がハンドラを `Begin` しなければ発火側は待たない。「待たせたくない購読者」はスコープを同期部分だけに掛ける（`BattleView.OnTaskExecuted` 参照）
- 発火側は `Subject` が遅延初期化のため **null チェック（購読者ゼロ）で即 return する定型**を守る（`BattleEventBridge` 全 Fire メソッド参照）
- `Begin`/`End` は Interlocked だが `Wait` との競合まではケアされていない。**メインスレッド上での使用が前提**
- `ObservableEx.FromUniTask` は Client 側での直接使用ゼロ（2026-07時点）。基盤内（`SceneManagerBase` / `AssetBundleManager` / `FileDownloader` / `ParticlePlayer` / `PatternImage` / `TimeLinePlayer` 等）で `.Share()` による多重実行防止や `Timeout` 適用に使われている。Client 側で「UniTask に Rx オペレータを掛けたい」時はこれを使う（自作しない）

## 関連

- [Extensions/Core.md](../Extensions/Core.md) — `Scope`（AsyncHandlerScope の基底）/ `LifetimeDisposable`（`.AddTo(Disposable)` の実体）
- [Extensions/Methods.md](../Extensions/Methods.md) — `UniTaskExtensions`（UniRx互換シム: `ObserveEveryValueChanged` / `TakeUntilDestroy` / `DoOnError` / `OnErrorRetry` / `ToUniTask` 等）
- [UniTask](UniTask.md) — UniTask の PlayerLoop 初期化（本モジュールの依存ライブラリ）
- [UI](UI.md) — `UIButton.OnClickAsObservable()` 等の R3 ベース UI イベント

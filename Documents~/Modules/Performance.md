# Performance

> **namespace**: `Modules.Performance`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Performance/`（`FrameCallLimiter.cs` の1ファイルのみ）
> **Client側使用**: using は1ファイル（2026-07時点）。ただし**未使用のusing**で、実際の使用箇所は基盤内部（Master更新・ExternalAsset更新）
> **依存**: UniTask / R3

## 概要

1フレーム内で実行する後続処理の回数を制限するリミッター `FunctionFrameLimiter` を提供する。
大量のループ処理（数百件のアセット更新・マスター更新等）を複数フレームに分割し、スパイクを防ぐ。
FPS計測などの計測機能は**ない**（モジュール名から誤解しやすい）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 大量ループを1フレームN件ずつに分割実行したい | `new FunctionFrameLimiter(N)` → ループ内で `await limiter.Wait()` |
| フレーム内カウントを手動でリセットしたい | `FunctionFrameLimiter.Reset()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `FunctionFrameLimiter` | sealed class（非MonoBehaviour） | 1フレームあたりの実行数制限。上限到達時は `UniTask.NextFrame` で次フレームまで待機 |

※ ファイル名は `FrameCallLimiter.cs` だがクラス名は `FunctionFrameLimiter`。grep 時に注意。

## 使い方(実例)

Client側コードでの直接使用実績はない（`ContentsUpdateManager.cs` に using があるのみで未使用）。以下は基盤内の実例。

### 実例1: マスター更新の分割実行（1フレーム50件）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/Master/MasterManager.cs
var frameCallLimiter = new FunctionFrameLimiter(50);

async UniTask MasterUpdate(IMaster master, string masterVersion)
{
    // Master.Update 内部で frameCallLimiter.Wait() が呼ばれ、1フレーム50件に制限される.
    var updateResult = await master.Update(masterVersion, frameCallLimiter, linkedCancelToken);
    ...
}
```

### 実例2: アセット更新呼び出しの制限（1フレーム150件）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cs
// 初期化時.
updateAssetCallLimiter = new FunctionFrameLimiter(150);

// UpdateAsset 内（大量に並列呼び出しされる箇所）.
// 呼び出し制限.
await updateAssetCallLimiter.Wait(cancelToken: CancellationToken.None);
```

### 最小の想定例（新規で使う場合）

```csharp
var limiter = new FunctionFrameLimiter(30);

foreach (var item in items)
{
    await limiter.Wait(cancelToken: cancelToken);

    Process(item);
}
```

## API(主要公開メンバー)

### FunctionFrameLimiter

| メンバー | 説明 |
|---|---|
| `FunctionFrameLimiter(ulong max)` | コンストラクタ。`max` = 1フレームで許可する実行数 |
| `Wait(ulong increment = 1, CancellationToken cancelToken = default) : UniTask` | 実行枠を1つ（increment分）消費。フレーム内の上限到達時は次フレームまで待つ |
| `Reset()` | 現在フレームの消費カウントを0に戻す |

## 注意点・罠

- **クラス名とファイル名が不一致**（`FrameCallLimiter.cs` ≠ `FunctionFrameLimiter`）
- 初回インスタンス生成時に static な `Observable.EveryUpdate` 購読で `Time.frameCount` をキャッシュし始める（全インスタンス共有・解除されない）。メインスレッドの Unity フレームに同期する仕組みのため、**メインスレッドからの利用が前提**
- `Wait()` は上限未達なら即時リターン（アロケーションなし）。1件ずつのループに挟むだけでよい
- 複数インスタンスは独立してカウントする（Master用50 / ExternalAsset用150 のように用途別に生成）
- インスタンスを使い回す場合、前回処理の途中カウントが残ることがあるため必要に応じて `Reset()`

## 関連

- [Master](Master.md) — `Master.Update` が `FunctionFrameLimiter` を引数に取る（マスター更新の分割実行）
- [ExternalAsset](ExternalAsset.md) — アセット更新呼び出しの制限に使用

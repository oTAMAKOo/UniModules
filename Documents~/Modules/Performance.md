# Performance

> **namespace**: `Modules.Performance`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Performance/`（`FrameCallLimiter.cs` の1ファイルのみ）
> **Client側使用**: using は1ファイル（2026-07時点）。ただし**未使用のusing**で、実際の使用箇所は基盤内部（Master更新・ExternalAsset更新）
> **依存**: UniTask / R3

## 概要

1フレーム内で実行する後続処理の回数を制限するリミッター `FunctionFrameLimiter`（sealed class・非MonoBehaviour。唯一のクラス）を提供する。
大量のループ処理（数百件のアセット更新・マスター更新等）を複数フレームに分割し、スパイクを防ぐ。上限到達時は `UniTask.NextFrame` で次フレームまで待機する。
FPS計測などの計測機能は**ない**（モジュール名から誤解しやすい）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 大量ループを1フレームN件ずつに分割実行したい | `new FunctionFrameLimiter(N)` → ループ内で `await limiter.Wait()` |
| フレーム内カウントを手動でリセットしたい | `FunctionFrameLimiter.Reset()` |

## 使い方

Client側コードでの直接使用実績はない（`ContentsUpdateManager.cs` に using があるのみで未使用）。以下は基盤内の実例。

- マスター更新の分割実行（1フレーム50件）: `new FunctionFrameLimiter(50)` を `Master.Update(masterVersion, frameCallLimiter, cancelToken)` に渡し、内部で `Wait()` が呼ばれる。実例: `Client/Assets/UniModules/Scripts/Modules/Master/MasterManager.cs`
- アセット更新呼び出しの制限（1フレーム150件）: 初期化時に `new FunctionFrameLimiter(150)` を生成し、大量に並列呼び出しされる `UpdateAsset` 内で `await updateAssetCallLimiter.Wait(...)`。実例: `Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cs`
- 新規で使う場合の基本形: `new FunctionFrameLimiter(N)` を生成し、ループ内の処理前に `await limiter.Wait(cancelToken: cancelToken)` を挟むだけ

## 注意点・罠

- **クラス名とファイル名が不一致**（`FrameCallLimiter.cs` ≠ `FunctionFrameLimiter`）。grep 時に注意
- 初回インスタンス生成時に static な `Observable.EveryUpdate` 購読で `Time.frameCount` をキャッシュし始める（全インスタンス共有・解除されない）。メインスレッドの Unity フレームに同期する仕組みのため、**メインスレッドからの利用が前提**
- `Wait()` は上限未達なら即時リターン（アロケーションなし）。1件ずつのループに挟むだけでよい
- 複数インスタンスは独立してカウントする（Master用50 / ExternalAsset用150 のように用途別に生成）
- インスタンスを使い回す場合、前回処理の途中カウントが残ることがあるため必要に応じて `Reset()`

## 関連

- [Master](Master.md) — `Master.Update` が `FunctionFrameLimiter` を引数に取る（マスター更新の分割実行）
- [ExternalAsset](ExternalAsset.md) — アセット更新呼び出しの制限に使用

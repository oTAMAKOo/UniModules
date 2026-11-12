# Performance

1フレームの処理回数を制限する機能。
大量のループ処理を、1フレームに決めた件数ずつ、複数フレームに分けて実行できる。

## 基本情報

> **namespace**: `Modules.Performance`
> **場所**: `Modules/Performance/`（`FrameCallLimiter.cs` の1ファイルのみ）
> **依存**: UniTask / R3

## 概要

1フレーム内で実行する後続処理の回数を制限するリミッター `FunctionFrameLimiter`（sealed class・非MonoBehaviour。唯一のクラス）を提供する。
大量のループ処理（数百件のアセット更新・マスター更新等）を複数フレームに分割し、スパイクを防ぐ。上限到達時は `UniTask.NextFrame` で次フレームまで待機する。
FPS計測などの計測機能は**ない**（モジュール名から誤解しやすい）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 大量ループを1フレームN件ずつに分割実行したい | `new FunctionFrameLimiter(N)`（`N` は `ulong`） → ループ内で `await limiter.Wait()` |
| 重い処理を1回で複数件ぶんとして数えたい | `await limiter.Wait(increment)`（`Wait(ulong increment = 1, CancellationToken cancelToken = default)`） |
| フレーム内カウントを手動でリセットしたい | `FunctionFrameLimiter.Reset()` |

## 使い方

基盤内の実例:

- マスター更新の分割実行（1フレーム50件）: `new FunctionFrameLimiter(50)` を `Master.Update(masterVersion, frameCallLimiter, cancelToken)` に渡し、内部で `Wait()` が呼ばれる。引用元: `Modules/Master/MasterManager.cs` / `Modules/Master/Master.cs`
- アセット更新呼び出しの制限（1フレーム150件）: 初期化時に `new FunctionFrameLimiter(150)` を生成し、大量に並列呼び出しされる `UpdateAsset` 内で `await updateAssetCallLimiter.Wait(...)`。引用元: `Modules/ExternalAssets/ExternalAsset.cs`
- 新規で使う場合の基本形: `new FunctionFrameLimiter(N)` を生成し、ループ内の処理前に `await limiter.Wait(cancelToken: cancelToken)` を挟むだけ

## 注意点・罠

- **クラス名とファイル名が不一致**（`FrameCallLimiter.cs` ≠ `FunctionFrameLimiter`）。grep 時に注意
- 初回インスタンス生成時に static な `Observable.EveryUpdate` 購読で `Time.frameCount` をキャッシュし始める（全インスタンス共有・解除されない）。メインスレッドの Unity フレームに同期する仕組みのため、**メインスレッドからの利用が前提**
- `Wait()` は上限未達なら即時リターン（アロケーションなし）。1件ずつのループに挟むだけでよい
- 複数インスタンスは独立してカウントする（用途別に生成）
- カウントはフレームが変わった後の最初の `Wait()` で自動的に 0 へ戻る。`Reset()` は同じフレーム内でカウントを 0 へ戻したいときに使う
- 上限の判定は加算の前に行う（`max <= current` なら次フレームまで待つ）。`increment` に 2 以上を渡すと、1フレームの合計が上限を超えることがある

## 関連

- [Master](../Master/Manual.md) — `Master.Update` が `FunctionFrameLimiter` を引数に取る（マスター更新の分割実行）
- [ExternalAssets](../ExternalAssets/Manual.md) — アセット更新呼び出しの制限に使用

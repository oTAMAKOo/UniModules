# DoTweenExtension

DOTween の動きをまとめて制御する機能。
Tween を終了まで待って再生でき、登録した Tween の速度変更・一時停止・一括停止ができる。
DOTween と、UniTask の DOTween 連携のシンボルが必要。

## 基本情報

> **namespace**: `Modules.DoTweenExtension`
> **場所**: `Modules/DoTweenExtension/`（`TweenController.cs` の1ファイルのみ）
> **依存**: DOTween / UniTask（`UNITASK_DOTWEEN_SUPPORT` 定義が必要） / R3 / Extensions（`LifetimeDisposable`）/ Modules.TimeUtil（`TimeScale`）

## 概要

DOTween の `Tweener` を「await 可能・キャンセル対応・**再生速度一括制御**」で実行するコントローラ。
`Time.timeScale` を使わずに管理下の全 Tween の `timeScale` を書き換える方式のため、UI や他演出に影響を与えずに特定グループ（同一 `TweenController` に登録した演出）だけ倍速/停止できる。
DOTween を単発で使う分には本モジュールは不要。**速度連動・一括Killが必要なグループ演出**で使う。
主要クラス: `TweenController`（sealed・非MonoBehaviour・`LifetimeDisposable` 継承。Tweener の await 再生・実行中リスト管理・TimeScale 一括反映・一括 Kill）の1クラスのみ。利用側はグループ演出の Singleton 等でラップし、寿命に合わせて `KillAllTweeners()` を呼ぶ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Tween を await で再生したい（キャンセル対応） | `await tweenController.Play(tweener, cancelToken)`（`cancelToken` は省略可） |
| 再生中の Tween 全部の速度を変えたい | `tweenController.TimeScale = 2f;`（以降に `Play` する Tween にも適用される） |
| 演出を一時停止したい（グループ単位） | `TimeScale = 0f`（再開は元の値に戻す） |
| 管理下の Tween を全部止めたい | `KillAllTweeners()` |

## 使い方

- 生成 → 解放: 所有者クラスで `new TweenController()`、解放時に `KillAllTweeners()`
- Tween の作成 → Play の定石: `DOAnchorPosY(...).SetEase(...).SetLink(gameObject)` で作成 → `await tweenController.Play(tweener, cancelToken)`。fire-and-forget は `.Forget()`

## 注意点・罠

- `Play()` は渡された Tweener を一度 `Pause()` してから再生する。**自動再生済みの Tween を渡しても最初から意図通り制御される**が、`Play()` を通さない Tween は TimeScale 制御の対象外。await 中のキャンセルは例外にならず握りつぶされる（その他例外は `Debug.LogException`）
- Tween 作成時は `SetLink(gameObject)` を付けるのが安全（GameObject 破棄時に自動 Kill され、`Play()` の await も解ける）。付けないと破棄済みオブジェクトを対象に Tween が走り続ける恐れ
- `KillAllTweeners()` は **Complete させない**（途中の値のまま停止）。終了値を保証したい演出は個別に `Complete()` を検討
- `TimeScale = 0` は「一時停止」。DOTween 側の `Pause()` は使っていないので、再開は値を戻すだけでよい
- `TweenController` は `LifetimeDisposable` 派生。使い捨てにせず、所有者が寿命管理し、解放時に `KillAllTweeners()` を呼ぶ
- グループ演出で新規に Tween を使う場合は **直接 DOTween を再生せず `tweenController.Play()` を通す**こと（速度制御・一時停止に連動させるため。`Time.timeScale` は使わない方針）

## 関連

- [TimeUtil](../TimeUtil/Manual.md) — `TimeScale`（値変更通知付きの倍率ホルダ。本モジュールの速度制御の実体）
- [UniTaskExtension](../UniTaskExtension/Manual.md) — UniTask まわりの拡張（`Play()` 内の `tweener.ToUniTask()` は UniTask 本体の DOTween 連携で、`UNITASK_DOTWEEN_SUPPORT` が必要）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `LifetimeDisposable` / `Singleton<T>`

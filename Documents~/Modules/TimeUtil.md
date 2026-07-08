# TimeUtil

> **namespace**: `Modules.TimeUtil`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TimeUtil/`
> **Client側使用**: 0ファイル（2026-07時点。基盤内では `DoTween/TweenController` が `TimeScale` を使用）
> **依存**: R3 / Extensions（`Singleton<T>`, `LifetimeDisposable`, `XDouble`, `ToUnixTime`）

## 概要

時間関連の小型ユーティリティ集。サーバー基準時刻の保持（`TimeManager<T>`）、指定時刻到達の通知（`TimeNotice`）、時間経過で回復する値＝スタミナ類の計算（`RecoveryValue`）、演出用タイムスケール（`TimeScale`）、`Time.timeScale` 非依存の実時間（`RealTime`）の5クラス。ガード無しで全てコンパイルされているが、Client側からの直接使用はない。

**注意**: 本プロジェクトの時刻基盤は `SystemModel`（`Client/Assets/Scripts/Client/Model/System/SystemModel.time.cs`）が独自実装しており、`TimeManager<T>` は使っていない。規約の「`DateTime.Now` 禁止 → `systemModel.LocalTime`」もそちらを指す。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **現在時刻を取得したい（本プロジェクト）** | `systemModel.LocalTime` / `CurrentUnixTime`（`SystemModel.time.cs`）。本モジュールの `TimeManager` ではない |
| スタミナ等「時間で回復する値」の計算 | `RecoveryValue`（現在値・次回/全回復までの残り時間・割合）。**Client側で再発明する前に検討** |
| 指定時刻になったら1回だけ通知 | `TimeNotice.Set(name, unixTime)` + `OnTimeAsObservable()`（要 `Initialize`）、または `TimeManager<T>.Notice(dateTime)` |
| 演出の再生速度を購読可能な形で持つ | `TimeScale`（`Value` 変更 → `OnTimeScaleChangedAsObservable`）。DOTween連動は [DoTween](DoTween.md) の `TweenController.TimeScale` |
| `Time.timeScale` の影響を受けない経過時間 | `RealTime.time` / `RealTime.deltaTime`（static） |
| サーバー時刻を基準に進む「今」 | `TimeManager<T>` 派生の `Set(baseTime)` → `Now`（本プロジェクトでは `SystemModel` が同等機能を独自実装済み） |

## 使い方

- Client側の使用例なし。基盤内の実使用は `TweenController` のみ（`new TimeScale()` + `OnTimeScaleChangedAsObservable` 購読で再生中の全 Tweener の timescale へ反映。引用元: `Client/Assets/UniModules/Scripts/Modules/DoTween/TweenController.cs`）
- スタミナ計算の想定形（実在コードではない）: `new RecoveryValue(max, recoveryInterval, recoveryAmount, lastRecoveryTime, fullRecoveryTime)` → `UpdateTime(systemModel.LocalTime)` で経過分回復 → `GetNextRecoveryTime` / `GetFullRecoveryTime` / `GetRatio` で残り時間・割合取得（シグネチャは `RecoveryValue.cs` 参照）

## 注意点・罠

- **時刻取得の正は `SystemModel`**。`TimeManager<T>` / `TimeNotice` を新規採用すると時刻系が二重管理になる。使う場合は `TimeNotice.Initialize` に `systemModel.CurrentUnixTime` を渡す等、`SystemModel` 基準に統一すること。
- `TimeNotice` は `Initialize` 必須（未初期化で `Set` を呼ぶと `timers` が null で NullReference）。また通知は Update 駆動のため、アプリ非アクティブ中の到達は復帰後の次フレームで発火する。
- `TimeScale` は名前に反して `UnityEngine.Time.timeScale` を変更しない（値と通知だけ）。戦闘の倍速制御はこれではなく `BattleManager.GetSpeedScale()` パターン（Client側）を使う。
- `RecoveryValue.GetNextRecoveryTime` は `LastRecoveryTime + RecoveryInterval` から算出する。`UpdateTime` を定期的に呼んで LastRecoveryTime を進めておくこと（呼ばずに放置すると回復期限超過として Zero が返る）。現在値は内部でメモリ改竄対策の `XDouble` 保持。
- `RealTime.deltaTime` は 0〜1 秒に Clamp される（長フレームスパイク対策。1秒超の実デルタは取れない）。
- `TimeManager<T>` は abstract + 自己参照ジェネリクス。使うには `sealed class GameTime : TimeManager<GameTime>` の様な派生定義が必要。

## 関連

- [DoTween](DoTween.md) — `TweenController` が `TimeScale` を利用（基盤内唯一の実使用）
- [Scenario](Scenario.md) — `ScenarioController.TimeScale` として利用（未使用モジュール）
- [PlayFab](PlayFab.md) — 本プロジェクトの時刻の源泉（サーバー時間取得 → `SystemModel`）

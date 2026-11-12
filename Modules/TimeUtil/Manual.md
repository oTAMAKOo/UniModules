# TimeUtil

時間まわりの小さな道具をまとめた機能。
基準時刻から進む現在時刻、指定時刻の通知、時間で回復する値の計算、一定間隔の通知などができる。

## 基本情報

> **namespace**: `Modules.TimeUtil`
> **場所**: `Modules/TimeUtil/`（`TimeManager.cs` / `TimeNotice.cs` / `RecoveryValue.cs` / `TimeScale.cs` / `RealTime.cs` / `PeriodicTickManager.cs` の6ファイル）
> **依存**: R3 / Extensions（`Singleton<T>`, `LifetimeDisposable`, `XDouble`, `ToUnixTime`）/ [Devkit](../Devkit/Manual.md) の `UnityConsole`（`PeriodicTickManager` の開発ログ）

## 概要

時間関連の小型ユーティリティ集。サーバー基準時刻の保持（`TimeManager<T>`）、指定時刻到達の通知（`TimeNotice`）、時間経過で回復する値＝スタミナ類の計算（`RecoveryValue`）、演出用タイムスケール（`TimeScale`）、`Time.timeScale` 非依存の実時間（`RealTime`）、時刻の境界ごとに順番どおり通知する定期通知（`PeriodicTickManager<T>`）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| サーバー時刻を基準に進む「今」を扱いたい | `TimeManager<T>` を派生（例: `sealed class GameTime : TimeManager<GameTime>`）→ `Set(baseTime)` → `Now` |
| スタミナ等「時間で回復する値」の計算 | `RecoveryValue`（現在値 `Current`・`IsMax`・次回/全回復までの残り時間・割合） |
| 回復する値を消費・付与で書き換えたい / 最大値を変えたい | `RecoveryValue.SetValue(value, currentTime)` / `SetMaxValue(maxValue, currentTime)`（全回復時刻を計算し直す） |
| 指定時刻になったら1回だけ通知 | `TimeNotice.Set(name, unixTime)`（`DateTime` 版もある）+ `OnTimeAsObservable()`（要 `Initialize(現在時刻を返す関数)`）、または `TimeManager<T>.Notice(dateTime)` |
| 名前を付けた通知が登録済みか調べたい | `TimeNotice.IsExists(name)` |
| 演出の再生速度を購読可能な形で持つ | `TimeScale`（`Value` 変更 → `OnTimeScaleChangedAsObservable`。`Reset()` で `DefaultTimeScale` = 1 へ戻す）。DOTween連動は [DoTweenExtension](../DoTweenExtension/Manual.md) の `TweenController.TimeScale` |
| `Time.timeScale` の影響を受けない経過時間 | `RealTime.time` / `RealTime.deltaTime`（static） |
| 1 秒・1 分など一定間隔の処理を、決まった順番・例外の隔離つきで 1 か所から呼びたい | `PeriodicTickManager<T>` を派生して `GetCurrentTime()` を実装 → 購読側は `OnTickAsObservable(間隔秒, 順番)` → 準備が整ったら `StartTick()` |

## 使い方

- 基盤内の実使用は `TimeScale` のみで、`TweenController`（`new TimeScale()` + `OnTimeScaleChangedAsObservable` 購読で再生中の全 Tweener の timescale へ反映。引用元: `Modules/DoTweenExtension/TweenController.cs`）と `ScenarioController`（`TimeScale` プロパティとして公開。引用元: `Modules/Scenario/ScenarioController.cs`）が使う
- `TimeNotice` の想定形: `TimeNotice.Initialize(() => 現在の UnixTime)` → `TimeNotice.Set(name, 時刻)` → `OnTimeAsObservable()` で `TimeNotice.Timer`（`Name` / `UnixTime`）を受け取る。同じ `name` で `Set` し直すと時刻を上書きする。通知した登録は自動で消える
- スタミナ計算の想定形: `new RecoveryValue(max, recoveryInterval, recoveryAmount, lastRecoveryTime, fullRecoveryTime)` → `UpdateTime(currentTime)` で経過分回復 → `GetNextRecoveryTime` / `GetFullRecoveryTime` / `GetRatio` で残り時間・割合取得（シグネチャは `RecoveryValue.cs` 参照）
- `TimeManager<T>` は abstract + 自己参照ジェネリクス。使うには `sealed class GameTime : TimeManager<GameTime>` の様な派生定義が必要

### PeriodicTickManager

- abstract + 自己参照ジェネリクスの Singleton。派生で `protected override long GetCurrentTime()`（現在時刻の秒。0 以下は時刻未確定として通知しない）を実装する
- `OnTickAsObservable(intervalSeconds, order = DefaultObserverOrder)`: 間隔 N 秒の購読は、時刻が N の倍数を跨いだフレームで 1 回通知される（1 フレームで複数の境界を跨いでも 1 回）。購読先は `order` の小さい順、同じ `order` は購読順。通知内容は `TickInfo`（`IntervalSeconds` / `Time` / `Reason`）
- `StartTick()`: 全購読先へ `TickReason.Initial` を通知してから時刻の判定を始める（時刻未確定なら確定したフレームで初回通知）。`StopTick()` / `SuspendTick()` / `ResumeTick()`（再開時は全購読先へ `TickReason.Resume`）。状態は `IsRunning` / `IsSuspended` で読める
- `intervalSeconds` が 0 以下だと `ArgumentOutOfRangeException`。購読先の例外は `Debug.LogError` / `Debug.LogException` に出して、残りの購読先を続けて呼ぶ。派生からは `OnTickAsObservable(intervalSeconds, order, label)`（protected）で例外ログ用の名前を付けられる
- 派生で `CreateDispatchScope()` を上書きすると、1 回の通知処理全体を任意のスコープで囲める
- 派生で `OnStartTick()` / `OnStopTick()` を上書きすると、開始時（初回通知より前）・停止時の処理を足せる
- 派生で `TickFrameProvider` を上書きすると時刻を判定するフレームを変えられる（既定は `PreLateUpdate`）

## 注意点・罠

- `TimeNotice` は `Initialize` 必須（未初期化で `Set` を呼ぶと `timers` が null で NullReference）。また通知は Update 駆動のため、アプリ非アクティブ中の到達は復帰後の次フレームで発火する
- `TimeNotice` が通知するのは、前回の Update の時刻より後で今回の時刻以前の登録だけ（`prevTime < UnixTime <= currentTime`）。`Set` した時点ですでに過ぎている時刻は通知されず、登録も残り続ける
- `TimeManager<T>.Now` は `Set(baseTime)` からの `Time.realtimeSinceStartup` の経過を基準時刻に足した値。`Set` を呼ぶ前は基準時刻が `DateTime` の既定値のままなので、正しい時刻にならない
- `TimeManager<T>.Notice` は購読ごとに `Observable.EveryUpdate` で毎フレーム `Now` と比べ、到達したフレームで1回流して完了する。すでに過ぎている時刻を渡すと次の Update で流れる
- `TimeScale` は名前に反して `UnityEngine.Time.timeScale` を変更しない（値と通知だけ）
- `RecoveryValue.GetNextRecoveryTime` は `LastRecoveryTime + RecoveryInterval` から算出する。`UpdateTime` を定期的に呼んで LastRecoveryTime を進めておくこと（呼ばずに放置すると回復期限超過として Zero が返る）。現在値は内部でメモリ改竄対策の `XDouble` 保持
- `RecoveryValue` のコンストラクタは現在値を引数に取らず、`fullRecoveryTime - lastRecoveryTime` から現在値を逆算する（どちらかが null なら最大値）。引数の `max` / `recoveryInterval` / `recoveryAmount` は `float`
- `RealTime.deltaTime` は 0〜1 秒に Clamp される（長フレームスパイク対策。1秒超の実デルタは取れない）。再生中でないとき（`Application.isPlaying` が false）は `deltaTime` は 0、`time` は `Time.realtimeSinceStartup` を返す
- `PeriodicTickManager` は `StartTick()` より後に購読した購読先へ初回通知を出さない。開始時の状態は購読側で作る
- `PeriodicTickManager` の通知は「前回からの経過」を渡さない。経過時間に応じた処理は購読側で自分の時刻から計算する（フレームの遅延・一時停止・初回通知で間隔ちょうどにならない）
- `PeriodicTickManager` は停止（`StopTick` / `Refresh`）しても購読を保持する。購読の解除は購読側の `Dispose`（`AddTo`）で行う
- `PeriodicTickManager` の時刻が前回より戻った場合は通知せず、基準だけ合わせる

## 関連

- [DoTweenExtension](../DoTweenExtension/Manual.md) — `TweenController` が `TimeScale` を利用
- [Scenario](../Scenario/Manual.md) — `ScenarioController.TimeScale` として利用

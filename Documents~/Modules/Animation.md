# Animation

> **namespace**: `Modules.Animation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Animation/`
> **Client側使用**: 約5ファイル（2026-07時点）
> **依存**: UniTask / R3 / Unity.Linq（StateMachineTrigger）/ Extensions（`UnityUtility`, `AnimatorExtensions.IsAvailable`）

## 概要

Unity の Animator（AnimatorController）による演出再生を「**ステート名指定で再生 → await で終了まで待てる**」形に包む基盤。中心は `AnimationPlayer` で、UI・演出アニメ（Open/Close/In/Out 等）を UniTask フローに直列で組み込める。
加えて、Animator ステートの Enter/Exit をコードへ通知する `StateMachineTrigger`（Animator 側に付与）→ `StateMachineEventReceiver`（受信側）の仕組みを提供する。
生の `Animator.Play` + 終了ポーリングを自前で書かず、本モジュールを使うこと。
主要クラス: `AnimationPlayer`（**本モジュールの入口**。`[RequireComponent(typeof(Animator))]` `[ExecuteAlways]`、`StateMachineEventReceiver` 継承）/ `StateMachineTrigger`（StateMachineBehaviour。ステート Enter/Exit を祖先の Receiver へ送信）/ `StateMachineEvent`・`StateMachineEventType`（イベントデータ）/ `StateMachineParameter`（`Int/Float/Bool/Trigger/TriggerReset` 派生。`SetParameters()` 用）/ `ImmediateTransition`（Entry→初期ステートの1フレーム表示遅延を解消）/ `State`・`EndActionType`（enum）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| アニメを再生して終了まで待ちたい | `await animationPlayer.Play("ステート名")` |
| 再生だけして待たない | `animationPlayer.Play("ステート名").Forget()` |
| ループ再生したい | インスペクタ `End Action = Loop` + `Play().Forget()`（await は返ってこない） |
| 再生終了で自動非表示 / 自動破棄したい | `EndActionType.Deactivate` / `EndActionType.Destroy` |
| 再生開始 / 終了を購読したい | `OnEnterAnimationAsObservable()` / `OnEndAnimationAsObservable()` |
| 演出速度を変えたい（戦闘倍速対応） | `SpeedRate` プロパティ（例: `= BattleManager.GetSpeedScale()`） |
| 一時停止 / 再開したい | `Pause` プロパティ |
| Animator の Trigger / パラメータを操作したい | `SetTrigger()` / `ResetTrigger()` / `SetParameter<T>()` / `SetParameters()` |
| ステートの開始 / 終了をコードで受けたい | Animator のステートに `StateMachineTrigger` を付与 + `OnStateMachineEventAsObservable()` |
| AnimationClip 上のイベントを受けたい | クリップの AnimationEvent に関数 `Event`（string引数）を設定 + `OnAnimationEventAsObservable()` |
| Entry→初期ステートの1フレーム表示遅延を消したい | `ImmediateTransition` を Animator と同じ GameObject に付与 |
| 停止したい | `Stop()` |

## 使い方

### セットアップ（シリアライズ設定）

`AnimationPlayer` は AnimatorController 付きの GameObject にアタッチし、`[SerializeField]` で参照する（プロジェクト規約のフィールド例: `private AnimationPlayer animationController = null;`）。

| インスペクタ項目 | 既定値 | 意味 |
|---|---|---|
| `Stop On Awake` | true | 初期化時に停止状態にする（Play するまで再生されない） |
| `Ignore TimeScale` | true | `Animator.updateMode` を UnscaledTime にし `Time.timeScale` の影響を受けない |
| `End Action` | None | 再生終了時の挙動（None / Destroy / Deactivate / Loop） |

### 定型パターンと参照先

- **await で終了待ち（ウィンドウ開閉）**: `Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs`（`OnOpen` / `OnClose` で `await animationController.Play("Open" / "Close")`）
- **直列再生（In → Keep → Out）+ 倍速反映**: `Client/Assets/Scripts/Client/Scene/Battle/View/BattleUnit/Parts/BattleUnitActionNameView.cs`（`SpeedRate = BattleManager.GetSpeedScale()` → `await Play(StateIn)` → `Play(StateKeep).Forget()` でタップ待ち → `await Play(StateOut)`）
- **Trigger 駆動 + StateMachineEvent 受信（カットイン）**: `Client/Assets/Scripts/Client/Scene/Battle/Vfx/Cutin/CutInController.cs`（`OnStateMachineEventAsObservable()` を購読し、`Play(StateIdle).Forget()` + `SetTrigger` / `ResetTrigger` で遷移駆動。受信側は `EventType` × `ParameterName` で分岐）
- **待たない再生（タイトルのタップ待ち表示）**: `Client/Assets/Scripts/Client/Scene/Title/TitleView.cs`（`Play("Show").Forget()`）
- **再生し分け + 終了後に非表示**: `Client/Assets/Scripts/Client/Scene/Battle/Vfx/Direction/BattleStartDirectionController.cs`（`await Play(animationName)` → `UnityUtility.SetActive(animationPlayer, false)`）

## 注意点・罠

- `Play()` に渡すのは **Animator の「ステート名」**（Clip 名ではない）。存在しないと `Animation State Not found.` を LogError して即 return（例外にはならない）
- `Play()` は内部で自GameObjectを `SetActive(true)` するが、**親が非アクティブだと** `Animation can't play not active in hierarchy.` エラーで再生されない。親の活性化は呼び出し側の責務
- SetActive(true) に伴い OnEnable 系コンポーネント（TextSetter 等）が走るため、テキスト等の表示データは**有効化後に設定**する（BattleUnitActionNameView のコメント参照）
- `Stop On Awake = true`（既定）のため「シーンに置いただけでは再生されない」。必ず `Play()` を呼ぶ
- `EndActionType.Loop` 中は `await Play()` が**返ってこない**（キャンセルまでループ）。`Forget()` + CancellationToken で扱う
- `Ignore TimeScale = true`（既定）のため `Time.timeScale` では止まらない/遅くならない。**戦闘の倍速は `SpeedRate = BattleManager.GetSpeedScale()`**（プロジェクトの演出速度制御パターン）
- `cancelToken` によるキャンセルは await を抜けるだけで **Stop() はされない**（アニメは流れ続ける）。`OperationCanceledException` は内部で握りつぶされる
- `Stop()` は全 AnimationClip を最終フレームでサンプルするため、見た目が最終フレームで固定される
- `speedRate` は public フィールドだが、直接代入では Animator に反映されない。**`SpeedRate` プロパティ経由で設定**する
- `SetParameter<T>` は bool / int / float のみ対応（他型は ArgumentException）。`SetParameters()` は Animator 初期化完了を待ってパラメータを一括適用する（fire-and-forget）
- AnimationClip の AnimationEvent を受けるには、クリップ側イベントの関数名を `Event`（string 引数）にする
- OnEnable / OnDisable で Initialize / Stop する既存基盤（プロジェクトの「Unity ライフサイクルメソッド原則禁止」ルールの対象外・既存実装のためそのまま使う）
- `[ExecuteAlways]` のためエディタ非再生時も動作する。インスペクタ表示は専用の `AnimationPlayerInspector`（エディタ専用）

## 関連

- [Window](Window.md) — Client側 `WindowBase` が Open/Close アニメを AnimationPlayer で再生
- [InputControl](InputControl.md) — 演出中の入力ブロック（`Window.Open/Close` は自動、任意演出は `BlockInput`）
- [R3Extension](R3Extension.md) — Observable ⇔ UniTask 変換（終了通知を await したい場合等）
- [DoTween](DoTween.md) — コード駆動のトゥイーン演出（Animator を使わない軽量な動きはこちら）

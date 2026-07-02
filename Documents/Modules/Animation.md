# Animation

> **namespace**: `Modules.Animation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Animation/`
> **Client側使用**: 約5ファイル（2026-07時点）
> **依存**: UniTask / R3 / Unity.Linq（StateMachineTrigger）/ Extensions（`UnityUtility`, `AnimatorExtensions.IsAvailable`）

## 概要

Unity の Animator（AnimatorController）による演出再生を「**ステート名指定で再生 → await で終了まで待てる**」形に包む基盤。中心は `AnimationPlayer` で、UI・演出アニメ（Open/Close/In/Out 等）を UniTask フローに直列で組み込める。
加えて、Animator ステートの Enter/Exit をコードへ通知する `StateMachineTrigger`（Animator 側に付与）→ `StateMachineEventReceiver`（受信側）の仕組みを提供する。
生の `Animator.Play` + 終了ポーリングを自前で書かず、本モジュールを使うこと。

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

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `AnimationPlayer` | MonoBehaviour（`[RequireComponent(typeof(Animator))]` `[ExecuteAlways]` `[DisallowMultipleComponent]`、`StateMachineEventReceiver` 継承） | ステート名指定の再生・await 終了待ち・終了アクション・速度/ポーズ制御。**本モジュールの入口** |
| `StateMachineEventReceiver` | MonoBehaviour / `IStateMachineEventHandler` | `StateMachineTrigger` からのイベント受口（AnimationPlayer の基底クラス） |
| `StateMachineTrigger` | StateMachineBehaviour（Animator のステートに付与） | ステート Enter/Exit 時に祖先GameObjectの Receiver へ `StateMachineEvent` を送信 |
| `StateMachineEvent` / `StateMachineEventType` | class / enum | パラメータ名 + `EnterState` / `ExitState` のイベントデータ |
| `StateMachineParameter`（`Int/Float/Bool/Trigger/TriggerReset` 派生） | abstract class 群 | `SetParameters()` でまとめて Animator に適用するパラメータ表現 |
| `ImmediateTransition` | MonoBehaviour | 有効化直後に Entry ステートを強制的に抜けさせ、初期表示の1フレーム遅延を解消 |
| `State` / `EndActionType` | enum | 再生状態（Play/Pause/Stop）/ 終了時挙動（None/Destroy/Deactivate/Loop） |
| `AnimationPlayerInspector` | **エディタ専用**（CustomEditor） | `stopOnAwake` / `ignoreTimeScale` / `endActionType` のインスペクタ表示 |

### セットアップ（シリアライズ設定）

`AnimationPlayer` は AnimatorController 付きの GameObject にアタッチし、`[SerializeField]` で参照する（プロジェクト規約のフィールド例: `private AnimationPlayer animationController = null;`）。

| インスペクタ項目 | 既定値 | 意味 |
|---|---|---|
| `Stop On Awake` | true | 初期化時に停止状態にする（Play するまで再生されない） |
| `Ignore TimeScale` | true | `Animator.updateMode` を UnscaledTime にし `Time.timeScale` の影響を受けない |
| `End Action` | None | 再生終了時の挙動（None / Destroy / Deactivate / Loop） |

## 使い方(実例)

### await で終了待ち（ウィンドウ開閉）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs
[SerializeField]
private AnimationPlayer animationController = null;

protected override async UniTask OnOpen()
{
    SoundPlayer.Se(OpenSe).Forget();

    await animationController.Play("Open");
}

protected override async UniTask OnClose()
{
    SoundPlayer.Se(CloseSe).Forget();

    await animationController.Play("Close");
}
```

### 直列再生（In → Keep → Out）+ 倍速反映

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/View/BattleUnit/Parts/BattleUnitActionNameView.cs
// AnimationPlayer.Play が SetActive(true) する。OnEnable の TextSetter に上書きされないよう、有効化後にテキストを設定する.
UnityUtility.SetActive(animationPlayer.gameObject, true);

// 演出速度倍率を反映 (In/Out アニメ).
animationPlayer.SpeedRate = BattleManager.GetSpeedScale();

// In (表示).
await animationPlayer.Play(StateIn);

// Keep (タップ待ち中の表示維持).
animationPlayer.Play(StateKeep).Forget();

await WaitForTapNext();

// Out (退場).
await animationPlayer.Play(StateOut);

UnityUtility.SetActive(animationPlayer.gameObject, false);
```

### Trigger 駆動 + StateMachineEvent 受信（カットイン）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/Vfx/Cutin/CutInController.cs
animationPlayer.OnStateMachineEventAsObservable()
    .Subscribe(x => OnReceiveStateMachineEvent(x))
    .AddTo(this);

// 再生開始 (Idle ステートから Trigger で遷移させる).
animationPlayer.Play(StateIdle).Forget();

animationPlayer.SetTrigger(TriggerPlayDefault);
animationPlayer.ResetTrigger(TriggerEnd);

// 受信側: Animator の各ステートに付与した StateMachineTrigger から通知が来る.
private void OnReceiveStateMachineEvent(StateMachineEvent stateMachineEvent)
{
    switch (stateMachineEvent.EventType)
    {
        case StateMachineEventType.ExitState:
            switch (stateMachineEvent.ParameterName)
            {
                case "ExitIdle":
                    ExitIdleState().Forget();
                    break;
            }
            break;
    }
}
```

### 待たない再生（タイトルのタップ待ち表示）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Title/TitleView.cs
tapStartAnimation.Play("Show").Forget();
```

### 再生し分け + 終了後に非表示

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/Vfx/Direction/BattleStartDirectionController.cs
await animationPlayer.Play(animationName);

UnityUtility.SetActive(animationPlayer, false);
```

## API(主要公開メンバー)

### AnimationPlayer

| メンバー | 説明 |
|---|---|
| `UniTask Play(string animationName, int layer = -1, bool immediate = true, CancellationToken cancelToken = default)` | ステート再生し**終了まで待機**。内部で自GameObjectを SetActive(true)。immediate: 呼び出しフレーム内に対象ステートへ遷移確定 |
| `void Stop()` | 停止。全Clipを最終フレームでサンプルし Animator を無効化 |
| `void Initialize()` | Animator 取得・初期化（OnEnable で自動実行。手動呼び出しは通常不要） |
| `bool IsPlaying` / `State State` | 再生状態（State は Play / Pause / Stop） |
| `bool Pause`（get/set） | 一時停止/再開（Animator.speed を 0 ⇔ 復元） |
| `float SpeedRate`（get/set） | 再生速度倍率（DefaultSpeed × rate）。**設定は必ずプロパティ経由**（同名 public フィールド直代入では反映されない） |
| `void OverridePausedSpeed(float speed)` | ポーズ中に復帰後速度を上書き |
| `void ResetAnimatorSpeed()` | Animator.speed を DefaultSpeed に戻す |
| `EndActionType EndActionType`（get/set） | 終了時挙動（None / Destroy / Deactivate / Loop）を実行時変更 |
| `bool StopOnAwake`（get/set） | 初期化時に停止状態にするか |
| `void SetStartNormalizedTime(float normalizedTime)` / `float StartNormalizedTime` | 次回 Play の開始位置（0〜1） |
| `string CurrentAnimationName` | 再生中のステート名 |
| `Animator Animator` / `AnimationClip[] Clips` / `float DefaultSpeed` / `bool IsInitialized` | 内部 Animator と初期情報 |
| `void SetTrigger(string name)` / `void SetTrigger(int id)` / `void ResetTrigger(string name)` | Animator Trigger 操作（Animator 利用不可時は無視） |
| `void SetParameter<T>(string name, T value)` | bool / int / float のみ対応（他型は ArgumentException） |
| `void SetParameters(IEnumerable<StateMachineParameter> parameters)` | Animator 初期化完了を待ってパラメータ一括適用（fire-and-forget） |
| `UniTask<T[]> GetStateMachineBehavioursAsync<T>()` | StateMachineBehaviour 取得（Animator 初期化完了待ちのため非同期） |
| `Observable<AnimationPlayer> OnEnterAnimationAsObservable()` | 対象ステートへの遷移完了（再生開始）通知 |
| `Observable<AnimationPlayer> OnEndAnimationAsObservable()` | 再生終了通知 |
| `Observable<string> OnAnimationEventAsObservable()` / `void Event(string value)` | AnimationClip 上の AnimationEvent 中継（クリップ側の関数名は `Event`） |
| `Observable<StateMachineEvent> OnStateMachineEventAsObservable()`（基底） | StateMachineTrigger からの Enter/Exit イベント |

### StateMachineTrigger（Animator のステートに付与）

| メンバー | 説明 |
|---|---|
| SerializeField: `parameterName` / `eventType` | 通知時のパラメータ名と Enter/Exit 種別（Animator 側インスペクタで設定） |
| `void SendStateEvent<T>(Animator animator)` | 祖先の T（Receiver）へイベント送信（通常は OnStateEnter/Exit から自動送信） |

### StateMachineParameter 派生（SetParameters 用）

| クラス | コンストラクタ |
|---|---|
| `IntStateMachineParameter` | `(string parameterName, int value)` |
| `FloatStateMachineParameter` | `(string parameterName, float value)` |
| `BoolStateMachineParameter` | `(string parameterName, bool value)` |
| `TriggerStateMachineParameter` / `TriggerResetStateMachineParameter` | `(string parameterName)` |

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
- AnimationClip の AnimationEvent を受けるには、クリップ側イベントの関数名を `Event`（string 引数）にする
- OnEnable / OnDisable で Initialize / Stop する既存基盤（プロジェクトの「Unity ライフサイクルメソッド原則禁止」ルールの対象外・既存実装のためそのまま使う）
- `[ExecuteAlways]` のためエディタ非再生時も動作する。インスペクタ表示は専用の `AnimationPlayerInspector`（エディタ専用）

## 関連

- [Window](Window.md) — Client側 `WindowBase` が Open/Close アニメを AnimationPlayer で再生
- [InputControl](InputControl.md) — 演出中の入力ブロック（`Window.Open/Close` は自動、任意演出は `BlockInput`）
- [R3Extension](R3Extension.md) — Observable ⇔ UniTask 変換（終了通知を await したい場合等）
- [DoTween](DoTween.md) — コード駆動のトゥイーン演出（Animator を使わない軽量な動きはこちら）

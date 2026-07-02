# TimeLine

> **namespace**: `Modules.TimeLine`（Player・属性・共通型） / `Modules.TimeLine.Component`（Track/Clip/Behaviour）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TimeLine/`
> **Client側使用**: 0ファイル（2026-07時点）
> **依存**: Unity Timeline（`com.unity.timeline` 1.8.12、**導入済み**） / UnityEngine.Playables / UniTask / R3（`Modules.R3Extension.ObservableEx`） / Extensions

## 概要

Unity Timeline（`PlayableDirector`）の再生制御ラッパーと自作トラック3種（イベント呼び出し・ラベルジャンプ・区間ループ）を提供する基盤。Flash の `gotoAndPlay` 風のラベル制御や、クリップ区間に入った/出た瞬間に `[TimeLineEvent]` 付きメソッドをリフレクション実行する仕組みを持つ。

**本プロジェクトでは未使用（主要部はコンパイル対象外）**。中核クラスは `#if ENABLE_UNITY_TIMELINE` ガード内だが、シンボルは Scripting Define Symbols / `Client/Assets/csc.rsp` とも未定義。Timeline パッケージ自体は導入済みのため、シンボル定義のみで有効化できる可能性はある（動作未検証）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **Timeline演出を再生制御したい（本プロジェクト）** | 本モジュールは無効。`PlayableDirector` を直接使うか、有効化（`ENABLE_UNITY_TIMELINE` 定義）をユーザーに相談 |
| （有効時）Timeline再生・完了待ち | `TimeLinePlayer.Play(time)`（`Observable<Unit>` が完了で発火） |
| （有効時）ラベル位置へジャンプ再生/停止 | `TimeLinePlayer.GotoAndPlay(label)` / `GotoAndStop(label)`（`LabelTrack` 上のクリップ名がラベル） |
| （有効時）再生速度変更 | `TimeLinePlayer.SetSpeed(speed)` |
| （有効時）クリップ区間でC#メソッドを呼ぶ | 対象メソッドに `[TimeLineEvent]` を付け、`EventTrack` 上の `EventClip` に設定（Inspector編集） |
| （有効時）区間ループと脱出制御 | `LoopTrack` + `LoopClip`、`TimeLinePlayer.OnLoopCheckAsObservable()` で `LoopInfo.Loop = false` にすると脱出 |
| ExposedReference の実行時解決・差し替え | `ExposedReferenceResolver<T>`（**ガード外なので現状でも使用可**） |

## 主要クラス

| クラス | 種別 | 役割 | ガード |
|---|---|---|---|
| `TimeLinePlayer` | MonoBehaviour（`[RequireComponent(PlayableDirector)]`） | 中核。再生/一時停止/停止・ラベルジャンプ・速度変更・完了検知（`EveryUpdate` 監視）・ループ通知 | `ENABLE_UNITY_TIMELINE` |
| `TimeLineEventAttribute` | Attribute（`Modules.TimeLine`） | `EventTrack` から呼び出し可能なメソッドの目印。public・戻り値void・引数は string/int/float/bool/enum/GameObject のみ | なし |
| `EventTrack` / `EventClip` / `EventBehaviour` / `EventMixerBehaviour` | TrackAsset / PlayableAsset / PlayableBehaviour | クリップ区間の Enter/Stay/Exit タイミングで `EventMethod` を実行 | `ENABLE_UNITY_TIMELINE` |
| `EventMethod` | Serializable class（`LifetimeDisposable`） | 呼び出し対象（`ExposedReference<GameObject>`）＋メソッド名＋引数のシリアライズと、リフレクション `Invoke()` | なし |
| `LabelTrack` / `LabelClip` / `LabelBehaviour` | TrackAsset / PlayableAsset / PlayableBehaviour | ジャンプ先ラベル定義（クリップの `displayName` がラベル名。Behaviour は空実装） | `ENABLE_UNITY_TIMELINE` |
| `LoopTrack` / `LoopClip` / `LoopBehaviour` | TrackAsset / PlayableAsset / PlayableBehaviour | クリップ区間をループ再生（`OnBehaviourPause` で `director.time` を巻き戻し） | `ENABLE_UNITY_TIMELINE` |
| `LoopInfo` | class | ループ状態（`Label` / `Loop` フラグ）。購読側が `Loop = false` で脱出させる | なし |
| `ExposedReferenceResolver<T>` | class（汎用） | `ExposedReference<T>` の解決・値差し替え（`SetValue` でGUID発行）・`OnUpdateReferenceAsObservable` | なし |
| `EventClipInspector` / `EventMethodInfo` | Editor（`Event/Editor/`） | `EventClip` のカスタムインスペクタ（`[TimeLineEvent]` メソッド一覧から選択・引数編集） | Inspector のみガード有 |

## 使い方(実例)

Client側・基盤内とも使用例なし。実コードのシグネチャに基づく最小の想定例（`ENABLE_UNITY_TIMELINE` 定義が前提）。

```csharp
// 想定例（実在コードではない）. シグネチャは
// Client/Assets/UniModules/Scripts/Modules/TimeLine/TimeLinePlayer.cs 参照.
var player = UnityUtility.GetComponent<TimeLinePlayer>(gameObject);

// 完了待ち再生（R3: Observable<Unit>）.
await player.Play().FirstAsync();

// ラベルジャンプ・ループ脱出.
player.OnLoopCheckAsObservable()
    .Subscribe(info => info.Loop = !isSkip)
    .AddTo(this);

player.GotoAndPlay("Main").Subscribe().AddTo(this);
```

```csharp
// 想定例: EventTrack から呼ばれるメソッド定義（EventMethod.GetTimeLineEventMethods の条件）.
[TimeLineEvent]
public void OnTimelineEvent(string id, float value)
{
    // Inspector（EventClipInspector）でこのメソッドを選択して使う.
}
```

## API(主要公開メンバー)

### TimeLinePlayer

| メンバー | 説明 |
|---|---|
| `Play(double time = 0, bool resetIfPlaying = true) : Observable<Unit>` | 再生開始。戻り値は完了（`State.Finish`）まで待つ Observable |
| `Pause()` / `Stop()` | 一時停止 / 停止（`Stop` は状態を `Finish` にする） |
| `GotoAndPlay(string label) : Observable<Unit>` / `GotoAndStop(string label)` | `LabelTrack` のクリップ位置へジャンプ。ラベル不在時は即完了/何もしない |
| `SetSpeed(double speed)` | 全ルートPlayableの速度変更 |
| `SetTime(double time)` | 時間直接指定（前後で `Evaluate()`） |
| `OnLoopCheckAsObservable() : Observable<LoopInfo>` | `LoopClip` 区間終端ごとの通知。`LoopInfo.Loop` を書き換えて継続判定 |
| `CurrentTime` / `Speed` | 現在時間 / 速度 |

### EventMethod / ExposedReferenceResolver&lt;T&gt;

| メンバー | 説明 |
|---|---|
| `EventMethod.Setup(PlayableDirector)` / `Invoke()` / `Clear()` | 参照解決の初期化 / リフレクション実行（初回に `Build`）/ 参照クリア |
| `EventMethod.GetTimeLineEventMethods(GameObject) : IEnumerable<MethodInfo>` | static。`[TimeLineEvent]` 付き・void戻り・対応引数型のみのメソッド列挙 |
| `EventMethod.EventType` | `Enter` / `Stay` / `Exit`（`Modules.TimeLine.Component.EventType`） |
| `ExposedReferenceResolver<T>.GetValue()` / `SetValue(T)` / `Clear()` / `Resolve()` | 参照の取得 / 差し替え（新GUID発行） / 解除 |
| `ExposedReferenceResolver<T>.OnUpdateReferenceAsObservable()` | `SetValue/Clear` 時に更新後の `ExposedReference<T>` を通知（シリアライズ書き戻し用） |

## 注意点・罠

- **主要部はコンパイル対象外**（`ENABLE_UNITY_TIMELINE` 未定義）。ただし `ExposedReferenceResolver<T>` / `TimeLineEventAttribute` / `EventMethod` / `LoopInfo` / `LabelBehaviour` はガード外で現在もコンパイルされている（単体利用可）。
- `TimeLinePlayer.Play` の完了検知は「`PlayState.Paused` かつ経過時間が `duration` 以上」というヒューリスティック（`CheckFinish`）。Timeline 側の Wrap Mode 設定によっては完了が発火しない可能性がある。
- `EventMethod` の引数は string/int/float/bool/enum/GameObject のみ対応。enum は int として渡される。メソッド解決は「`DeclaringType.メソッド名`」の文字列一致で、リネームすると Inspector 設定が無言で壊れる（実行時 `Debug.LogError`）。
- `EventType` は `Modules.TimeLine.Component.EventType`。`UnityEngine.EventType`（IMGUI）と同名なので using によっては衝突する。
- `LoopBehaviour` は `OnBehaviourPause` でループ判定する癖のある実装。`LoopInfo.Loop` 初期値は true（購読側が false にしない限り無限ループ）。
- `TimeLinePlayer` は R3 の `Observable<Unit>` を返す（UniTask ではない）。await するには `FirstAsync()` 等で変換する。

## 関連

- [Animation](Animation.md) — 単発アニメ再生基盤（Timeline を使わない演出はこちらが主流）
- [R3Extension](R3Extension.md) — `ObservableEx.FromUniTask`（`Play` の戻り値実装に使用）
- [Scenario](Scenario.md) — 別系統のカットシーン基盤（Lua駆動。こちらも未使用）

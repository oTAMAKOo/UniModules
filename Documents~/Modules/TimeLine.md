# TimeLine

> **namespace**: `Modules.TimeLine`（Player・属性・共通型） / `Modules.TimeLine.Component`（Track/Clip/Behaviour）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TimeLine/`
> **Client側使用**: 0ファイル（2026-07時点）
> **依存**: Unity Timeline（`com.unity.timeline` 1.8.12、**導入済み**） / UnityEngine.Playables / UniTask / R3（`Modules.R3Extension.ObservableEx`） / Extensions

## 概要

Unity Timeline（`PlayableDirector`）の再生制御ラッパーと自作トラック3種（イベント呼び出し・ラベルジャンプ・区間ループ）を提供する基盤。Flash の `gotoAndPlay` 風のラベル制御や、クリップ区間に入った/出た瞬間に `[TimeLineEvent]` 付きメソッドをリフレクション実行する仕組みを持つ。

主要クラス: `TimeLinePlayer`（中核。再生/一時停止/停止・ラベルジャンプ・速度変更・完了検知・ループ通知）/ `EventTrack`・`LabelTrack`・`LoopTrack`（自作トラック3種、各 Clip/Behaviour 付き）/ `TimeLineEventAttribute`（呼び出し可能メソッドの目印）/ `EventMethod`（呼び出し対象＋引数のシリアライズとリフレクション実行）/ `LoopInfo`（ループ状態。購読側が `Loop = false` で脱出させる）/ `ExposedReferenceResolver<T>`（`ExposedReference<T>` の解決・差し替え。汎用）/ `EventClipInspector`（Editor）。

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

## 注意点・罠

- **主要部はコンパイル対象外**（`ENABLE_UNITY_TIMELINE` 未定義）。ただし `ExposedReferenceResolver<T>` / `TimeLineEventAttribute` / `EventMethod` / `LoopInfo` / `LabelBehaviour` はガード外で現在もコンパイルされている（単体利用可）。
- `TimeLinePlayer.Play` の完了検知は「`PlayState.Paused` かつ経過時間が `duration` 以上」というヒューリスティック（`CheckFinish`）。Timeline 側の Wrap Mode 設定によっては完了が発火しない可能性がある。
- `GotoAndPlay` / `GotoAndStop` はラベル不在時は即完了/何もしない。
- `[TimeLineEvent]` 対象メソッドの条件は public・戻り値void・引数は string/int/float/bool/enum/GameObject のみ。enum は int として渡される。メソッド解決は「`DeclaringType.メソッド名`」の文字列一致で、リネームすると Inspector 設定が無言で壊れる（実行時 `Debug.LogError`）。
- `EventType` は `Modules.TimeLine.Component.EventType`（`Enter` / `Stay` / `Exit`）。`UnityEngine.EventType`（IMGUI）と同名なので using によっては衝突する。
- `LoopBehaviour` は `OnBehaviourPause` でループ判定する癖のある実装。`LoopInfo.Loop` 初期値は true（購読側が false にしない限り無限ループ）。
- `TimeLinePlayer` は R3 の `Observable<Unit>` を返す（UniTask ではない）。await するには `FirstAsync()` 等で変換する。

## 関連

- [Animation](Animation.md) — 単発アニメ再生基盤（Timeline を使わない演出はこちらが主流）
- [R3Extension](R3Extension.md) — `ObservableEx.FromUniTask`（`Play` の戻り値実装に使用）
- [Scenario](Scenario.md) — 別系統のカットシーン基盤（Lua駆動。こちらも未使用）

# Particle

パーティクルをまとめて再生する機能。
配下の全 ParticleSystem を一括で再生・停止でき、再生の終了まで待てる。終了時の自動破棄や描画順の一括指定もできる。

## 基本情報

> **namespace**: `Modules.Particle`（Editor のインスペクタも同じ namespace）
> **場所**: `Modules/Particle/`（ParticlePlayer.cs / ParticlePlayerSortingOrder.cs + `Editor/` にインスペクタ2ファイル）
> **依存**: UniTask / R3 / Unity.Linq / Extensions（`UnityUtility`, `IsPlayback`, `GetSubemitters`）/ Modules.R3Extension

## 概要

配下の全 `ParticleSystem` を収集して一括制御する再生プレイヤー。`Play()` を await すると**再生終了まで待てる**のが素の ParticleSystem との最大の違い。
終了時アクション（破棄/非アクティブ/ループ）、SortingOrder 一括適用、再生速度倍率、時間/生死トリガーのイベント通知を持つ。
`[ExecuteAlways]` のためエディタ上でも再生プレビュー可能（専用インスペクタ付き）。
主要クラス: `ParticlePlayer`（一括再生・停止・終了検知・イベント発行） / `ParticlePlayerSortingOrder`（子 ParticleSystem 個別の SortingOrder 相対値。親 ParticlePlayer の基準値に加算） / `ParticlePlayer.EventInfo`（Time / Birth / Alive / Death トリガー + message のイベント定義。インスペクタ設定） / enum群（`State` / `EndActionType` / `LifecycleControl` / `LifecycleType`） / エディタ専用インスペクタ2種（非再生モードでの再生エミュレート等）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| エフェクトを再生して終了まで待ちたい | `await particlePlayer.Play()` |
| 再生終了を購読したい（プール返却等） | `OnEndAsObservable()` |
| 再生終了時に自動で非アクティブ化/破棄したい | `EndActionType = EndActionType.Deactivate / Destroy` |
| 再生を止めたい | `Stop(immediate, clear)` |
| 一時停止したい | `Pause = true / false` |
| 再生速度を変えたい | `SpeedRate = 2f`（`ResetSpeed()` で戻す） |
| 再生状態・経過時間を知りたい | `State`（`Play` / `Pause` / `Stop`）/ `CurrentTime` / `IsAlive()` |
| 描画順をまとめて変えたい | `SortingLayer`（配下の全 ParticleSystem の `sortingLayerID`）/ `SortingOrder`（`ParticlePlayerSortingOrder` を付けた ParticleSystem の基準値） |
| 子パーティクル単位で描画順をずらしたい | `ParticlePlayerSortingOrder`（相対値を加算。コードからは `Set(sortingOrder)`） |
| 再生中の特定タイミングで処理したい | `EventInfo`（インスペクタ設定）+ `OnEventAsObservable()`（設定した `message` が流れる） |
| AnimationClip から再生・中断したい | シリアライズフィールド `play` / `pause` をアニメーションで切り替える（毎フレーム監視して再生・停止・中断へ反映） |
| 有効化と同時に自動再生したい | インスペクタ `AutoPlay`（`activateOnPlay`。実行中のみ） |
| ループを含むエフェクトを決まった時間で終わらせたい | インスペクタ `Lifcycle Type = Manual` + `Life Time`（`LifecycleControl.Manual` / `lifeTime`） |
| ParticleSystem 単体の再生中判定だけしたい | `particleSystem.IsPlayback()`（[Extensions/Methods.md](../../Documents~/Extensions/Methods.md)。本モジュール不要） |

## 使い方

- **キャッシュ再利用パターン**（`EndActionType = EndActionType.Deactivate` + `OnEndAsObservable()` 購読でキューへ返却。Play 時に自動で `SetActive(true)` されるため Deactivate 済みインスタンスをそのまま再 `Play()` できる）: `Modules/TouchEffect/TouchEffectManager.cs`
- [ObjectPool](../ObjectPool/Manual.md) と組み合わせる場合も同様に「取得 → `Play()` → `OnEndAsObservable()` で `pool.Release()`」の形になる
- **終了まで待つ最小形**: `UnityUtility.Instantiate<ParticlePlayer>(parent, effectPrefab)` → `await effect.Play()`

## 注意点・罠

- **Unity ライフサイクル（OnEnable/OnDisable）で自動初期化・停止する既存実装**。OnEnable で `Initialize()` + `activateOnPlay` なら自動再生、OnDisable で `Stop(true, true)`。非アクティブ化＝強制停止になる点に注意
- 収集した全 ParticleSystem の `playOnAwake` は**強制的に false 化**される（制御を ParticlePlayer に一元化するため）
- 更新は Unity 標準再生ではなく **`Simulate()` による自前フレーム更新**（`ignoreTimeScale` 対応のため）。`Time.timeScale` を 0 にしても `ignoreTimeScale: true` なら動く
- 終了判定は `ParticleSystem.IsPlayback()` 拡張（`IsAlive()` バグ回避実装）。**ループ設定の ParticleSystem が1つでもあると自然終了しない**（常に生存扱い）→ ループエフェクトは `LifecycleControl.Manual` + `lifeTime` か明示 `Stop()` で止める
- `Play(restart: false)` は再スタートせず再生中の Observable に相乗りする。`OnEndAsObservable()` の通知は EndAction 実行後
- 子の増減時は `OnTransformChildrenChanged` で自動再収集されるが、既存子の ParticleSystem 差し替え等は ContextMenu「CollectContents」or 再アクティブ化が必要
- `Stop(immediate: false)` は放出停止→自然消滅を待ってから終了処理・状態リセットする（即時ではない）。引数の既定は `Stop(immediate = false, clear = true)`
- **`SortingOrder` は `ParticlePlayerSortingOrder` を付けた ParticleSystem にしか適用されない**（付いていない ParticleSystem の `sortingOrder` は変わらない）。`SortingLayer` は全 ParticleSystem に適用される
- **`SpeedRate` は設定のたびに現在の `simulationSpeed` へ掛け算される**。再生中に続けて設定すると倍率が累積するため、変更前に `ResetSpeed()` で元の速度へ戻す（`Play()` の開始時は元の速度に戻してから `SpeedRate` を適用する）
- **イベント（`EventInfo`）は実行中（`Application.isPlaying`）のみ発行**され、`message` が空のものは発行されない。`Time` は再生開始からの経過時間（`time`）、`Birth` / `Alive` / `Death` は `target` に指定した ParticleSystem の状態で判定する（`target` 未設定だと発行されない。`Alive` は生存中の毎フレーム発行）
- `EndActionType.Destroy` / `Deactivate` による破棄・非アクティブ化は実行中のみ行われる

## 関連

- [Extensions/Methods.md](../../Documents~/Extensions/Methods.md) — `IsPlayback()` / `GetSubemitters()`（ParticleSystem 拡張。単発の再生判定はこちらだけで足りる）
- [ObjectPool](../ObjectPool/Manual.md) — エフェクト使い回しの汎用プール（`EndActionType.Deactivate` + `OnEndAsObservable` で返却する構成が定石）
- [UI](../UI/Manual.md) — `UIParticleSystem`（uGUI 上でのパーティクル描画。namespace `Modules.UI.Particle` で本モジュールとは別物）

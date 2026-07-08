# Particle

> **namespace**: `Modules.Particle`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Particle/`（ParticlePlayer.cs / ParticlePlayerSortingOrder.cs + `Editor/` にインスペクタ2ファイル）
> **Client側使用**: using は1ファイル（2026-07時点。`Editor/TuneComponent/AdditionalComponent.cs` で実質未使用）。実利用は基盤の `TouchEffectManager`（タッチエフェクト）とプレハブ（`VfxTouchEffect.prefab` 等）経由
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
| 描画順をまとめて変えたい | `SortingOrder` / `SortingLayer` プロパティ |
| 子パーティクル単位で描画順をずらしたい | `ParticlePlayerSortingOrder`（相対値を加算） |
| 再生中の特定タイミングで処理したい | `EventInfo`（インスペクタ設定）+ `OnEventAsObservable()` |
| ParticleSystem 単体の再生中判定だけしたい | `particleSystem.IsPlayback()`（[Extensions/Methods.md](../Extensions/Methods.md)。本モジュール不要） |

## 使い方

Client側スクリプトからの直接利用は現状なし（プレハブ: `Resource (Internal)/Core/Effect/TouchEffect/VfxTouchEffect.prefab` 等に付与され、基盤 `TouchEffectManager` が制御）。

- **キャッシュ再利用パターン**（`EndActionType = EndActionType.Deactivate` + `OnEndAsObservable()` 購読でキューへ返却。Play 時に自動で `SetActive(true)` されるため Deactivate 済みインスタンスをそのまま再 `Play()` できる）: `Client/Assets/UniModules/Scripts/Modules/TouchEffect/TouchEffectManager.cs`
- [ObjectPool](ObjectPool.md) と組み合わせる場合も同様に「取得 → `Play()` → `OnEnd` で `pool.Return()`」の形になる
- **終了まで待つ最小形**（想定例）: `UnityUtility.Instantiate<ParticlePlayer>(parent, effectPrefab)` → `await effect.Play()`

## 注意点・罠

- **Unity ライフサイクル（OnEnable/OnDisable）で自動初期化・停止する既存実装**。OnEnable で `Initialize()` + `activateOnPlay` なら自動再生、OnDisable で `Stop(true, true)`。非アクティブ化＝強制停止になる点に注意
- 収集した全 ParticleSystem の `playOnAwake` は**強制的に false 化**される（制御を ParticlePlayer に一元化するため）
- 更新は Unity 標準再生ではなく **`Simulate()` による自前フレーム更新**（`ignoreTimeScale` 対応のため）。`Time.timeScale` を 0 にしても `ignoreTimeScale: true` なら動く
- 終了判定は `ParticleSystem.IsPlayback()` 拡張（`IsAlive()` バグ回避実装）。**ループ設定の ParticleSystem が1つでもあると自然終了しない**（常に生存扱い）→ ループエフェクトは `LifecycleControl.Manual` + `lifeTime` か明示 `Stop()` で止める
- `Play(restart: false)` は再スタートせず再生中の Observable に相乗りする。`OnEndAsObservable()` の通知は EndAction 実行後
- 子の増減時は `OnTransformChildrenChanged` で自動再収集されるが、既存子の ParticleSystem 差し替え等は ContextMenu「CollectContents」or 再アクティブ化が必要
- `Stop(immediate: false)` は放出停止→自然消滅を待ってから終了処理・状態リセットする（即時ではない）
- Client側 `AdditionalComponent.cs` の `using Modules.Particle` は現状未使用（自動アタッチ設定に ParticlePlayer は含まれていない）

## 関連

- [Extensions/Methods.md](../Extensions/Methods.md) — `IsPlayback()` / `GetSubemitters()`（ParticleSystem 拡張。単発の再生判定はこちらだけで足りる）
- [ObjectPool](ObjectPool.md) — エフェクト使い回しの汎用プール（`EndActionType.Deactivate` + `OnEndAsObservable` で返却する構成が定石）
- [UI](UI.md) — `UIParticleSystem`（uGUI 上でのパーティクル描画。namespace `Modules.UI.Particle` で本モジュールとは別物）

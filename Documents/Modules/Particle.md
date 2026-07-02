# Particle

> **namespace**: `Modules.Particle`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Particle/`（ParticlePlayer.cs / ParticlePlayerSortingOrder.cs + `Editor/` にインスペクタ2ファイル）
> **Client側使用**: using は1ファイル（2026-07時点。`Editor/TuneComponent/AdditionalComponent.cs` で実質未使用）。実利用は基盤の `TouchEffectManager`（タッチエフェクト）とプレハブ（`VfxTouchEffect.prefab` 等）経由
> **依存**: UniTask / R3 / Unity.Linq / Extensions（`UnityUtility`, `IsPlayback`, `GetSubemitters`）/ Modules.R3Extension

## 概要

配下の全 `ParticleSystem` を収集して一括制御する再生プレイヤー。`Play()` を await すると**再生終了まで待てる**のが素の ParticleSystem との最大の違い。
終了時アクション（破棄/非アクティブ/ループ）、SortingOrder 一括適用、再生速度倍率、時間/生死トリガーのイベント通知を持つ。
`[ExecuteAlways]` のためエディタ上でも再生プレビュー可能（専用インスペクタ付き）。

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

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `ParticlePlayer` | sealed MonoBehaviour（`[ExecuteAlways]`） | 配下 ParticleSystem の一括再生・停止・終了検知・イベント発行 |
| `ParticlePlayerSortingOrder` | sealed MonoBehaviour | 子 ParticleSystem 個別の SortingOrder 相対値（親 ParticlePlayer の基準値に加算） |
| `State` / `EndActionType` / `LifecycleControl` / `LifecycleType` | enum | 再生状態 / 終了時アクション / 生存管理方式（ParticleSystem 依存 or 手動 lifeTime） / 生死フェーズ |
| `ParticlePlayer.EventInfo` | Serializable class | イベント定義（Time / Birth / Alive / Death トリガー + message 文字列） |
| `ParticlePlayerInspector` | エディタ専用 | 非再生モードでの再生エミュレート（Play/Pause ボタン・イベントログ出力） |
| `ParticlePlayerSortingOrderInspector` | エディタ専用 | 親基準値 + 相対値の表示編集 |

## 使い方(実例)

Client側スクリプトからの直接利用は現状なし（プレハブ: `Resource (Internal)/Core/Effect/TouchEffect/VfxTouchEffect.prefab` 等に付与され、基盤 `TouchEffectManager` が制御）。以下は基盤内の実例。

### 実例1: キャッシュ再利用パターン（Deactivate + OnEnd 購読）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/TouchEffect/TouchEffectManager.cs（抜粋）
particleController = UnityUtility.Instantiate<ParticlePlayer>(touchEffectRoot, touchEffectPrefab);

// キャッシュ目的なのでDeactiveを指定.
particleController.EndActionType = EndActionType.Deactivate;

particleController.OnEndAsObservable()
    .Subscribe(endEffect =>
        {
            if (endEffect != null)
            {
                cachedTouchEffects.Enqueue(endEffect);
            }
        })
    .AddTo(this);
```

再生は `particleController.Play().Forget();`（同ファイル）。Play 時に自動で `SetActive(true)` されるため、Deactivate 済みインスタンスをそのまま再 `Play()` できる。
[ObjectPool](ObjectPool.md) と組み合わせる場合も同様に「取得 → `Play()` → `OnEnd` で `pool.Return()`」の形になる。

### 実例2: 終了まで待つ最小形

```csharp
// 最小の想定例（Client側に直接使用実績がないため）.
var effect = UnityUtility.Instantiate<ParticlePlayer>(parent, effectPrefab);

await effect.Play();
```

## API(主要公開メンバー)

### ParticlePlayer

| メンバー | 説明 |
|---|---|
| `Play(bool restart = true) : UniTask` | 再生して終了（全 ParticleSystem の IsPlayback 終了 or lifeTime 経過）まで待つ。`restart: false` なら再生中の Observable に相乗り |
| `Stop(bool immediate = false, bool clear = true)` | 停止。`immediate: false` は放出停止→自然消滅を待って終了処理 |
| `Pause : bool` | 一時停止/再開（State を Pause ⇔ 直前状態へ） |
| `State : State` / `CurrentTime : float` | 現在状態（Play/Pause/Stop）/ 再生経過秒 |
| `IsAlive() : bool` | 生存判定（LifecycleControl.Manual 時は `currentTime <= lifeTime`） |
| `EndActionType : EndActionType` | 終了時アクション（None/Destroy/Deactivate/Loop）。実行時変更可 |
| `SpeedRate : float` / `ResetSpeed()` | 再生速度倍率（simulationSpeed に乗算）/ 初期速度へ戻す |
| `SortingLayer : int` / `SortingOrder : int` | 配下全 Renderer へ一括適用（SortingOrder は `ParticlePlayerSortingOrder` の相対値と合算） |
| `OnEndAsObservable() : Observable<ParticlePlayer>` | 再生終了通知（EndAction 実行後） |
| `OnEventAsObservable() : Observable<string>` | EventInfo の message 通知 |
| `IsInitialized : bool` | 初期化済みか（初期化は OnEnable / Play 時に自動） |

### ParticlePlayerSortingOrder

| メンバー | 説明 |
|---|---|
| `Set(int sortingOrder)` | 相対値を設定し親 ParticlePlayer 基準で即適用 |
| `Apply(int baseSortingOrder)` | 基準値 + 相対値を Renderer へ適用（通常は ParticlePlayer が呼ぶ） |
| `FindParentParticlePlayer() : ParticlePlayer` | 祖先の ParticlePlayer 検索 |

## 注意点・罠

- **Unity ライフサイクル（OnEnable/OnDisable）で自動初期化・停止する既存実装**。OnEnable で `Initialize()` + `activateOnPlay` なら自動再生、OnDisable で `Stop(true, true)`。非アクティブ化＝強制停止になる点に注意
- 収集した全 ParticleSystem の `playOnAwake` は**強制的に false 化**される（制御を ParticlePlayer に一元化するため）
- 更新は Unity 標準再生ではなく **`Simulate()` による自前フレーム更新**（`ignoreTimeScale` 対応のため）。`Time.timeScale` を 0 にしても `ignoreTimeScale: true` なら動く
- 終了判定は `ParticleSystem.IsPlayback()` 拡張（`IsAlive()` バグ回避実装）。**ループ設定の ParticleSystem が1つでもあると自然終了しない**（常に生存扱い）→ ループエフェクトは `LifecycleControl.Manual` + `lifeTime` か明示 `Stop()` で止める
- 子の増減時は `OnTransformChildrenChanged` で自動再収集されるが、既存子の ParticleSystem 差し替え等は ContextMenu「CollectContents」or 再アクティブ化が必要
- `Stop(immediate: false)` は終了通知を待ってから状態リセットする（即時ではない）
- Client側 `AdditionalComponent.cs` の `using Modules.Particle` は現状未使用（自動アタッチ設定に ParticlePlayer は含まれていない）

## 関連

- [Extensions/Methods.md](../Extensions/Methods.md) — `IsPlayback()` / `GetSubemitters()`（ParticleSystem 拡張。単発の再生判定はこちらだけで足りる）
- [ObjectPool](ObjectPool.md) — エフェクト使い回しの汎用プール（`EndActionType.Deactivate` + `OnEndAsObservable` で返却する構成が定石）
- [UI](UI.md) — `UIParticleSystem`（uGUI 上でのパーティクル描画。namespace `Modules.UI.Particle` で本モジュールとは別物）

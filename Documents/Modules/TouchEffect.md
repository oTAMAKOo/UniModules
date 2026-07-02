# TouchEffect

> **namespace**: `Modules.TouchEffect`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TouchEffect/`（`TouchEffectManager.cs` の1ファイルのみ）
> **Client側使用**: **使用中**。派生1 + 生成/登録2ファイル（2026-07時点）
> **依存**: UniTask / R3 / Extensions（`SingletonMonoBehaviour<T>`, `UnityUtility`） / Modules.Particle（`ParticlePlayer`）

## 概要

画面タップ・ドラッグ時に指先へパーティクルエフェクトを出す常駐マネージャーの基底クラス。タッチ/マウス入力を自動判別し、`ParticlePlayer` をキャッシュ（オブジェクトプール）しながら再生する。プロジェクト側で派生クラスを作り、対象レイヤーとプレハブ（SerializeField）を設定して使う。

本プロジェクトでは `Dominion.Client.Module.TouchEffect.TouchEffectManager` として派生済みで、アプリ初期化時に常駐生成される。**タップエフェクトは既に全画面で有効**なので、個別画面で再発明しないこと。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| タップエフェクトを出したい | 何もしなくてよい（起動時に常駐済み。`Layer.Overlap` で全画面に出る） |
| タップエフェクトを一時的に消したい/出したい | `TouchEffectManager.Instance.Hide()` / `Show()` |
| エフェクト有効条件を変えたい | Client側派生 `TouchEffectManager.IsEnable`（`Client/Assets/Scripts/Client/Core/TouchEffect/TouchEffectManager.cs`）を修正 |
| エフェクトの見た目を変えたい | プレハブ `Client/Assets/Resource (Internal)/Core/Prefabs/Manager/TouchEffectManager.prefab` の `touchEffectPrefab` / `dragEffectPrefab` を差し替え |
| 別レイヤー・別カメラに出したい | `TouchEffectManager<T>` を新規派生し `TargetLayer` / `TargetLayerMask` を override |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `TouchEffectManager<TInstance>` | abstract class（`SingletonMonoBehaviour<TInstance>`） | 基底。入力監視（`Update`）・タッチ/ドラッグエフェクトのプール再生・表示切替。`TargetLayer` / `TargetLayerMask` が abstract |
| `TouchEffectManager`（Client側） | sealed class（上記派生） | 本プロジェクトの実装。`Layer.Overlap` 対象、SRDebugger のデバッグパネル表示中は `IsEnable = false` |

## 使い方(実例)

```csharp
// 実例: Client/Assets/Scripts/Client/Core/TouchEffect/TouchEffectManager.cs（派生定義）.
public sealed class TouchEffectManager : Modules.TouchEffect.TouchEffectManager<TouchEffectManager>
{
    public override bool IsEnable { get { return CheckEnable(); } }

    public override int TargetLayer { get { return (int)Layer.Overlap; } }

    public override int TargetLayerMask { get { return Layer.Overlap.ToLayerMask(); } }
}
```

```csharp
// 実例: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs（常駐生成）.
public void CreateTouchEffectManager()
{
    var touchEffectManager = UnityUtility.Instantiate<TouchEffectManager>(null, touchEffectManagerPrefab);

    touchEffectManager.Initialize();
}
```

シーン重複管理への登録実例: `Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs`（`{ typeof(TouchEffectManager), DuplicatedSettings.Default }`）。

## API(主要公開メンバー)

### TouchEffectManager&lt;TInstance&gt;

| メンバー | 説明 |
|---|---|
| `Initialize()` | virtual。カメラ検索（`TargetLayerMask` から `FindCameraForLayer`）とキャッシュルート生成。生成直後に必ず呼ぶ |
| `IsEnable : bool` | virtual。false の間は入力を無視（エフェクトを出さない） |
| `TargetLayer : int` / `TargetLayerMask : int` | **abstract**。エフェクトのレイヤーと投影先カメラ判定用マスク |
| `Show()` / `Hide()` | キャッシュルートごと表示/非表示（再生中エフェクトも消える） |
| `SetTouchEffectPosition(ParticlePlayer, Vector3 screenPosition)` | protected virtual。スクリーン座標→ワールド配置の変換（既定は `ScreenToWorldPoint` の XY のみ）。奥行きやUI座標系対応は override |

### SerializeField（プレハブ側設定）

| フィールド | 説明 |
|---|---|
| `rootObject` | キャッシュルートの親 |
| `touchEffectPrefab` | タップ（`TouchPhase.Began`）時のエフェクト。null なら機能自体が無効 |
| `dragEffectPrefab` | ドラッグ（`TouchPhase.Moved`）時のエフェクト。null なら無効 |
| `intervalDistance` | ドラッグエフェクトの発生間隔（スクリーン距離。既定 8px） |

## 注意点・罠

- `Initialize()` を呼ばないと `Update` が即 return して一切動かない（`isInitialized` ガード）。生成箇所は `InitializeObject.manager.cs` に集約済み。
- 入力は `Input.GetTouch` / `GetMouseButton` の旧 Input Manager 直読み。`fingerId == 0`（1本目の指）しかエフェクトを出さない。マルチタッチの2本目以降には出ない仕様。
- エフェクト再生は `ParticlePlayer.EndActionType = Deactivate` によるプール運用。プレハブは `ParticlePlayer` コンポーネント必須（→ [Particle](Particle.md)）。
- カメラは `Initialize` 時に `TargetLayerMask` を描画するカメラを検索して保持。見つからない場合は毎タッチで再検索し `Debug.LogWarning`（`Touch effect camera not found.`）。レイヤー構成やカメラ生成順を変えると出なくなる。
- 基底の `Update`（Unityライフサイクル）で動作する既存実装。プロジェクトの「ライフサイクルメソッド原則禁止」ルールは既存クラスには適用しない（修正指示があるまで現状維持）。
- タッチ判定プラットフォームは Android/iOS のみ（それ以外はマウス扱い）。初回判定を Nullable でキャッシュする。

## 関連

- [Particle](Particle.md) — エフェクト再生本体（`ParticlePlayer` / `EndActionType`）
- [InputControl](InputControl.md) — 入力のブロック制御（本モジュールは Input 直読みのためブロックの影響を受けない点に注意）

# TouchEffect

タップした位置にエフェクトを出す機能。
タップとドラッグのそれぞれにパーティクルを再生し、表示の切り替えができる。

## 基本情報

> **namespace**: `Modules.TouchEffect`
> **場所**: `Modules/TouchEffect/`（`TouchEffectManager.cs` の1ファイルのみ）
> **依存**: UniTask / R3 / Extensions（`SingletonMonoBehaviour<T>`, `UnityUtility`） / Modules.Particle（`ParticlePlayer`）

## 概要

画面タップ・ドラッグ時に指先へパーティクルエフェクトを出す常駐マネージャーの基底クラス。タッチ/マウス入力を自動判別し、`ParticlePlayer` をキャッシュ（オブジェクトプール）しながら再生する。利用側で派生クラスを作り、対象レイヤーとプレハブ（SerializeField: `rootObject` / `touchEffectPrefab` / `dragEffectPrefab` / `intervalDistance`）を設定して使う。

主要クラス: `TouchEffectManager<TInstance>`（abstract、`SingletonMonoBehaviour<TInstance>`。入力監視・プール再生・表示切替。`TargetLayer` / `TargetLayerMask` が abstract）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| タップエフェクトを一時的に消したい/出したい | 派生クラスの `Instance.Hide()` / `Show()`（エフェクトの親を非表示/表示にする） |
| エフェクトを有効・無効にしたい | `IsEnable` を設定（`public virtual bool IsEnable { get; set; }`。既定は false。条件で決めるなら派生クラスで override） |
| エフェクトの見た目を変えたい | プレハブの `touchEffectPrefab` / `dragEffectPrefab` を差し替え（未設定の側は再生されない） |
| ドラッグエフェクトの出る間隔を変えたい | `intervalDistance`（前フレームからの移動量がこの値を超えたフレームで再生。スクリーン座標の距離、既定 8） |
| 別レイヤー・別カメラに出したい | `TouchEffectManager<T>` を派生し `TargetLayer`（レイヤー番号）/ `TargetLayerMask`（カメラ検索用のマスク）を実装 |

## 使い方

- 派生クラスで `TargetLayer` / `TargetLayerMask` を定義し、プレハブに `touchEffectPrefab` / `dragEffectPrefab` を SerializeField 参照でセットする
- アプリ初期化時に `UnityUtility.Instantiate` → `Initialize()` で常駐生成し、`IsEnable = true` にする
- シーン跨ぎで重複が起きる構成なら、[Scene](../Scene/Manual.md) の `UniqueComponents`（`DuplicatedSettings`）に派生クラス型を登録して自動破棄させる

## 注意点・罠

- `Initialize()` を呼ばないと `Update` が即 return して一切動かない（`isInitialized` ガード）
- **`IsEnable` の既定は false**。`Initialize()` だけではエフェクトは出ない
- 入力は `Input.GetTouch` / `GetMouseButton` の旧 Input Manager 直読み。`fingerId == 0`（1本目の指）しかエフェクトを出さない。マルチタッチの2本目以降には出ない仕様
- エフェクト再生は `ParticlePlayer.EndActionType = Deactivate` によるプール運用。プレハブは `ParticlePlayer` コンポーネント必須（→ [Particle](../Particle/Manual.md)）
- カメラは `Initialize` 時に `TargetLayerMask` を描画するカメラを検索して保持。見つからない場合は毎タッチで再検索し `Debug.LogWarning`（`Touch effect camera not found.`）。レイヤー構成やカメラ生成順を変えると出なくなる
- スクリーン座標→ワールド配置の変換は protected virtual `SetTouchEffectPosition`（既定は `ScreenToWorldPoint` の XY のみ）。奥行きやUI座標系対応は override する
- 基底の `Update`（Unityライフサイクル）で動作する既存実装
- タッチ判定プラットフォームは Android/iOS のみ（`Application.platform` で判定。エディタを含むそれ以外はマウス左ボタン扱い）。初回判定を Nullable でキャッシュする
- `Hide()` は親を非表示にするだけで、入力監視と再生処理は止まらない。処理ごと止めるなら `IsEnable = false`

## 関連

- [Particle](../Particle/Manual.md) — エフェクト再生本体（`ParticlePlayer` / `EndActionType`）
- [InputControl](../InputControl/Manual.md) — 入力のブロック制御（本モジュールは Input 直読みのためブロックの影響を受けない点に注意）

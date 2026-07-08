# Resolution

> **namespace**: `Modules.Resolution`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Resolution/`
> **Client側使用**: 2ファイル（2026-07時点: `SceneManager.cs` / `InitializeObject.core.cs`）
> **依存**: R3 / Extensions（`UnityUtility`, `SingletonMonoBehaviour`, RectTransform拡張） / Unity.Linq / Modules.UI.Extension（`UICanvas`）

## 概要

画面解像度・アスペクト比・セーフエリア（ノッチ等）への UI 適応を行うコンポーネント群。
基準解像度（`CanvasScaler.referenceResolution`）外の領域をレターボックスで隠す `LetterBox` と、RectTransform のアンカーをセーフエリア等に自動追従させる Adjuster 3種で構成。
いずれも **シーン/プレハブにアタッチして使う MonoBehaviour** で、コードから直接操作する場面はほぼない。
主要クラス: `LetterBox`（`SingletonMonoBehaviour`。上下 or 左右のレターボックス帯 + ステータスバー帯を画面比に応じて表示。`CanvasScaler.ScaleWithScreenSize` 時のみ有効化）/ `SafeAreaAdjuster`（`Screen.safeArea` を正規化して自身のアンカーに反映）/ `FixedResolutionSafeAreaAdjuster`（基準解像度とセーフエリアの**狭い方**に合わせてアンカー調整。固定解像度＝レターボックス併用デザイン向け）/ `ReferenceResolutionAdjuster`（自身を中央アンカー・基準解像度サイズに固定。public API なし）。全クラス `[ExecuteAlways]`。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 基準解像度外を黒帯（レターボックス）で隠したい | `LetterBox`（プレハブを生成。Client では `InitializeObject` が生成済み） |
| RectTransform をセーフエリアぴったりに合わせたい | `SafeAreaAdjuster` をアタッチ |
| 固定解像度デザインでセーフエリアとレターボックス両方を考慮したい | `FixedResolutionSafeAreaAdjuster` をアタッチ |
| RectTransform を基準解像度サイズ（中央固定）にしたい | `ReferenceResolutionAdjuster` をアタッチ |
| レターボックス領域を即時再計算したい | `LetterBox.Instance.Apply()` |

## 使い方

- LetterBox の生成（アプリ初期化時に1度だけ。プレハブから `UnityUtility.Instantiate<LetterBox>`）: 実例は `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs` の `CreateLetterBox()`
- シーン跨ぎの重複管理: `Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs` で `{ typeof(LetterBox), DuplicatedSettings.Default }` を登録（シーン遷移時に重複した LetterBox を自動破棄）
- `SafeAreaAdjuster` / `FixedResolutionSafeAreaAdjuster` / `ReferenceResolutionAdjuster` は対象の RectTransform にアタッチするだけで動作する（Client 側コードからの参照は無し。プレハブ運用）

## 注意点・罠

- 全クラス `[ExecuteAlways]`。エディタの編集モードでも動作し、Adjuster 2種は `DrivenRectTransformTracker` により対象 RectTransform のアンカー等が **Inspector から編集不可（Driven 表示）** になる
- `LetterBox` はプレハブ前提（帯用 RectTransform を SerializeField で差す）。生成は `InitializeObject.core.cs` が担っており、**新規シーンで独自生成する必要はない**
- `LetterBox` は `CanvasScaler.uiScaleMode == ScaleWithScreenSize` でない場合、自身の GameObject を非アクティブ化して何もしない
- `LetterBox.StatusBarHeight` は 0 より大きい時のみステータスバー帯を表示（Client 側では未設定 = 0）
- `SafeAreaAdjuster` と `FixedResolutionSafeAreaAdjuster` の使い分け: 前者は画面全体（`Screen.width/height`）基準、後者は基準解像度 + Canvas スケール基準でレターボックス領域を考慮する
- 毎フレーム Apply が走るが差分チェックでスキップされる（`Apply(force: true)` で強制再適用可）。Undo でアンカーが 0 になった場合の再適用ケアも入っている（ソースコメントより）
- このモジュールは新規実装での再発明が起きやすい（「セーフエリア対応」を自作しがち）。**セーフエリア対応はここにあるものを使う**

## 関連

- [UI](UI.md) — `UICanvas`（LetterBox が Canvas 取得・CanvasScaler 調整に使用）
- [Scene](Scene.md) — `DuplicatedSettings` による常駐オブジェクト重複管理
- [DeviceOrientation](DeviceOrientation.md) — 画面向き関連

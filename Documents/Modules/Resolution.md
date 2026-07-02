# Resolution

> **namespace**: `Modules.Resolution`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Resolution/`
> **Client側使用**: 2ファイル（2026-07時点: `SceneManager.cs` / `InitializeObject.core.cs`）
> **依存**: R3 / Extensions（`UnityUtility`, `SingletonMonoBehaviour`, RectTransform拡張） / Unity.Linq / Modules.UI.Extension（`UICanvas`）

## 概要

画面解像度・アスペクト比・セーフエリア（ノッチ等）への UI 適応を行うコンポーネント群。
基準解像度（`CanvasScaler.referenceResolution`）外の領域をレターボックスで隠す `LetterBox` と、RectTransform のアンカーをセーフエリア等に自動追従させる Adjuster 3種で構成。
いずれも **シーン/プレハブにアタッチして使う MonoBehaviour** で、コードから直接操作する場面はほぼない。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 基準解像度外を黒帯（レターボックス）で隠したい | `LetterBox`（プレハブを生成。Client では `InitializeObject` が生成済み） |
| RectTransform をセーフエリアぴったりに合わせたい | `SafeAreaAdjuster` をアタッチ |
| 固定解像度デザインでセーフエリアとレターボックス両方を考慮したい | `FixedResolutionSafeAreaAdjuster` をアタッチ |
| RectTransform を基準解像度サイズ（中央固定）にしたい | `ReferenceResolutionAdjuster` をアタッチ |
| レターボックス領域を即時再計算したい | `LetterBox.Instance.Apply()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `LetterBox` | `SingletonMonoBehaviour<LetterBox>` / `[ExecuteAlways]` | 上下 or 左右のレターボックス帯 + ステータスバー帯を画面比に応じて表示。`CanvasScaler.ScaleWithScreenSize` 時のみ有効化 |
| `SafeAreaAdjuster` | MonoBehaviour / `[ExecuteAlways]` `[RequireComponent(RectTransform)]` | `Screen.safeArea` を正規化して自身のアンカー（anchorMin/Max）に反映。毎フレーム差分チェック |
| `FixedResolutionSafeAreaAdjuster` | MonoBehaviour / `[ExecuteAlways]` `[RequireComponent(RectTransform)]` | 基準解像度とセーフエリアの**狭い方**に合わせてアンカー調整。固定解像度（レターボックス併用）デザイン向け |
| `ReferenceResolutionAdjuster` | MonoBehaviour / `[ExecuteAlways]` `[RequireComponent(RectTransform)]` | 自身を中央アンカー・基準解像度サイズに固定。public API なし（OnEnable で自動適用） |

## 使い方(実例)

### LetterBox の生成（アプリ初期化時に1度だけ）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
private void CreateLetterBox()
{
    if (LetterBox.Instance == null)
    {
        UnityUtility.Instantiate<LetterBox>(null, letterBoxPrefab);
    }

    if (LetterBox.Instance != null)
    {
        LetterBox.Instance.transform.name = "LetterBox";
    }
}
```

### シーン跨ぎの重複管理（常駐オブジェクト登録）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs
// シーン遷移時に重複した LetterBox を自動破棄する登録.
{ typeof(LetterBox), DuplicatedSettings.Default },
```

### Adjuster 系（コードではなくアタッチして使う）

`SafeAreaAdjuster` / `FixedResolutionSafeAreaAdjuster` / `ReferenceResolutionAdjuster` は対象の RectTransform にアタッチするだけで動作する（Client 側コードからの参照は無し。プレハブ運用）。

## API(主要公開メンバー)

### LetterBox（Singleton: `LetterBox.Instance`）

| メンバー | 説明 |
|---|---|
| `float StatusBarHeight { get; set; }` | ステータスバー帯の高さ。0 より大きい時のみ statusBar 帯を表示（Client 側では未設定 = 0） |
| `void Apply()` | レターボックス帯のサイズ・表示を再計算。`matchWidthOrHeight == 1`（縦フィット）なら左右帯、それ以外は上下帯を使用 |

- `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` でシーンロード後に自動 `Apply()` される
- SerializeField（`uiCanvas` / `statusBar` / `top` / `bottom` / `left` / `right`）はプレハブで設定する前提

### SafeAreaAdjuster / FixedResolutionSafeAreaAdjuster

| メンバー | 説明 |
|---|---|
| `void Apply(bool force = false)` | アンカー再計算。通常は Update/LateUpdate から自動実行され、safeArea・解像度に差分がなければスキップ。`force: true` で強制再適用 |

### ReferenceResolutionAdjuster

public メンバーなし。OnEnable 時に自動適用（反映漏れ対策として最初の Update でも R3 の `Observable.EveryUpdate().Take(1)` で再適用）。

## 注意点・罠

- 全クラス `[ExecuteAlways]`。エディタの編集モードでも動作し、Adjuster 2種は `DrivenRectTransformTracker` により対象 RectTransform のアンカー等が **Inspector から編集不可（Driven 表示）** になる
- `LetterBox` はプレハブ前提（帯用 RectTransform を SerializeField で差す）。生成は `InitializeObject.core.cs` が担っており、**新規シーンで独自生成する必要はない**
- `LetterBox` は `CanvasScaler.uiScaleMode == ScaleWithScreenSize` でない場合、自身の GameObject を非アクティブ化して何もしない
- `SafeAreaAdjuster` と `FixedResolutionSafeAreaAdjuster` の使い分け: 前者は画面全体（`Screen.width/height`）基準、後者は基準解像度 + Canvas スケール基準でレターボックス領域を考慮する
- 毎フレーム Apply が走るが差分チェックでスキップされる。Undo でアンカーが 0 になった場合の再適用ケアも入っている（ソースコメントより）
- このモジュールは新規実装での再発明が起きやすい（「セーフエリア対応」を自作しがち）。**セーフエリア対応はここにあるものを使う**

## 関連

- [UI](UI.md) — `UICanvas`（LetterBox が Canvas 取得・CanvasScaler 調整に使用）
- [Scene](Scene.md) — `DuplicatedSettings` による常駐オブジェクト重複管理
- [DeviceOrientation](DeviceOrientation.md) — 画面向き関連

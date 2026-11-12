# Movie

CRI Sofdec のムービーを再生する機能。
ムービーの再生準備・再生・一時停止・停止と終了の検知ができ、uGUI や RenderTexture へ描画できる。
CRI SDK と ENABLE_CRIWARE_SOFDEC シンボルが必要。

## 基本情報

> **namespace**: `Modules.Movie`（Editor専用: `Modules.Movie.Editor`）
> **場所**: `Modules/Movie/`
> **依存**: CRI SDK（`CriWare` / `CriWare.CriMana` namespace） / UniTask / R3 / Extensions / Modules.CriWare / Modules.Devkit.Generators・Modules.Devkit.Project（Editor）

## 概要

CRI Sofdec ムービー（.usm）の再生管理基盤。`CriMana.Player` のラップ（準備→再生→終了検知→自動解放）と、uGUI / RenderTexture への描画コンポーネントを提供する。

主要クラス: `MovieManagementBase<TInstance, TMovie>`（abstract Singleton。再生管理の基底。利用側が継承し、`GetManaInfo(TMovie mana)` を実装して enum 等から `ManaInfo` を解決する）/ `MovieElement`（再生1件分のハンドル。`IsReady` / `IsFinished` / `PlayTime` / `TotalTime` / `OnFinishAsObservable()` 等）/ `ManaInfo`（.usm のパス情報。`Usm` / `UsmPath`）/ `CriMovieForUI`（uGUI向け描画）/ `CriMovieForRenderTexture`（RenderTexture向け描画）/ `MovieScriptGenerator`（Editor専用。.usm一覧から `Movies.Mana` enum を自動生成）。

全ファイルが `#if ENABLE_CRIWARE_SOFDEC` で丸ごと囲われており、利用側で同シンボルが未定義の場合はコンパイル対象外になる（→ [CriWare](../CriWare/Manual.md)）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ムービーを再生したい | CRI SDK 導入 + `ENABLE_CRIWARE_SOFDEC` 定義（利用側で有効化されていない場合は使えない） |
| （CRI有効時）ムービー再生 | `MovieManagementBase` 派生の `Instance.Initialize()` → `Play(mana または ManaInfo または moviePath, movieController, loop = false, shaderOverrideCallBack = null)`（`movieController` は `CriManaMovieMaterialBase`。戻り値は `MovieElement`） |
| （CRI有効時）再生準備だけ先行 | `Prepare(mana または ManaInfo または moviePath, movieController)` → `MovieElement.IsReady` を待って `element.Play(loop)` |
| （CRI有効時）一時停止 / 再開 / 停止 | `element.Pause(true)` / `element.Pause(false)` / `element.Stop()` |
| （CRI有効時）終了検知 | `MovieElement.OnFinishAsObservable()` / `IsFinished` |
| （CRI有効時）再生時間・状態の取得 | `MovieElement.PlayTime` / `TotalTime`（取得できない間は -1）/ `ElementStatus` / `PlayerStatus` / `HasError()` |
| （CRI有効時）ムービー音量の変更 | `SetAudioVolume(volume)`（再生中の全ムービーと、以降に再生するムービーへ適用） |
| （CRI有効時）全ムービーの Player を破棄 | `ReleaseAll()` |
| （CRI有効時）uGUIに描画 | `CriMovieForUI`（`CriManaMovieControllerForUI` 派生。Awake を待たずに初期化するなら `ManualInitialize()`） |
| （CRI有効時）RenderTextureに描画 | `CriMovieForRenderTexture.Target` に RenderTexture を設定 |
| 【Editor・CRI有効時】.usm一覧から `Movies.Mana` enum を生成 | `MovieScriptGenerator.Generate(scriptPath, scriptNamespace, rootFolderPath, rootFolderName)`（`CriAssetUpdater` から呼ばれ、`Movies.cs`（`Movies.Mana` と `Movies.GetManaInfo(Mana)`）を出力する） |

## 注意点・罠

- **シンボル定義だけでは有効化できない**。(1) CRI SDK（Sofdec プラグイン）が必要、(2) 具象 `MovieManagement` クラスの実装が必要、(3) `Movies` クラス（enum定義）を `MovieScriptGenerator` で生成する運用（`CriAssetUpdater.ExecuteAll()` 経由）の整備が必要。
- （CRI有効時）`MovieManagementBase.Initialize()` を呼ばないと再生終了の検知・自動解放が動かない。
- （CRI有効時）非ループ再生は `PlayEnd` 到達で**自動的に Player 解放**される（`MovieElement.ReleasePlayer`）。再利用する場合は都度 `Prepare` / `Play` し直す。`Stop()`・エラー・Player の破棄でも同じく終了扱いになり、終了時は描画コンポーネント（`movieController`）が非アクティブ化される。
- （CRI有効時）ループ再生（`loop = true`）は自動では終了しない。`element.Stop()` で止める。
- `Editor/MovieScriptGenerator` はエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [CriWare](../CriWare/Manual.md) — CRI ライブラリ初期化・アセット配信基盤
- [ExternalAssets](../ExternalAssets/Manual.md) — CRI有効時は `ExternalAsset.GetMovieInfo(resourcePath)` が `ManaInfo` を返す（`ExternalAsset.cri.cs`）
- [Sound](../Sound/Manual.md) — CRI(ADX) 版のサウンド基盤

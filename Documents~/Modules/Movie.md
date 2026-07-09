# Movie

> **namespace**: `Modules.Movie`（Editor専用: `Modules.Movie.Editor`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Movie/`
> **依存**: CRI SDK（`CriWare` / `CriWare.CriMana` namespace） / UniTask / R3 / Extensions / Modules.CriWare / Modules.Devkit.Generators（Editor）

## 概要

CRI Sofdec ムービー（.usm）の再生管理基盤。`CriMana.Player` のラップ（準備→再生→終了検知→自動解放）と、uGUI / RenderTexture への描画コンポーネントを提供する。

主要クラス: `MovieManagementBase<TInstance, TMovie>`（abstract Singleton。再生管理の基底。利用側が具象クラスを実装する前提）/ `MovieElement`（再生1件分のハンドル。`IsReady` / `OnFinishAsObservable()` 等）/ `CriMovieForUI`（uGUI向け描画）/ `CriMovieForRenderTexture`（RenderTexture向け描画）/ `MovieScriptGenerator`（Editor専用。.usm一覧から `Movies.Mana` enum を自動生成）。

全ファイルが `#if ENABLE_CRIWARE_SOFDEC` で丸ごと囲われており、利用側で同シンボルが未定義の場合はコンパイル対象外になる（→ [CriWare](CriWare.md)）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ムービーを再生したい | CRI SDK 導入 + `ENABLE_CRIWARE_SOFDEC` 定義（利用側で有効化されていない場合は使えない） |
| （CRI有効時）ムービー再生 | `MovieManagementBase` 派生の `Instance.Initialize()` → `Play(moviePath, movieController)` |
| （CRI有効時）再生準備だけ先行 | `Prepare(...)` → `MovieElement.IsReady` を待って `element.Play()` |
| （CRI有効時）終了検知 | `MovieElement.OnFinishAsObservable()` |
| （CRI有効時）uGUIに描画 | `CriMovieForUI`（`CriManaMovieControllerForUI` 派生） |
| （CRI有効時）RenderTextureに描画 | `CriMovieForRenderTexture.Target` に RenderTexture を設定 |
| 【Editor・CRI有効時】.usm一覧から `Movies.Mana` enum を生成 | `MovieScriptGenerator.Generate()`（`CriAssetUpdater` から呼ばれる） |

## 注意点・罠

- **シンボル定義だけでは有効化できない**。(1) CRI SDK（Sofdec プラグイン）が必要、(2) 具象 `MovieManagement` クラスの実装が必要、(3) `Movies` クラス（enum定義）を `MovieScriptGenerator` で生成する運用（`CriAssetUpdater.ExecuteAll()` 経由）の整備が必要。
- （CRI有効時）`MovieManagementBase.Initialize()` を呼ばないと再生終了の検知・自動解放が動かない。
- （CRI有効時）非ループ再生は `PlayEnd` 到達で**自動的に Player 解放**される（`MovieElement.ReleasePlayer`）。再利用する場合は都度 `Prepare` / `Play` し直す。
- `Editor/MovieScriptGenerator` はエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [CriWare](CriWare.md) — CRI ライブラリ初期化・アセット配信基盤
- [ExternalAsset](ExternalAsset.md) — CRI有効時は `ExternalAsset.GetMovieInfo(resourcePath)` が `ManaInfo` を返す（`ExternalAsset.cri.cs`）
- [Sound](Sound.md) — CRI(ADX) 版のサウンド基盤

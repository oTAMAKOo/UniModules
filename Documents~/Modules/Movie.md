# Movie

> **namespace**: `Modules.Movie`（Editor専用: `Modules.Movie.Editor`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Movie/`
> **Client側使用**: 0ファイル（2026-07時点。`InitializeObject.manager.cs` に `#if ENABLE_CRIWARE_SOFDEC` 内の死に参照が残存するのみ）
> **依存**: CRI SDK（`CriWare` / `CriWare.CriMana` namespace、**未導入**） / UniTask / R3 / Extensions / Modules.CriWare / Modules.Devkit.Generators（Editor）

## 概要

CRI Sofdec ムービー（.usm）の再生管理基盤。`CriMana.Player` のラップ（準備→再生→終了検知→自動解放）と、uGUI / RenderTexture への描画コンポーネントを提供する。

**本プロジェクトでは未使用（コンパイル対象外）**。全ファイルが `#if ENABLE_CRIWARE_SOFDEC` で丸ごと囲われており、同シンボルはどこにも定義されていない（`Client/ProjectSettings/ProjectSettings.asset` の `scriptingDefineSymbols` / `Client/Assets/csc.rsp` とも無し）。CRI SDK 自体も未導入 → [CriWare](CriWare.md)。**本プロジェクトにムービー再生基盤は現状存在しない**（必要なら Unity 標準 `VideoPlayer` 等の導入から検討）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **ムービーを再生したい（本プロジェクト）** | このモジュールでは不可（コンパイル対象外）。代替基盤も現状無し |
| （CRI有効時）ムービー再生 | `MovieManagementBase` 派生の `Instance.Initialize()` → `Play(moviePath, movieController)` |
| （CRI有効時）再生準備だけ先行 | `Prepare(...)` → `MovieElement.IsReady` を待って `element.Play()` |
| （CRI有効時）終了検知 | `MovieElement.OnFinishAsObservable()` |
| （CRI有効時）uGUIに描画 | `CriMovieForUI`（`CriManaMovieControllerForUI` 派生） |
| （CRI有効時）RenderTextureに描画 | `CriMovieForRenderTexture.Target` に RenderTexture を設定 |
| 【Editor・CRI有効時】.usm一覧から `Movies.Mana` enum を生成 | `MovieScriptGenerator.Generate()`（`CriAssetUpdater` から呼ばれる） |

## 主要クラス

全クラスが `#if ENABLE_CRIWARE_SOFDEC` 内のため、本プロジェクトでは**一切コンパイルされない**（CriWare の `CriWareObject` のような「型だけ常在」も無い）。

| クラス | 種別 | 役割 |
|---|---|---|
| `IMovieManagement` | interface | `Play(element, loop)` / `Pause(element, pause)` / `Stop(element)` |
| `MovieManagementBase<TInstance, TMovie>` | abstract Singleton | 再生管理の基底。`Initialize()` で `PostLateUpdate` 毎フレーム更新を開始し、再生終了・破棄済み要素を自動解放。`GetManaInfo(TMovie)` が abstract で、利用側が具象クラス（例: `MovieManagement`）を実装する前提 |
| `MovieElement` | class（LifetimeDisposable） | 再生1件分のハンドル。状態（`Status.Play/Pause/Stop`）、`PlayTime` / `TotalTime`（取得不可時 -1）、`IsReady` / `IsFinished` / `HasError()`、`OnFinishAsObservable()` |
| `ManaInfo` | class | .usm ファイルパス情報（`Usm` / `UsmPath`＝拡張子抜き） |
| `CriMovieForUI` | MonoBehaviour（`CriManaMovieControllerForUI` 派生） | uGUI 向け再生。`uiRenderMode = true` 固定、`ManualInitialize()` で Awake を待たず初期化可 |
| `CriMovieForRenderTexture` | MonoBehaviour（`CriManaMovieMaterial` 派生） | 毎フレーム `Graphics.Blit` で `Target`（RenderTexture）へ描画 |
| `MovieScriptGenerator` | static class（**Editor専用**） | 指定フォルダの .usm を走査し `Movies.Mana` enum + `GetManaInfo` を持つ `Movies.cs` を自動生成 |

## 使い方(実例)

Client側の実使用コードは無い。残存しているのは以下の死にコードのみ（`using Modules.Movie` すら無く、具象 `MovieManagement` クラスも未実装のため、シンボルを定義するとむしろコンパイルエラーになる）。

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
// ※ #if ENABLE_CRIWARE_SOFDEC 内のため本プロジェクトでは無効.
#if ENABLE_CRIWARE_SOFDEC

if (MovieManagement.Exists)
{
    MovieManagement.Instance.ReleaseAll();
}

#endif
```

## API(主要公開メンバー)

CRI 有効時のみ存在。`MovieManagementBase<TInstance, TMovie>` の主要メンバー:

| メンバー | 説明 |
|---|---|
| `Initialize()` | 毎フレーム更新の購読開始（二重呼び出しガード有り）。使用前に必須 |
| `Prepare(TMovie / ManaInfo / string, CriManaMovieMaterialBase, shaderOverrideCallBack = null) : MovieElement` | 再生準備（非アクティブ中も `PlayerManualUpdate` で準備を進行） |
| `Play(TMovie / ManaInfo / string, CriManaMovieMaterialBase, bool loop = false, ...) : MovieElement` / `Play(MovieElement, bool loop)` | 再生開始 |
| `Pause(MovieElement, bool)` / `Stop(MovieElement)` | 一時停止 / 停止 |
| `SetAudioVolume(float)` | 全再生中要素へ音量反映 |
| `ReleaseAll()` | 全 Player を Dispose して管理リストをクリア |
| `GetManaInfo(TMovie) : ManaInfo`（protected abstract） | enum値→ManaInfo 解決。派生側で実装 |

## 注意点・罠

- **本プロジェクトでは未使用（コンパイル対象外）**。ムービー前提の実装（.usm 再生等）を書かないこと。
- **シンボルを定義するだけでは有効化できない**。(1) CRI SDK（Sofdec プラグイン）が Assets に無い、(2) 具象 `MovieManagement` クラスが未実装（`InitializeObject.manager.cs` の残存参照が未定義シンボルとなりコンパイルエラー）、(3) `Movies` クラス（enum定義）を `MovieScriptGenerator` で生成する運用（`CriAssetUpdater.ExecuteAll()` 経由）の整備が必要。
- （CRI有効時）`MovieManagementBase.Initialize()` を呼ばないと再生終了の検知・自動解放が動かない。
- （CRI有効時）非ループ再生は `PlayEnd` 到達で**自動的に Player 解放**される（`MovieElement.ReleasePlayer`）。再利用する場合は都度 `Prepare` / `Play` し直す。
- `Editor/MovieScriptGenerator` はエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [CriWare](CriWare.md) — CRI ライブラリ初期化・アセット配信基盤（同様にコンパイル対象外。有効化手順の罠も参照）
- [ExternalAsset](ExternalAsset.md) — CRI有効時は `ExternalAsset.GetMovieInfo(resourcePath)` が `ManaInfo` を返す（`ExternalAsset.cri.cs`、現状無効）
- [Sound](Sound.md) — 同じく CRI(ADX) 版が無効化され UnityAudio 版で運用中のモジュール

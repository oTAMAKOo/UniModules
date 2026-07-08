# CriWare

> **namespace**: `Modules.CriWare`（Editor専用: `Modules.CriWare.Editor`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/CriWare/`
> **Client側使用**: 1ファイル（`SceneManager.cs` の型参照のみ、2026-07時点）
> **依存**: CRI SDK（`CriWare` namespace、**未導入**） / UniTask / R3 / Extensions / Modules.ExternalAssets / Modules.Net / Modules.Devkit.*

## 概要

CRIWARE（ADX=サウンド / Sofdec=ムービー / FileSystem=ダウンロード）のライブラリ初期化と、CRIアセット（.acf/.acb/.awb/.usm）の配信ダウンロード・エディタ取込を担う基盤。

主要クラス: `CriWareObject`（SingletonMonoBehaviour。CRIライブラリ初期化の管理。**コンパイルされるがメンバーは縮退**）/ `CriWareCustomErrorHandler`（エラーログ中継。同じく縮退コンパイル）/ `CriAssetManager`（CRIアセットの配信DL管理。コンパイルされない）/ `CriAssetDefinition`（拡張子定数）/ `CriWareConfig`（暗号化済みCRI認証キー）。Editor側（`Modules.CriWare.Editor`、全てコンパイル対象外）に `CriAssetUpdater` / `CriAssetUpdateWindow`（CRI成果物のUnity取込）/ `CriAssetGenerator`（ビルド時配信出力）等。

**本プロジェクトでは未使用（コンパイル対象外）**。`ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` / `ENABLE_CRIWARE_FILESYSTEM` はどのプラットフォームの Scripting Define Symbols にも定義されておらず（`Client/ProjectSettings/ProjectSettings.asset` の `scriptingDefineSymbols` 参照）、モジュール内のほぼ全コードが `#if ENABLE_CRIWARE_*` で無効化されている。サウンドは UnityAudio 版 `SoundManagement` + `AudioAssetManager`（`ExternalAsset.LoadAsset<Object>` / Resources）方式で運用中 → [Sound](Sound.md)。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **音を鳴らしたい（本プロジェクト）** | このモジュールではなく `SoundPlayer` → [Sound](Sound.md) |
| ムービーを再生したい（本プロジェクト） | このモジュールではない（CRI Sofdec 無効。Movie モジュールも同様に無効） |
| CRIを有効化したい | Scripting Define Symbols への `ENABLE_CRIWARE_*` 追加 + CRI SDK 導入 + Client側残存コードの改修（罠参照） |
| （CRI有効時）ライブラリ初期化 | `CriWareObject.Instance.Initialize(cryptoKey)` |
| （CRI有効時）CRIアセットのDL | `CriAssetManager`（`ExternalAsset` が内部で自動利用。直接触らない） |
| （CRI有効時）CRI拡張子の定数 | `CriAssetDefinition.AcbExtension` / `AwbExtension` / `AcfExtension` / `UsmExtension` / `AssetAllExtensions` |
| （CRI有効時）認証キーの復号取得 | `CriWareConfig.LoadInstance(resourcesPath)` → `await GetCriWareKey()` |
| 【Editor・CRI有効時】CRI成果物をUnityへ取込 | `CriAssetUpdater.ExecuteAll()` / `CriAssetUpdateWindow.Open()` |
| 【Editor・CRI有効時】エディタ再生をミュート | `EditorCriWareMute.Prefs.editorAudioMute` |

## 使い方

- Client側の唯一の使用箇所: `Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs` — シーン重複配置チェックの対象型として `typeof(CriWareObject)` を登録しているのみ。
- 基盤内の使用例（CRI有効時のみコンパイル）: `Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cri.cs` の `InitializeCri()` 参照。

## 注意点・罠

- **本プロジェクトでは未使用（コンパイル対象外）**。CRI前提の実装（Cue再生、.usmムービー等）を書かないこと。音は `SoundPlayer`、配信アセットは `ExternalAsset.LoadAsset<Object>` を使う。
- `CriWareObject` / `CriWareCustomErrorHandler` / `CriWareConsoleEvent` は**クラス定義自体はコンパイルされる**（`#if` がメンバー内側のため）。`SceneManager` の重複チェック登録が生きているのはこのため。`CriWareObject.Initialize` は呼んでも何もしない空メソッドになる。
- **シンボルを定義するだけでは有効化できない**。(1) CRI SDK（`CriWare` namespace のプラグイン）が Assets に無い、(2) Client側の `#if ENABLE_CRIWARE_ADX` 残存コード（`SceneManager.PlayBgm` の `SoundPlayer.Bgm(cueSheet, cue)` / `Sounds.GetCueInfo` 等）が現行 `SoundPlayer` / `Sounds` に存在しないメンバーを参照しておりコンパイルエラーになる、(3) `CriWareObject.Initialize(cryptoKey)` の起動フローへの組込みと `CriWareConfig` 派生の実装が必要。
- 同シンボルに依存する他モジュールも連動して無効: `Modules.Sound` の CriWare 版・`ExternalAsset.GetCueInfo` / `GetMovieInfo`（`ExternalAsset.cri.cs`）・`Modules.Movie`（Sofdec）。
- （CRI有効時）初期化順の制約: `CriAtomServer.CreateInstance()` を `CriAtom` 生成より先に呼ばないと `CriAtom` が `CriAtomServer` を勝手に生成してしまう（`CriWareObject.Initialize` 内コメント）。`CriFsServer` / `CriAtom` はライブラリ初期化完了後に生成する。
- `EditorCriWareMute` / `Editor/` 配下は全てエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [Sound](Sound.md) — サウンド再生基盤（本プロジェクトの実運用は UnityAudio 版 + `SoundPlayer`）
- [ExternalAsset](ExternalAsset.md) — 配信アセット基盤（CRI有効時は `CriAssetManager` を内包。`GetCueInfo` / `GetMovieInfo` は無効）
- [Movie](Movie.md) — Sofdec ムービー再生（同様にコンパイル対象外）

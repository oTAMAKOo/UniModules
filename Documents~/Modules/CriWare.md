# CriWare

> **namespace**: `Modules.CriWare`（Editor専用: `Modules.CriWare.Editor`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/CriWare/`
> **依存**: CRI SDK（`CriWare` namespace） / UniTask / R3 / Extensions / Modules.ExternalAssets / Modules.Net / Modules.Devkit.*

## 概要

CRIWARE（ADX=サウンド / Sofdec=ムービー / FileSystem=ダウンロード）のライブラリ初期化と、CRIアセット（.acf/.acb/.awb/.usm）の配信ダウンロード・エディタ取込を担う基盤。

主要クラス: `CriWareObject`（SingletonMonoBehaviour。CRIライブラリ初期化の管理。シンボル未定義時は**クラス定義のみコンパイルされメンバーは縮退**）/ `CriWareCustomErrorHandler`（エラーログ中継。同じく縮退コンパイル）/ `CriAssetManager`（CRIアセットの配信DL管理。シンボル定義時のみコンパイル）/ `CriAssetDefinition`（拡張子定数）/ `CriWareConfig`（暗号化済みCRI認証キー）。Editor側（`Modules.CriWare.Editor`、シンボル定義時のみコンパイル）に `CriAssetUpdater` / `CriAssetUpdateWindow`（CRI成果物のUnity取込）/ `CriAssetGenerator`（ビルド時配信出力）等。

`ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` / `ENABLE_CRIWARE_FILESYSTEM` を利用側で定義し CRI SDK を導入すると有効化される。未定義時はモジュール内のほぼ全コードが `#if ENABLE_CRIWARE_*` で無効化される。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| CRIを有効化したい | 利用側で `ENABLE_CRIWARE_*` を定義 + CRI SDK 導入 + `CriWareConfig` 派生の実装 + `CriWareObject.Initialize` の呼出組込（罠参照） |
| （CRI有効時）ライブラリ初期化 | `CriWareObject.Instance.Initialize(cryptoKey)` |
| （CRI有効時）CRIアセットのDL | `CriAssetManager`（`ExternalAsset` が内部で自動利用。直接触らない） |
| （CRI有効時）CRI拡張子の定数 | `CriAssetDefinition.AcbExtension` / `AwbExtension` / `AcfExtension` / `UsmExtension` / `AssetAllExtensions` |
| （CRI有効時）認証キーの復号取得 | `CriWareConfig.LoadInstance(resourcesPath)` → `await GetCriWareKey()` |
| 【Editor・CRI有効時】CRI成果物をUnityへ取込 | `CriAssetUpdater.ExecuteAll()` / `CriAssetUpdateWindow.Open()` |
| 【Editor・CRI有効時】エディタ再生をミュート | `EditorCriWareMute.Prefs.editorAudioMute` |

## 使い方

- 基盤内の使用例（CRI有効時のみコンパイル）: `Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cri.cs` の `InitializeCri()` 参照

## 注意点・罠

- `CriWareObject` / `CriWareCustomErrorHandler` / `CriWareConsoleEvent` は**クラス定義自体は常時コンパイルされる**（`#if` がメンバー内側のため）。シンボル未定義時は `CriWareObject.Initialize` が空メソッドになる。シーンの重複配置チェック等で `typeof(CriWareObject)` を参照するコードはこの縮退状態でも動く
- **シンボル定義だけでは有効化できない**。(1) CRI SDK（`CriWare` namespace のプラグイン）の導入、(2) `CriWareObject.Initialize(cryptoKey)` を呼び出す起動フローの組込、(3) `CriWareConfig` 派生の実装、(4) CRI Cue/Movie を扱う利用側コードの実装がそれぞれ必要
- CRI系シンボル（`ENABLE_CRIWARE_ADX(_LE)` / `ENABLE_CRIWARE_SOFDEC` / `ENABLE_CRIWARE_FILESYSTEM`）に依存する他モジュールも連動: `Modules.Sound` の CriWare 版（ADX）・`ExternalAsset.GetCueInfo`（ADX）/ `GetMovieInfo`（Sofdec）（`ExternalAsset.cri.cs`）・`Modules.Movie`（Sofdec）
- （CRI有効時）初期化順の制約: `CriAtomServer.CreateInstance()` を `CriAtom` 生成より先に呼ばないと `CriAtom` が `CriAtomServer` を勝手に生成してしまう（`CriWareObject.Initialize` 内コメント）。`CriFsServer` / `CriAtom` はライブラリ初期化完了後に生成する
- `EditorCriWareMute` / `Editor/` 配下は全てエディタ専用。ランタイムコードから参照しないこと

## 関連

- [Sound](Sound.md) — サウンド再生基盤（CRI 有効時は ADX 版が有効化される）
- [ExternalAsset](ExternalAsset.md) — 配信アセット基盤（CRI有効時は `CriAssetManager` を内包。`GetCueInfo` / `GetMovieInfo` は CRI 有効時のみ）
- [Movie](Movie.md) — Sofdec ムービー再生

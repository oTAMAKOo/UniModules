# CriWare

CRIWARE の初期化とアセット管理の機能。
CRI ライブラリの初期化、CRI アセットのダウンロード、エディタでの CRI 成果物の取り込みができる。
CRI SDK と ENABLE_CRIWARE 系のシンボルが必要。

## 基本情報

> **namespace**: `Modules.CriWare`（Editor専用: `Modules.CriWare.Editor`。`CriSoundAssetInspector` / `CriWareConfigInspector` / `EditorCriWareMute` は `Modules.CriWare`）
> **場所**: `Modules/CriWare/`
> **依存**: CRI SDK（`CriWare` namespace） / UniTask / R3 / Extensions / Modules.ExternalAssets / Modules.Network / Modules.Devkit.*

## 概要

CRIWARE（ADX=サウンド / Sofdec=ムービー / FileSystem=ダウンロード）のライブラリ初期化と、CRIアセット（.acf/.acb/.awb/.usm）の配信ダウンロード・エディタ取込を担う基盤。

主要クラス: `CriWareObject`（SingletonMonoBehaviour。CRIライブラリ初期化の管理。シンボル未定義時は**クラス定義のみコンパイルされメンバーは縮退**）/ `CriWareCustomErrorHandler`（エラーログ中継。同じく縮退コンパイル）/ `CriAssetManager`（Singleton。CRIアセットの配信DL管理。シンボル定義時のみコンパイル）/ `CriAssetDefinition`（拡張子定数）/ `CriWareConfig`（abstract ScriptableObject。暗号化済みCRI認証キー）。Editor側（`Modules.CriWare.Editor`）に `CriAssetUpdater` / `CriAssetUpdateWindow`（CRI成果物のUnity取込）/ `CriAssetGenerator`（ビルド時配信出力）/ `CriForceInitializer`（エディタ上での CRI 強制初期化）/ `CriAssetConfig`（取込元・取込先フォルダ等の設定）/ `CriExternalSoundInfoGenerator`（配信サウンドの ACB からキュー一覧を作る）等。Editor側は `CriAssetConfig` と `CriExternalSoundInfoGenerator` だけ常時コンパイルされ（メンバーは縮退）、ほかはシンボル定義時のみコンパイルされる。

`ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` のいずれかを利用側で定義し CRI SDK を導入すると有効化される。ダウンロード処理（`CriAssetManager.UpdateCriAsset` 等）と `CriFsServer` の生成は、追加で `ENABLE_CRIWARE_FILESYSTEM` が必要。未定義時はモジュール内のほぼ全コードが `#if ENABLE_CRIWARE_*` で無効化される。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| CRIを有効化したい | 利用側で `ENABLE_CRIWARE_*` を定義 + CRI SDK 導入 + `CriWareConfig` 派生の実装 + `CriWareObject.Initialize` の呼出組込（罠参照） |
| （CRI有効時）ライブラリ初期化 | `CriWareObject.Instance.Initialize(cryptoKey)` |
| （CRI有効時）CRIのログ出力を種類ごとに切り替える | `CriWareObject.Instance.ErrorHandler.SetLogOutput(logType, output)`（既定は開発ビルドのみ出力） |
| （CRI有効時）起動時にDSPバス設定をアタッチしたい | `CriWareObject` のインスペクタの `dspBusSettingName` に、ACF に含まれるDSPバス設定の名前を入れる（空なら何もアタッチしない） |
| （CRI有効時）DSPバス設定を切り替えたい / 外したい | `CriWareObject.Instance.AttachDspBusSetting(settingName)` / `DetachDspBusSetting()`。アタッチ中の名前は `AttachedDspBusSettingName` |
| （CRI有効時）CRIアセットのDL | `CriAssetManager`（`ExternalAsset` が内部で自動利用。直接触らない） |
| （CRI有効時）DLのタイムアウト・エラーを購読 | `CriAssetManager.Instance.OnTimeOutAsObservable()` / `OnErrorAsObservable()` |
| （CRI有効時）CRI拡張子の定数 | `CriAssetDefinition.AcbExtension` / `AwbExtension` / `AcfExtension` / `UsmExtension` / `AssetAllExtensions` |
| （CRI有効時）認証キーの復号取得 | `CriWareConfig.LoadInstance(resourcesPath)` → `await GetCriWareKey()` |
| 【Editor・CRI有効時】CRI成果物をUnityへ取込 | `CriAssetUpdater.ExecuteAll()`（個別は `ExecuteSoundAssets()` / `ExecuteMovieAssets()`）/ `CriAssetUpdateWindow.Open()` |
| 【Editor・CRI有効時】エディタ上で CRI を初期化 | `CriForceInitializer.Initialize()` |
| 【Editor・ADX有効時】エディタ再生をミュート | `EditorCriWareMute.Prefs.editorAudioMute` |

## 使い方

- `CriAssetManager` の基盤内の使用例（CRI有効時のみコンパイル）: `Modules/ExternalAssets/ExternalAsset.cri.cs` の `InitializeCri()` 参照
- `CriWareObject` は SingletonMonoBehaviour。インスペクタの `initializerPrefab` に `CriWareInitializer` を持つプレハブを設定しておき、起動フローから `Initialize(cryptoKey)` を呼ぶ（認証キーが不要なら空文字を渡す）

## 注意点・罠

- `CriWareObject` / `CriWareCustomErrorHandler` / `CriWareConsoleEvent` は**クラス定義自体は常時コンパイルされる**（`#if` がメンバー内側のため）。シンボル未定義時は `CriWareObject.Initialize` が空メソッドになる。シーンの重複配置チェック等で `typeof(CriWareObject)` を参照するコードはこの縮退状態でも動く
- **シンボル定義だけでは有効化できない**。(1) CRI SDK（`CriWare` namespace のプラグイン）の導入、(2) `CriWareObject.Initialize(cryptoKey)` を呼び出す起動フローの組込、(3) `CriWareConfig` 派生の実装、(4) CRI Cue/Movie を扱う利用側コードの実装がそれぞれ必要
- CRI系シンボル（`ENABLE_CRIWARE_ADX(_LE)` / `ENABLE_CRIWARE_SOFDEC` / `ENABLE_CRIWARE_FILESYSTEM`）に依存する他モジュールも連動: `Modules.Sound` の CriWare 版（ADX）・`ExternalAsset.GetCueInfo`（ADX）/ `GetMovieInfo`（Sofdec）（`ExternalAsset.cri.cs`）・`Modules.Movie`（Sofdec）
- （CRI有効時）初期化順の制約: `CriAtomServer.CreateInstance()` を `CriAtom` 生成より先に呼ばないと `CriAtom` が `CriAtomServer` を勝手に生成してしまう（`CriWareObject.Initialize` 内コメント）。`CriFsServer` / `CriAtom` はライブラリ初期化完了後に生成する
- （CRI有効時）`ENABLE_CRIWARE_ADX_LE` では認証キーを設定しない（`cryptoKey` を渡しても使われない）。開発ビルド以外ではインゲームプレビューが無効化される
- （CRI有効時）**DSPバス設定は、名前を指定したときだけアタッチする**。`dspBusSettingName` が空（既定）なら `Initialize` は何もアタッチしない。名前を入れると、ACF の登録直後にアタッチする。ACF のファイル名が未設定の場合は ACF を登録しないため、アタッチも行わない
- （CRI有効時）`AttachDspBusSetting` は、名前が空・ライブラリが未初期化・ACF に無い名前の場合、アタッチせず false を返す（後ろの 2 つは警告ログを出す）。アタッチ中と同じ名前なら何もせず true を返し、別の名前ならデタッチしてから切り替える。アタッチ・デタッチの間は再生中の音が途切れることがあるため、音を再生していないタイミングで呼ぶ
- （CRI有効時）`Initialize` は、ACF を登録する前にアタッチ中のDSPバス設定をデタッチする。`Initialize` を呼び直した場合、`dspBusSettingName` の設定がアタッチし直され、コードから切り替えた設定は残らない
- `EditorCriWareMute` / `Editor/` 配下は全てエディタ専用。ランタイムコードから参照しないこと。`EditorCriWareMute` は `Editor/` の外にあり、`ENABLE_CRIWARE_ADX` または `ENABLE_CRIWARE_ADX_LE` かつ `UNITY_EDITOR` の時だけコンパイルされる

## 関連

- [Sound](../Sound/Manual.md) — サウンド再生基盤（CRI 有効時は ADX 版が有効化される）
- [ExternalAssets](../ExternalAssets/Manual.md) — 配信アセット基盤（CRI有効時は `CriAssetManager` を内包。`GetCueInfo` / `GetMovieInfo` は CRI 有効時のみ）
- [Movie](../Movie/Manual.md) — Sofdec ムービー再生

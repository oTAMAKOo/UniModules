# CriWare

> **namespace**: `Modules.CriWare`（Editor専用: `Modules.CriWare.Editor`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/CriWare/`
> **Client側使用**: 1ファイル（`SceneManager.cs` の型参照のみ、2026-07時点）
> **依存**: CRI SDK（`CriWare` namespace、**未導入**） / UniTask / R3 / Extensions / Modules.ExternalAssets / Modules.Net / Modules.Devkit.*

## 概要

CRIWARE（ADX=サウンド / Sofdec=ムービー / FileSystem=ダウンロード）のライブラリ初期化と、CRIアセット（.acf/.acb/.awb/.usm）の配信ダウンロード・エディタ取込を担う基盤。

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

## 主要クラス

「コンパイル」列: シンボル未定義の本プロジェクトでコンパイルされるか。

| クラス | 種別 | コンパイル | 役割 |
|---|---|---|---|
| `CriWareObject` | SingletonMonoBehaviour | **される**（メンバーは縮退） | CRIライブラリ初期化の管理。`CriAtomServer.CreateInstance()` → `CriWareInitializer`（Prefabから生成・手動初期化） → `CriWareCustomErrorHandler` → `CriFsServer` → `CriAtom` + ACF登録、の順序を保証する |
| `CriWareConsoleEvent` | static class | される | UnityConsole 用イベント名("CRI")と色の定数 |
| `CriWareCustomErrorHandler` | MonoBehaviour | される（中身は縮退） | `CriErrorNotifier` のコールバックを Unity ログへ中継。`SetLogOutput(LogType, bool)` で出力制御（既定: DebugBuildのみ全出力） |
| `CriAssetManager` | Singleton（sealed partial） | されない | CRIアセット（音声/ムービー）の配信DL管理。`CriFsWebInstaller` を複数並列（既定8）で運用、タイムアウト180秒。`ExternalAsset.cri.cs` から利用される |
| `CriAssetManager.CriAssetInstall` | class（`.install.cs`、`ENABLE_CRIWARE_FILESYSTEM`） | されない | DL1件分。ファイル欠損時のリトライ（3回/10秒間隔）付き |
| `CriAssetDefinition` | static class | されない | 拡張子定数（.acf/.acb/.awb/.usm） |
| `CriWareConfig` | abstract ScriptableObject | されない | 暗号化済みCRI認証キーの保持と復号（`GetCriWareKey`。`GetCryptoKey` は派生側で実装） |
| `EditorCriWareMute` | static class（**Editor専用**） | されない | エディタ再生ミュートの Prefs |

### Editor/（`Modules.CriWare.Editor` — 全て**エディタ専用**かつコンパイル対象外）

| クラス | 種別 | 役割 |
|---|---|---|
| `CriAssetConfig` | SingletonScriptableObject | CRI成果物の取込設定（取込元/先フォルダ、ACFパス、生成スクリプトのnamespace） |
| `CriAssetUpdater` | static class | CRI成果物置き場から StreamingAssets へ acb/awb/usm を取込み、`SoundScriptGenerator`（[Sound](Sound.md)）で `Sounds.Cue` スクリプトを自動生成 |
| `CriAssetUpdateWindow` | SingletonEditorWindow | 上記のGUI |
| `CriAssetGenerator` | class | ビルド時にCRIアセットを配信用出力先へコピー（`AssetInfoManifest` 連携） |
| `CriExternalSoundInfoGenerator` | static class | 外部配信ACBから CueSheet/Cue 一覧を生成 |
| `CriForceInitializer` | static class | エディタでのCRI強制初期化 |
| `CriAssetConfigInspector` / `CriWareConfigInspector` / `CriSoundAssetInspector` | UnityEditor.Editor / ExtendInspector | 各種インスペクタ拡張（ACB内Cue一覧表示等） |

## 使い方(実例)

### Client側の唯一の使用箇所（型参照のみ）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs
using Modules.CriWare;

// シーン重複配置チェックの対象型として登録しているのみ.
{ typeof(CriWareObject), DuplicatedSettings.Default },
```

### 基盤内の使用例（CRI有効時のみコンパイルされる参考コード）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cri.cs
// ※ #if ENABLE_CRIWARE_* 内のため本プロジェクトでは無効.
private void InitializeCri()
{
    // CriAssetManager初期化.

    criAssetManager = CriAssetManager.CreateInstance();
    criAssetManager.Initialize(SimulateMode);
    criAssetManager.SetNumInstallers(CriDefaultInstallerCount);
    criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
    criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
}
```

## API(主要公開メンバー)

いずれも CRI 有効時のみ存在（`CriWareObject.Initialize` と `CriWareCustomErrorHandler` はシグネチャのみ常在）。

### CriWareObject

| メンバー | 説明 |
|---|---|
| `Initialize(string cryptoKey)` | CRIライブラリ一式を初期化（本プロジェクトでは中身が空になる）。`Initializer` / `ErrorHandler` プロパティは有効時のみ |

### CriAssetManager

| メンバー | 説明 |
|---|---|
| `Initialize(bool simulateMode = false)` | 初期化。`CriFsWebInstaller.ExecuteMain()` の毎フレーム駆動を開始 |
| `SetNumInstallers(uint)` / `SetLocalMode(bool)` / `SetUrl(remoteUrl, versionHash)` / `SetManifest(AssetInfoManifest)` | 並列DL数 / ローカルモード / DL元URL / マニフェスト設定 |
| `UpdateCriAsset(installPath, assetInfo, progress, cancelToken) : UniTask` | CRIアセット1件をDL（音声は acb+awb の2ファイル対応） |
| `BuildDownloadUrl(AssetInfo) : string` / `GetFilePath(installPath, AssetInfo) : string` | DL URL / ローカル保存パス構築 |
| `IsCriAsset(string filePath) : bool` | 拡張子がCRIアセットか判定 |
| `WaitQueueingInstall(AssetInfo, cancelToken) : UniTask` / `ClearInstallQueue()` | DL待機 / キュー全キャンセル |
| `OnTimeOutAsObservable() : Observable<AssetInfo>` / `OnErrorAsObservable() : Observable<Exception>` | タイムアウト（180秒）/ エラー通知 |

### CriWareConfig / CriWareCustomErrorHandler / CriAssetDefinition

| メンバー | 説明 |
|---|---|
| `CriWareConfig.LoadInstance(resourcesPath) : CriWareConfig` | Resources からロード（static） |
| `CriWareConfig.GetCriWareKey() : UniTask<string>` | 認証キーを復号して取得（`GetCryptoKey` は abstract） |
| `CriWareCustomErrorHandler.Initialize()` / `SetLogOutput(LogType, bool)` | ログ中継の初期化 / 出力レベル制御 |
| `CriAssetDefinition.AcfExtension` 等 | `.acf` / `.acb` / `.awb` / `.usm` / `AssetAllExtensions` |

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
- Movie（ドキュメント未作成）— Sofdec ムービー再生（同様にコンパイル対象外）

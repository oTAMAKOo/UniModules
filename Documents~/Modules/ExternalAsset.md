# ExternalAsset

> **namespace**: `Modules.ExternalAssets`（AssetBundle低層・暗号化ストリームは `Modules.AssetBundles`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ExternalAsset/`
> **Client側使用**: 約32ファイル（`Modules.ExternalAssets` 30 / `Modules.AssetBundles` 3、2026-07時点）
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`, PathUtility 等） / Modules.Net（WebRequest / WebDownload） / Modules.Devkit.Console / Modules.Performance / Modules.ApplicationEvent / 条件付き: Modules.CriWare・Modules.Sound・Modules.Movie（`ENABLE_CRIWARE_*`）、Modules.Amazon.S3（`ENABLE_AMAZON_WEB_SERVICE`）

## 概要

配信アセット（AssetBundle / 生ファイル / CRIアセット）のダウンロード・キャッシュ・バージョン管理・ロード・解放を一元管理する基盤。窓口は `ExternalAsset`（`Singleton<ExternalAsset>`、機能別 partial）。
アセット一覧は `AssetInfoManifest`（ScriptableObject）で管理し、**ローカルキャッシュのファイル名にコンテンツハッシュ(SHA256先頭32文字)を埋め込む**ことで、更新要否判定を `File.Exists` だけで完結させる設計。
Claude がアセットをロードする時は原則 `await ExternalAsset.LoadAsset<T>(resourcePath)` の1行（未DLなら自動DL→ロードまで面倒を見る）。

### データの流れ（全体像）

```
[エディタ] Assets/Resource (External)/ にアセット配置
    → ManageWindow でグループ登録 (ManagedAssets.asset / ManageInfo)
    → AssetInfoManifest 生成（Postprocessor による自動再生成あり）
[ビルド]  BuildManager.Build（メニュー or Jenkins: JenkinsResource）
    → SBPでAssetBundleビルド(LZ4) → .package化(ストリーム暗号化) → RootHash.txt出力
    → ExternalAssetS3Uploader → S3 / rootHash を PlayFab TitleData へ (AssetRootHash)
[実行時]  Initialize → SetUrl(Urls.AssetsUrl, rootHash) → UpdateManifest()
    → LoadAsset<T>(resourcePath)（未DL分は自動DL）
    URL:       {AssetsUrl}/{Platform}/{rootHash}/{FileName}.package?v={hash}
    キャッシュ: {persistentDataPath}/Contents/{Hash先頭32文字}{拡張子}
```

- **resourcePath**（ロードパス）= `Assets/Resource (External)/` からの相対パス・**拡張子付き**。例: `"Contents/Item/Icon/ItemIconAtlas.spriteatlasv2"`
- `Share:` プレフィックス付きは `Assets/Resource (Share)/` 配下（共有アセット。`ExternalAsset.ShareGroupPrefix`）
- リモートファイル名（`AssetInfo.FileName`）はアセットバンドル名等のハッシュで難読化（`AssetManagement.SetCryptoFileName`）

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **アセットをロード（最頻出）** | `await ExternalAsset.LoadAsset<T>(resourcePath)`（static。未DLなら自動DL） |
| ロード後も保持し手動解放したい | `LoadAsset<T>(resourcePath, autoUnload: false)` → 不要時 `ExternalAsset.UnloadAssetBundle(resourcePath)` |
| 事前ダウンロードのみ（進捗付き） | `await ExternalAsset.UpdateAsset(resourcePath, progress, cancelToken)`（static） |
| マニフェスト（アセット一覧）更新 | `await ExternalAsset.Instance.UpdateManifest(cancelToken)`（起動時必須） |
| 配信URL・バージョン設定 | `ExternalAsset.Instance.SetUrl(remoteUrl, versionHash)` |
| 更新が必要なアセット一覧取得 | `await Instance.GetRequireUpdateAssetInfos(groupName)` |
| 個別の更新要否判定 | `Instance.IsRequireUpdate(assetInfo)` |
| グループのアセット情報列挙 | `Instance.GetGroupAssetInfos(groupName)` |
| resourcePath からアセット情報取得 | `Instance.GetAssetInfo(resourcePath)` / `Instance.ExistAssetInfo(resourcePath)` |
| 全AssetBundle解放 | `ExternalAsset.UnloadAllAssetBundles()`（static） |
| キャッシュ全削除 | `await Instance.DeleteAllCache()` |
| 指定アセットのキャッシュ削除 | `await Instance.DeleteCache(assetInfos)` |
| DL中断・キュークリア | `Instance.ClearDownloadQueue()` |
| エラー / タイムアウト購読 | `Instance.OnErrorAsObservable()` / `Instance.OnTimeOutAsObservable()` |
| ロード済みAssetBundle一覧（デバッグ） | `ExternalAsset.GetLoadedAssets()`（名前+参照カウント） |
| シーンをAssetBundleからロード | `await ExternalAssetSceneUtility.LoadScene(loadPath, mode)` → `UnLoadScene(scenes)` |
| AssetBundle暗号化方式の差し替え | `Instance.SetAssetBundleFileStreamFactory(factory)`（Client実装: `CustomAssetBundleFileStream`） |
| CRIサウンドのCueInfo取得 | `await ExternalAsset.GetCueInfo(resourcePath, cue)`（`ENABLE_CRIWARE_ADX*` 時のみ。**本プロジェクトでは無効**） |
| CRIムービー情報取得 | `await ExternalAsset.GetMovieInfo(resourcePath)`（`ENABLE_CRIWARE_SOFDEC` 時のみ。同上） |
| DL/ロードに外部処理を差し込む | `Instance.SetUpdateAssetHandler(IUpdateAssetHandler)` / `SetLoadAssetHandler(ILoadAssetHandler)` |
| 【Editor】Simulateモード切替 | メニュー `Extension > ExternalAsset > Simulate Mode`（`ExternalAsset.Prefs.isSimulate`） |
| 【Editor】アセットのグループ登録・管理 | メニュー `Extension > ExternalAsset > Open AssetManageWindow` |
| 【Editor】アセットのロードパス確認 | メニュー `Extension > ExternalAsset > Open AssetNavigationWindow` |
| 【Editor】マニフェスト手動生成 | メニュー `Extension > ExternalAsset > Generate AssetInfoManifest` |
| 【Editor】外部アセットビルド | `BuildExternalAssets.Execute()` / `BuildManager.Build(exportPath, manifest)` |
| 【Editor】S3アップロード | `ExternalAssetS3Uploader.Upload(uploader)`（Client実装: `Dominion.Editor.ExternalAsset.Uploader`） |

## 主要クラス

### ランタイム（`Modules.ExternalAssets`）

| クラス | 種別 | 役割 |
|---|---|---|
| `ExternalAsset` | Singleton（sealed partial） | 窓口。partial 構成: `.cs`(本体/マニフェスト/更新) / `.assetbundle.cs`(ロード/解放) / `.fileasset.cs` / `.cri.cs`(CRI, 条件コンパイル) / `.cache.cs`(キャッシュ削除) / `.version.cs`(更新判定) / `.share.cs`(Share prefix) / `.editor.cs`(Prefs.isSimulate) |
| `AssetInfo` | Serializable class | 1アセットの情報。`ResourcePath` / `Group` / `Labels` / `FileName`(難読化名) / `Size` / `CRC` / `Hash`(SHA256) / `AssetBundle` / `IsAssetBundle` |
| `AssetBundleInfo` | Serializable class | AssetBundle付随情報。`AssetBundleName` / `Dependencies` / `CRC` |
| `AssetInfoManifest` | ScriptableObject | 全 `AssetInfo` + `VersionHash`(rootHash)。それ自体もAssetBundleとして配信され毎回DLされる |
| `DownloadProgressInfo` | class | DL進捗通知（`AssetInfo` + `Progress` 0〜1）。`IProgress<DownloadProgressInfo>` で受け取る |
| `IUpdateAssetHandler` / `ILoadAssetHandler` | interface | 更新/ロードの前後フック（`OnUpdateRequest/Finish`, `OnLoadRequest/Finish`） |
| `AssetInfoNotFoundException` | Exception | マニフェストに無い resourcePath 指定時に `OnError` へ流れる |
| `FileAssetManager` | Singleton | 非AssetBundle生ファイル（.asset以外の任意ファイル）のDL管理 |
| `FileAssetDownLoader` | class（`FileDownLoader<DownloadRequest>` 派生） | 生ファイルDL実体。一時ファイル(.tmp)→リネームで原子的に書き込み |
| `ExternalAssetFileNameManager` | Singleton | ハッシュベースのキャッシュファイル名構築（`BuildHashedFileName` 等）。スレッドセーフ |
| `ExternalAssetSceneUtility` | static class | シーンAssetBundleのロード/アンロード。SimulateMode では `EditorSceneManager` 経由 |
| `ShaderReApply` | MonoBehaviour | AssetBundle由来マテリアルのシェーダーを `Shader.Find` で再適用（エディタでのピンク化対策。Start使用の既存例外） |

### ランタイム（`Modules.AssetBundles`）

| クラス | 種別 | 役割 |
|---|---|---|
| `AssetBundleManager` | Singleton（sealed partial） | AssetBundleのDL・依存解決・参照カウント・ロード（`LoadFromStreamAsync`）・Unload。DL/ロードのタイムアウト(60s/10s)+リトライ(5回/2秒間隔)内蔵。`.editor.cs` に SimulateLoadAsset(AssetDatabase直読み) |
| `AssetBundleDependencies` | class | アセットバンドル名→依存名[] のテーブル。再帰的な全依存取得（循環参照対策済み） |
| `AssetBundleFileStream` | abstract（Stream派生） | パッケージ復号ストリーム基底。`Transform(buffer, offset, count, streamPos)` を実装して差し替え |
| `DefaultAssetBundleFileStream` | sealed | デフォルト実装（位置非依存ビット反転） |
| `AesCtrAssetBundleFileStream` | abstract | AES-CTR実装（nonce はAssetBundle毎に派生させる設計）。Client実装: `CustomAssetBundleFileStream` |
| `AssetBundleFileStreamFactory` | delegate | `(Stream, assetBundleName) => AssetBundleFileStream`。ビルド時の暗号化とロード時の復号の両方で使う |
| `AssetBundleStreamCipher` | static class | `Xor` / `BitInvert` / `DeriveSeed`(SHA256派生シード) |

### エディタ専用（`Editor/` サブフォルダ）

| クラス | 種別 | 役割 |
|---|---|---|
| `AssetManagement` | Singleton | 管理情報→`AssetInfo` 変換・アセットバンドル名生成（`AssetBundleNamingRule`）・除外判定（`IgnoreType`）・難読化ファイル名設定 |
| `ManagedAssets` / `ManageInfo` | SingletonScriptableObject / Serializable | 管理登録データ本体（guid・group・labels・isAssetBundle・命名規則）。ManageWindowで編集 |
| `ExternalAssetConfig` | SingletonScriptableObject | ビルド出力先(`ExportDirectory`)・除外対象（管理外/AB対象外/フォルダ/拡張子）・依存検査除外 |
| `AssetInfoManifestGenerator` | class(static メソッド) | マニフェスト生成（全ManageInfo走査→AssetBundle名適用→保存） |
| `AssetInfoManifestAutoUpdater` | Singleton | 1秒間隔ループでマニフェスト自動再生成（フォーカス外・再生中・コンパイル中はスキップ） |
| `ManifestUpdateAssetPostprocessor` | AssetPostprocessor | `Resource (External)` 配下の変更検知→自動再生成リクエスト |
| `BuildExternalAssets` | static class | ビルドの入口（確認ダイアログ→マニフェスト生成→依存検証→`BuildManager.Build`） |
| `BuildManager` | static class | ビルド統括。`BundlePipeline` / `FileStreamFactory` 差し替え点、`OnPreBuildAsObservable`(鍵ロード等)、`RootHash.txt` 出力、依存検証 |
| `BuildAssetBundle` | class | SBPビルド実行・差分検出（最終更新日時比較）・依存/ハッシュのマニフェスト書き込み・不要ファイル掃除。出力先 `Library/AssetBundleBuildCache/{Platform}` |
| `BuildAssetBundlePipeline` | sealed（`IBuildAssetBundlePipeline`） | Scriptable Build Pipeline 実行（LZ4圧縮 + BuiltInShader抽出）。`BuildResult` を返す |
| `BuildAssetBundlePackage` | class | AssetBundle→`.package` 暗号化変換と出力先コピー（25件ずつ並列） |
| `FileAssetGenerator` | class(static) | 非AssetBundleファイルを出力先へコピー |
| `S3Uploader` | abstract（`S3UploaderBase` 派生） | 差分アップロード（ハッシュ比較で新規/更新/削除）。`ENABLE_AMAZON_WEB_SERVICE` 時のみ |
| `ExternalAssetS3Uploader` | class(static) | `BuildManager.GetExportPath()` の成果物を `S3Uploader.Execute` に渡す入口 |
| `ManageWindow`（+ `HeaderView` / `ManageAssetView` / `ManageInfoView` / `LabalPopupView`） | EditorWindow / View | グループ・管理アセットのGUI編集 |
| `AssetNavigationWindow` | EditorWindow | 選択アセットの Group / AssetBundleName / LoadPath 表示（クリップボードコピー付き） |
| `InvalidDependantWindow` | EditorWindow | 外部アセットが `Resource (External)` 外を参照している違反の一覧表示 |
| `GenerateAssetInfoManifestWindow` | EditorWindow | 手動Generate + AutoUpdater の ON/OFF |
| `AssetInfoGuidExtension` | static class | `AssetInfo.GetGuid()` 拡張（Editor専用・キャッシュ付き） |
| `AssetInfoManifestInspector` / `ManagedAssetsInspector` | CustomEditor | マニフェスト / 管理データの検索可能なインスペクタ表示 |

## 使い方(実例)

### 1. 初期化（起動時1回。通常は触らない — InitializeObject が実施済み）

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
public static void InitializeExternalAsset()
{
    var appConfig = AppConfig.Instance;
    var externalAsset = ExternalAsset.Instance;

    var assetsFolderName = UnityPathUtility.AssetsFolder;

    var externalAssetFolderName = appConfig.ExternalAssetFolderName;      // "Resource (External)"
    var externalAssetDirectory = PathUtility.Combine(assetsFolderName, externalAssetFolderName);

    var shareAssetFolderName = appConfig.ShareAssetFolderName;            // "Resource (Share)"
    var shareAssetDirectory = PathUtility.Combine(assetsFolderName, shareAssetFolderName);

    externalAsset.Initialize(externalAssetDirectory, shareAssetDirectory);
    externalAsset.SetInstallDirectory(UnityPathUtility.PersistentDataPath);

    CustomAssetBundleFileStream CreateFileStream(Stream stream, string assetBundleName)
    {
        return new CustomAssetBundleFileStream(stream, assetBundleName);  // AES-CTR復号
    }

    externalAsset.SetAssetBundleFileStreamFactory(CreateFileStream);

    // エラー時は共通ハンドリング (タイトルへ遷移).
    externalAsset.OnErrorAsObservable().Subscribe(ex => OnError(ex));
    externalAsset.OnTimeOutAsObservable().Subscribe(ex => OnError(null));
}
```

### 2. 起動フロー: URL設定 → マニフェスト更新（タイトル画面）

```csharp
// Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs
public async UniTask FetchInitalData()
{
    var systemModel = SystemModel.Instance;
    var externalAsset = ExternalAsset.Instance;

    // rootHash は PlayFab TitleData (AssetRootHash) 由来.
    externalAsset.SetUrl(Urls.AssetsUrl, systemModel.AssetRootHash);

    var tasks = new UniTask[]
    {
        UniTask.Defer(() => externalAsset.UpdateManifest()),
        UniTask.Defer(() => masterUpdateManager.UpdateMasterVersions()),
    };

    await UniTask.WhenAll(tasks);
}
```

### 3. ロード基本形（最頻出パターン: パス直指定・型指定）

```csharp
// Client/Assets/Scripts/Client/Manager/CommonAssetManager.cs
public async UniTask<Sprite> GetItemIconSprite(string spriteName)
{
    if (itemIconAtlasCache != null){ return itemIconAtlasCache.GetSprite(spriteName); }

    var itemIconAtlas = await ExternalAsset.LoadAsset<SpriteAtlas>("Contents/Item/Icon/ItemIconAtlas.spriteatlasv2");

    if (itemIconAtlasCache == null)
    {
        itemIconAtlasCache = new SpriteAtlasCache(itemIconAtlas, GetType().FullName + "-itemIconAtlasCache");
    }

    return itemIconAtlasCache.GetSprite(spriteName);
}
```

```csharp
// Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs (TextAsset系)
var textDataLoadPath = $"TextData/TextData-{langageIdentifier}.asset";

var asset = await ExternalAsset.LoadAsset<TextDataAsset>(textDataLoadPath);
```

### 4. autoUnload: false + 明示解放（アトラスからSprite取得後すぐ解放する例）

```csharp
// Client/Assets/Scripts/Client/Tutorial/TutorialNavigation.cs
public const string FaceAtlasAssetLoadPath = "Contents/Assistant/Thumbnail/AssistantThumbnail.spriteatlasv2";

var atlas = await ExternalAsset.LoadAsset<SpriteAtlas>(FaceAtlasAssetLoadPath, false);

backgroundImage.sprite = atlas.GetSprite("Background");
faceImage.sprite = atlas.GetSprite("101");

ExternalAsset.UnloadAssetBundle(FaceAtlasAssetLoadPath);
```

### 5. 事前ダウンロード（進捗付き並列DL）

```csharp
// Client/Assets/Scripts/Client/Scene/WorldMap/TileMap/WorldMapTileManager.cs
// ダウンロードが必要なアセットのみ対象にする.
var updateTargets = externalAsset.GetGroupAssetInfos(MapAssetGroupName)
    .Where(x => x.ResourcePath.StartsWith(mapAssetRootPath))
    .Where(x => externalAsset.IsRequireUpdate(x))
    .ToArray();

var progressValues = new float[updateTargets.Length];

var tasks = new List<UniTask>();

for (var i = 0; i < updateTargets.Length; i++)
{
    var index = i;

    IProgress<DownloadProgressInfo> downloadProgress = new Progress<DownloadProgressInfo>(info =>
    {
        progressValues[index] = info.Progress;

        progress.Report(progressValues.Sum() / progressValues.Length);
    });

    var task = ExternalAsset.UpdateAsset(updateTargets[index].ResourcePath, downloadProgress);

    tasks.Add(task);
}

await UniTask.WhenAll(tasks);
```

### 6. 起動時の一括更新（更新必要一覧→順次DL）

```csharp
// Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs
var allAssetInfos = await externalAsset.GetRequireUpdateAssetInfos();  // 全グループの要更新一覧.

// ...(グループフィルタ後)...

await ExternalAsset.UpdateAsset(assetInfo.ResourcePath, cancelToken: cancelToken);
```

## API(主要公開メンバー)

### ExternalAsset（static メンバー）

| メンバー | 説明 |
|---|---|
| `static ExternalAsset Instance` | シングルトン（初回アクセスで自動生成。`Singleton<T>` 由来） |
| `static bool Exists` / `static bool Initialized` | インスタンス生成済みか / `Initialize` 完了済みか |
| `static UniTask<T> LoadAsset<T>(string resourcePath, bool autoUnload = true)` | **標準ロードAPI**。未DLなら依存含め自動DL→ロード。失敗時は throw せず null 返却+`OnError` 通知。`T : UnityEngine.Object`（Component派生ならGameObject経由でGetComponent） |
| `static UniTask UpdateAsset(string resourcePath, IProgress<DownloadProgressInfo> progress = null, CancellationToken cancelToken = default)` | DLのみ実施（依存AssetBundle含む）。ローカルが最新なら何もしない |
| `static void UnloadAssetBundle(string resourcePath, bool unloadAllLoadedObjects = false)` | 参照カウントを減らし0で `AssetBundle.Unload`。true指定はロード済みオブジェクトも破棄（使用中参照が壊れるので注意） |
| `static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)` | 全AssetBundle解放（参照カウント無視の強制） |
| `static Tuple<string, int>[] GetLoadedAssets()` | ロード済み（AssetBundle名, 参照カウント）一覧。リーク調査用 |
| `static string GetAssetPathFromAssetInfo(string externalAssetPath, string shareResourcesPath, AssetInfo assetInfo)` | resourcePath→Unityアセットパス変換（`Share:` 解決込み） |
| `static UniTask<CueInfo> GetCueInfo(string resourcePath, string cue)` | 【CRI】ACB/AWBをDL・インストールし `CueInfo` 取得（`ENABLE_CRIWARE_ADX*` 時のみ定義） |
| `static UniTask<ManaInfo> GetMovieInfo(string resourcePath)` | 【CRI】USMをDLし `ManaInfo` 取得（`ENABLE_CRIWARE_SOFDEC` 時のみ定義） |
| `const string ShareGroupName = "Share"` / `ShareGroupPrefix = "Share:"` | 共有アセットグループ定数 |
| `static readonly string ContentsFolderName = "Contents"` | インストール先フォルダ名（`{persistentDataPath}/Contents`） |

### ExternalAsset（instance メンバー）

| メンバー | 説明 |
|---|---|
| `void Initialize(string externalAssetDirectory, string shareAssetDirectory)` | 初期化（二重呼び出しは無視）。内部で AssetBundleManager / FileAssetManager /（CRI時）CriAssetManager も初期化 |
| `void SetUrl(string remoteUrl, string versionHash)` | 配信元URLとrootHash設定。**UpdateManifest 前に必須** |
| `void SetInstallDirectory(string directory)` | キャッシュ保存先設定（`/Contents` が付与される。iOSはNoBackupFlag設定） |
| `void SetLocalMode(bool localMode)` | 配信なしローカル同梱モード（StreamingAssets参照。Client未使用） |
| `UniTask<bool> UpdateManifest(CancellationToken cancelToken = default)` | AssetInfoManifest を必ずDLして差し替え+不要キャッシュ削除。**これ以前の LoadAsset はエラー** |
| `IEnumerable<AssetInfo> GetGroupAssetInfos(string groupName = null)` | グループのアセット情報列挙（null で全件） |
| `AssetInfo GetAssetInfo(string resourcePath)` / `bool ExistAssetInfo(string resourcePath)` | resourcePath からアセット情報取得 / 存在確認 |
| `UniTask<AssetInfo[]> GetRequireUpdateAssetInfos(string groupName = null)` | 更新が必要なアセット一覧（スレッドプールで高速判定。SimulateModeは常に空） |
| `bool IsRequireUpdate(AssetInfo assetInfo)` | 更新要否（ハッシュ埋め込みファイル名の存在確認のみで完結） |
| `void ClearDownloadQueue()` | 実行中DLをキャンセルしキュークリア（内部CancellationTokenSource再生成） |
| `string GetFilePath(AssetInfo assetInfo)` | ローカルキャッシュの実ファイルパス取得 |
| `UniTask DeleteAllCache()` | 全キャッシュ削除（`OnReleaseManagedAssets` 発火→全Unload→ファイル削除） |
| `UniTask DeleteCache(AssetInfo[] assetInfos)` | 指定アセットのキャッシュ削除 |
| `void SetUpdateAssetHandler(IUpdateAssetHandler)` / `SetLoadAssetHandler(ILoadAssetHandler)` | 更新/ロード前後の外部フック登録 |
| `void SetAssetBundleFileStreamFactory(AssetBundleFileStreamFactory factory)` | 復号ストリーム差し替え（Clientは AES-CTR 実装を登録） |
| `void SetAssetBundleInstallerCount(uint)` / `SetFileAssetInstallerCount(uint)` / `SetCriInstallerCount(uint)` | 同時DL数変更（デフォルト各8） |
| `Observable<AssetInfo> OnTimeOutAsObservable()` / `Observable<Exception> OnErrorAsObservable()` | タイムアウト / エラー通知（購読が無い場合はLogError） |
| `Observable<string> OnUpdateAssetAsObservable()` / `OnLoadAssetAsObservable()` / `OnUnloadAssetAsObservable()` | 更新/ロード/解放の resourcePath 通知 |
| `Observable<Unit> OnReleaseManagedAssetsAsObservable()` | `DeleteAllCache` 前の解放要求（Clientはここで SpriteAtlasCache 等を解放） |
| `Observable<string> OnLoadAssetBundleAsObservable()` | AssetBundle実ロード時のFilePath通知 |
| `bool SimulateMode { get }` / `bool LocalMode { get }` / `string InstallDirectory { get }` / `bool LogEnable { get; set }` | 状態プロパティ |

### AssetInfo / AssetBundleInfo / AssetInfoManifest

| メンバー | 説明 |
|---|---|
| `AssetInfo.ResourcePath` / `Group` / `Labels` / `FileName` / `Size` / `CRC` / `Hash` / `AssetBundle` / `IsAssetBundle` | アセット1件のメタ情報（Sizeはbyte、HashはSHA256、FileNameは難読化済み配信名） |
| `AssetInfoManifest.VersionHash` | rootHash（全アセットハッシュから合成。ビルド毎に変化） |
| `AssetInfoManifest.GetAssetInfos(string group = null)` / `GetAssetInfo(resourcePath)` | 一覧/単件取得（内部キャッシュ `BuildCache()` 付き） |
| `AssetInfoManifest.GetManifestAssetInfo()`（static） | マニフェスト自身のAssetInfo（マニフェスト自体もAssetBundle配信） |
| `const string ManifestFileName = "AssetInfoManifest.asset"` / `UndefinedAssetGroup = "(undefined)"` | 定数 |

### AssetBundleManager（`Modules.AssetBundles`。通常は ExternalAsset 経由で触らない）

| メンバー | 説明 |
|---|---|
| `void Initialize(bool simulateMode = false)` | 初期化（アプリ終了時に全Stream解放の購読も行う） |
| `UniTask<T> LoadAsset<T>(installPath, assetInfo, assetPath, autoUnLoad = true, cancelToken)` | 依存解決+参照カウント+`AssetBundle.LoadFromStreamAsync`（復号ストリーム経由）でロード |
| `UniTask UpdateAssetBundle(installPath, assetInfo, progress, cancelToken)` / `UpdateAssetInfoManifest(installPath, cancelToken)` | DL実行（.tmp→リネーム、同一対象は Observable.Share で合流、リトライ5回） |
| `void UnloadAsset(assetBundleName, unloadAllLoadedObjects = false, force = false)` / `UnloadAllAsset(bool)` | 参照カウント減算→0でUnload+Stream解放 / 全強制解放 |
| `string[] GetAllDependencies(assetBundleName)` / `GetAllDependenciesAndSelf(assetBundleName)` | 再帰依存一覧 |
| `Tuple<string, int>[] GetLoadedAssetBundleNames()` | ロード済み一覧（参照カウント付き） |
| `string GetFilePath(installPath, assetInfo)` | キャッシュパス（`{Hash先頭32}.package`） |
| `static void ForceDeleteFile(string filePath)` | ReadOnly解除して削除 |
| `void SetMaxDownloadCount(uint)` / `SetUrl` / `SetManifest` / `SetFileStreamFactory` / `SetSimulateMode` / `SetLocalMode` | 設定系 |
| `bool DeleteOnLoadError { get; set; } = true` | ロード失敗時にキャッシュ削除して次回再DL |
| `const string PackageExtension = ".package"` / `TempPackageExtension = ".tmp"` | 拡張子定数 |
| `OnLoadAsObservable()` / `OnTimeOutAsObservable()` / `OnErrorAsObservable()` | イベント |

### その他ランタイム

| メンバー | 説明 |
|---|---|
| `ExternalAssetFileNameManager.BuildHashedFileName(assetInfo, fixedExtension = null)` | `Hash先頭32文字 + 拡張子` のキャッシュ名構築（ハッシュ未設定ならFileNameのまま） |
| `ExternalAssetFileNameManager.BuildPairedHashedFileName(assetInfo)` | ResourcePathベースのペア名構築（CRIのACB/AWBペア用） |
| `ExternalAssetSceneUtility.LoadScene(string loadPath, LoadSceneMode mode = Additive)` → `UniTask<string[]>` | シーンAssetBundleロード（戻り値のscene配列を `UnLoadScene(scenes)` に渡して解放）。SimulateMode対応済み |
| `ExternalAssetSceneUtility.OnLoadSceneAsObservable()` | シーンロード後の非同期フック（`SceneLoadAsyncHandler`） |
| `FileAssetManager.UpdateFileAsset(installPath, assetInfo, progress, cancelToken)` | 生ファイルDL（ExternalAsset.UpdateAsset から自動振り分け） |
| `AssetBundleStreamCipher.DeriveSeed(byte[] masterSeed, string identifier)` | SHA256でシード派生（Clientのnonce派生で使用） |

### エディタ専用（主要のみ）

| メンバー | 説明 |
|---|---|
| `AssetInfoManifestGenerator.Generate()` → `UniTask<AssetInfoManifest>` | マニフェスト再生成（AssetBundle名の適用・未定義名の掃除込み） |
| `BuildManager.Build(exportPath, assetInfoManifest, openExportFolder = true)` → `UniTask<string>`(versionHash) | フルビルド（CRI生成→FileAssetコピー→SBPビルド→パッケージ化→マニフェスト書き込み→再ビルド→RootHash.txt） |
| `BuildManager.GetExportPath()` | `ExternalAssetConfig.ExportDirectory/Contents/{Platform}/` |
| `BuildManager.BundlePipeline { get; set; }` / `FileStreamFactory { get; set; }` | ビルドパイプライン/暗号化の差し替え点（Clientは `InitializeAssetBundleFileStream` が `[InitializeOnLoadMethod]` で設定） |
| `BuildManager.OnPreBuildAsObservable()` | ビルド前フック（Clientは暗号鍵ファイルロードに使用） |
| `BuildManager.AssetDependenciesValidate(manifest)` / `ValidateDependencies(dependencies)` | 外部参照違反検査 |
| `AssetManagement.Instance.Initialize()` → `GetAllAssetInfos()` / `GetAssetInfo(assetPath)` / `GetAssetLoadPath(assetPath)` | 管理情報→AssetInfo変換（要 `ProjectResourceFolders`） |
| `AssetManagement.ApplyAllAssetBundleName(force = false)` / `SetAssetBundleName(assetPath, name)` | AssetImporter へのアセットバンドル名適用 |
| `AssetManagement.GetIgnoreType(assetPath)` → `IgnoreType?` | 除外判定（IgnoreManage / IgnoreAssetBundle / IgnoreFolder / IgnoreExtension） |
| `ExternalAssetS3Uploader.Upload(S3Uploader uploader)` → `UniTask<bool>` | ビルド成果物をS3へ差分アップロード |
| `ExternalAsset.Prefs.isSimulate` | SimulateモードON/OFF（ProjectPrefs） |
| `BuildAssetBundle.GetAssetBundleOutputPath()`（static） | SBP中間出力先 `Library/AssetBundleBuildCache/{Platform}/` |

## 注意点・罠

- **初期化順序厳守**: `Initialize` → `SetInstallDirectory` → `SetAssetBundleFileStreamFactory` → `SetUrl` → `UpdateManifest` → 以後 `LoadAsset` 可。`UpdateManifest` 前に `LoadAsset` すると "AssetInfoManifest is null." エラー（null返却）。Clientでは `InitializeObject` と `ContentsUpdateManager.FetchInitalData` が実施済みなので、通常の機能実装でこのフローを書くことはない。
- **LoadAsset は throw しない**: 失敗時は null を返し `OnErrorAsObservable` に流れる（Clientの購読でタイトル画面へ強制遷移する）。呼び出し側での null チェックは「エラー時に処理を進めない」ためのガードとして書く。
- **resourcePath は拡張子付き**（`.spriteatlasv2` / `.png` / `.asset` 等）。`Assets/Resource (External)/` からの相対パスで、大文字小文字も一致必須。存在しないパスは `AssetInfoNotFoundException`（→OnError）。パスに迷ったら `Extension > ExternalAsset > Open AssetNavigationWindow` でアセット選択して LoadPath を確認できる。
- **autoUnload のデフォルトは true**: ロード完了直後に参照カウントを戻す（`Unload(false)` なのでロード済みオブジェクト自体は残る）。SpriteAtlas 等を長期保持して後から `GetSprite` する場合や明示解放したい場合は `autoUnload: false` + 使用後 `UnloadAssetBundle`。`UnloadAssetBundle(path, unloadAllLoadedObjects: true)` は使用中の参照が壊れるため原則使わない。
- **SimulateMode はエディタ専用**（メニュー `Extension > ExternalAsset > Simulate Mode`）。AssetDatabase から直接ロードするため、DL・キャッシュ・暗号化・更新判定が一切通らない（`IsRequireUpdate` は常に false、`GetRequireUpdateAssetInfos` は空配列、`UnloadAsset` は無効）。DL/復号の実挙動確認は Simulate OFF + アセットビルドが必要。
- **CRI系は本プロジェクトでは無効**: `ExternalAsset.cri.cs`（`GetCueInfo` / `GetMovieInfo`）は `ENABLE_CRIWARE_ADX/ADX_LE/SOFDEC` シンボル定義時のみコンパイルされるが、Dominion の ProjectSettings に未定義。サウンドは `AudioAssetManager`（`Client/Assets/Scripts/Client/Core/Sound/`）が `ExternalAsset.LoadAsset<Object>` で AudioClip / IntroloopAudio をロードする方式。
- **シーンは LoadAsset 不可**: `ExternalAssetSceneUtility.LoadScene` を使う（内部で `LoadAsset<AssetBundle>` → `SceneManager.LoadSceneAsync` → ロード後 `UnloadAssetBundle`）。
- **更新判定はファイル存在のみ**: キャッシュ名 = `Hash先頭32文字+拡張子` なので中身のハッシュ再計算はしない。壊れたキャッシュはロード失敗時に `DeleteOnLoadError = true` で自動削除→次回再DL。不要キャッシュは `UpdateManifest` 後に1日1回自動削除（`Prefs.deleteUnUsedCacheTime`）。
- **アセット追加時はエディタ登録が必要**: `Resource (External)` に置いただけでは配信されない。`ManageWindow` でグループ登録（→ `ManagedAssets.asset` 変更 → マニフェスト自動再生成）。マニフェストに載っていないパスのロードは `AssetInfoNotFoundException`。
- **外部参照違反**: 外部アセットが `Resource (External)` / `Resource (Share)`（+検査除外）以外のアセットを参照するとビルド時に警告され `InvalidDependantWindow` に表示される（そのままだと参照アセットがAssetBundleに重複格納される）。
- **同一アセットへの並行呼び出しは安全**: DL・ロードとも実行中タスクを共有（`Observable.Share` / `updateQueueing` 待機）するので、複数箇所から同時に `LoadAsset` してよい。`UpdateAsset` は1フレーム150呼び出しに制限（`FunctionFrameLimiter`）。
- **イベントは R3**（UniRxではない）: 戻り値は `Observable<T>`。購読は `.Subscribe(...).AddTo(...)`。
- **ロード済みAssetBundleのリーク検知**: Client はシーン離脱時に `ExternalAsset.GetLoadedAssets()` で未解放を警告する仕組みあり（`InitializeObject.devkit.cs` の `RegisterAssetbundleWarning`）。`autoUnload: false` でロードしたら解放漏れに注意。
- **`ShaderReApply`** は例外的に `Start()` を使う既存MonoBehaviour（AssetBundle由来マテリアルのシェーダー再適用）。新規コードの参考にしない。
- エディタの `AssetInfoManifestAutoUpdater` は `Resource (External)` 配下の変更で自動再生成をリクエストする。バッチモード・再生中・コンパイル中・ビルド中はスキップされる。

## 関連

- [Master](Master.md) — 同じ「rootHash + S3 + PlayFab TitleData」方式のマスターデータ配信基盤。`ContentsUpdateManager`（Client）が本モジュールとマスター更新を統合制御
- Network（`Modules.Net`、ドキュメント未作成） — `DownloadRequest` / `FileDownLoader` / `NetworkConnection.WaitNetworkReachable` を下層で使用
- Sound / CriWare（ドキュメント未作成） — `CueInfo` / `CriAssetManager` 連携（CRI有効時のみ。本プロジェクトは無効）
- Performance（ドキュメント未作成） — `FunctionFrameLimiter`（UpdateAsset の呼び出し制限）
- Devkit（ドキュメント未作成） — `UnityConsole`（ロード/DLのログ出力先）、`ProjectResourceFolders`（`ExternalAssetPath` / `ShareResourcesPath` の定義元）、`SimulationModeAssetFileTracker`（Simulate時の使用アセット追跡）

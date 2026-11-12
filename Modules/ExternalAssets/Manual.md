# ExternalAssets

配信アセットを管理する機能。
アセットバンドルや生ファイルのダウンロード、キャッシュ、バージョン判定、ロード、解放ができる。
CRI アセットの扱いは CRIWARE 用のシンボルを定義したときだけ使える。

## 基本情報

> **namespace**: `Modules.ExternalAssets`（AssetBundle低層・暗号化ストリームは `Modules.AssetBundles`、AssetBundleビルドは `Modules.AssetBundles.Editor`）
> **場所**: `Modules/ExternalAssets/`
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`, `PathUtility`, `SecurePrefs` 等） / Modules.Network（WebRequest / WebDownload） / Modules.R3Extension / Modules.Devkit.Console / Modules.Performance / Modules.ApplicationEvent / エディタ側: Modules.Devkit.Project・Modules.Devkit.Prefs・Scriptable Build Pipeline / 条件付き: Modules.CriWare・Modules.Sound・Modules.Movie（`ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` 定義時のみ）、Modules.Amazon.S3（`ENABLE_AMAZON_WEB_SERVICE` 定義時のみ）

## 概要

配信アセット（AssetBundle / 生ファイル / CRIアセット）のダウンロード・キャッシュ・バージョン管理・ロード・解放を一元管理する基盤。窓口は `ExternalAsset`（`Singleton<ExternalAsset>`、機能別 partial）。
アセット一覧は `AssetInfoManifest`（ScriptableObject）で管理し、**ローカルキャッシュのファイル名にコンテンツハッシュ（`AssetInfo.Hash` の先頭32文字）を埋め込む**ことで、更新要否判定を `File.Exists` だけで完結させる設計。
アセットをロードする時は原則 `await ExternalAsset.LoadAsset<T>(resourcePath)` の1行（未DLなら自動DL→ロードまで面倒を見る）。

主要クラス: `ExternalAsset`（窓口）/ `AssetInfoManifest`（全 `AssetInfo` + `VersionHash`(rootHash)。それ自体もAssetBundleとして配信され毎回DLされる）/ `AssetBundleManager`（`Modules.AssetBundles`。DL・依存解決・参照カウント・ロード。DLは1試行60秒で打ち切り、最大5回まで2秒間隔でリトライする。ロードは1試行10秒で、タイムアウトと `FileLoadException` を同じ回数・間隔でリトライする）/ `FileAssetManager`（非AssetBundle生ファイルのDL。実DLを担う `FileAssetDownLoader` が一時ファイル(.tmp)→リネームで原子的に書き込む。リトライは `FileDownLoader` の既定(3回/2秒間隔)、1試行のタイムアウトは `DownloadRequest` の既定(30秒)）/ `AssetBundleFileStream`（パッケージ復号ストリーム基底。`Transform` を実装して変換方式を決める。既定はビット反転の `DefaultAssetBundleFileStream`。AES-CTR は abstract の `AesCtrAssetBundleFileStream` を継承し、鍵と AssetBundle ごとの nonce(8byte) を渡す。XOR・シード派生は `AssetBundleStreamCipher`）/ `ExternalAssetFileNameManager`（キャッシュのファイル名を作る）/ `AssetFileValidator`（サイズ・CRC の照合）。
エディタ側: `ManageWindow`（グループ登録 → `ManagedAssets.asset`）→ `AssetInfoManifestGenerator`（マニフェスト生成）→ `BuildManager`（ビルド統括。出力先・除外設定は `ExternalAssetConfig`、出力先は `{ExportDirectory}/Contents/{Platform}/`、SBP中間出力は `Library/AssetBundleBuildCache/{Platform}`）→ `ExternalAssetS3Uploader`（差分アップロード）。ビルドパイプライン/暗号化の差し替え点は `BuildManager.BundlePipeline`（`IBuildAssetBundlePipeline`）/ `BuildManager.FileStreamFactory`（利用側で `[InitializeOnLoadMethod]` で設定するのが定石）。ビルド前に鍵のロードなどを差し込むときは `BuildManager.OnPreBuildAsObservable()`（`PreBuildAsyncHandler` で完了を待たせる）。

### データの流れ（全体像）

```
[エディタ] Assets/Resource (External)/ にアセット配置
    → ManageWindow でグループ登録 (ManagedAssets.asset / ManageInfo)
    → AssetInfoManifest 生成（Postprocessor による自動再生成あり）
[ビルド]  BuildManager.Build（メニュー or CI）
    → SBPでAssetBundleビルド(LZ4) → .package化(ストリーム暗号化) → RootHash.txt出力
    → ExternalAssetS3Uploader → 配信ストレージ / rootHash を利用側で管理配布
[実行時]  Initialize → SetUrl(remoteUrl, rootHash) → UpdateManifest()
    → LoadAsset<T>(resourcePath)（未DL分は自動DL）
    URL(AssetBundle): {remoteUrl}/{Platform}/{rootHash}/{FileName}.package?v={Hash}
    URL(生ファイル):   {remoteUrl}/{Platform}/{rootHash}/{FileName}?v={Hash}
    キャッシュ(AssetBundle): {InstallDirectory}/{Hash先頭32文字}.package
    キャッシュ(生ファイル):   {InstallDirectory}/{Hash先頭32文字}{拡張子}
    ※ InstallDirectory = {SetInstallDirectory で渡したパス}/Contents（既定は persistentDataPath）
    ※ Hash を持たないマニフェスト自身は AssetInfoManifest.package の名前で保存される
```

- **resourcePath**（ロードパス）= `Assets/Resource (External)/` からの相対パス・**拡張子付き**。例: `"Contents/<カテゴリ>/<アセット名>.spriteatlasv2"`
- `Share:` プレフィックス付きは `Assets/Resource (Share)/` 配下（共有アセット。`ExternalAsset.ShareGroupPrefix`）
- リモートファイル名（`AssetInfo.FileName`）は、AssetBundle はアセットバンドル名、生ファイルは拡張子を除いた resourcePath のハッシュで難読化する（生ファイルは拡張子を残す。`AssetManagement.SetCryptoFileName`）

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
| AssetBundle暗号化方式の差し替え | `Instance.SetAssetBundleFileStreamFactory(factory)` |
| CRIサウンドのCueInfo取得 | `await ExternalAsset.GetCueInfo(resourcePath, cue)`（`ENABLE_CRIWARE_ADX*` 定義時のみ） |
| CRIムービー情報取得 | `await ExternalAsset.GetMovieInfo(resourcePath)`（`ENABLE_CRIWARE_SOFDEC` 定義時のみ） |
| DL/ロードに外部処理を差し込む | `Instance.SetUpdateAssetHandler(IUpdateAssetHandler)` / `SetLoadAssetHandler(ILoadAssetHandler)` |
| 更新・ロード・解放の完了を購読 | `Instance.OnUpdateAssetAsObservable()` / `OnLoadAssetAsObservable()` / `OnUnloadAssetAsObservable()`（resourcePath が流れる） |
| 同時ダウンロード数を変える | `Instance.SetAssetBundleInstallerCount(n)` / `SetFileAssetInstallerCount(n)`（既定はどちらも8） |
| 同梱アセットだけで動かす（DLしない） | `Instance.SetLocalMode(true)` |
| キャッシュの実ファイルパス取得 | `Instance.GetFilePath(assetInfo)` |
| シーンのロード完了に処理を差し込む | `ExternalAssetSceneUtility.OnLoadSceneAsObservable()`（`SceneLoadAsyncHandler` で完了を待たせる） |
| 【Editor】Simulateモード切替 | メニュー `Extension > ExternalAsset > Simulate Mode`（`ExternalAsset.Prefs.isSimulate`） |
| 【Editor】アセットのグループ登録・管理 | メニュー `Extension > ExternalAsset > Open AssetManageWindow` |
| 【Editor】アセットのロードパス確認 | メニュー `Extension > ExternalAsset > Open AssetNavigationWindow` |
| 【Editor】マニフェスト手動生成 | メニュー `Extension > ExternalAsset > Generate AssetInfoManifest` |
| 【Editor】外部アセットビルド | メニュー `Extension > ExternalAsset > Build ExternalAssets`（= `BuildExternalAssets.Execute()`。確認ダイアログあり）/ `BuildManager.Build(exportPath, manifest, openExportFolder)`（戻り値は rootHash。失敗時は null） |
| 【Editor】S3アップロード | `ExternalAssetS3Uploader.Upload(uploader)`（利用側で抽象クラス `S3Uploader` 派生を実装。`ENABLE_AMAZON_WEB_SERVICE` 定義時のみ） |

## 使い方

定型パターン:

- **ロード基本形（最頻出。パス直指定・型指定）**: `var atlas = await ExternalAsset.LoadAsset<SpriteAtlas>(loadPath);`（loadPath は拡張子付き resourcePath）（SpriteAtlas は `SpriteAtlasCache` と併用）
- **autoUnload: false + 明示解放**: `LoadAsset<SpriteAtlas>(loadPath, false)` → Sprite取得後に `ExternalAsset.UnloadAssetBundle(loadPath)`
- **事前ダウンロード（進捗付き並列DL）**: `GetGroupAssetInfos(group)` → `IsRequireUpdate` でDLが必要なアセットのみに絞り込み → 各 `ExternalAsset.UpdateAsset(resourcePath, progress)` を `UniTask.WhenAll`（進捗は `IProgress<DownloadProgressInfo>` で受けて合算）
- **起動時の一括更新**: `GetRequireUpdateAssetInfos()`（全グループの要更新一覧）→ グループフィルタ後に順次 `ExternalAsset.UpdateAsset(assetInfo.ResourcePath, cancelToken: cancelToken)`
- **初期化（起動時1回）**: `Initialize(externalAssetDirectory, shareAssetDirectory)`（保存先は `Application.persistentDataPath` に設定される）→ 保存先を変えるときだけ `SetInstallDirectory(directory)` → `SetAssetBundleFileStreamFactory`（利用側の復号ストリーム）→ `OnErrorAsObservable` / `OnTimeOutAsObservable` 購読
- **起動フロー: URL設定 → マニフェスト更新**: `SetUrl(remoteUrl, rootHash)` → `UpdateManifest()`（マスター更新等と `UniTask.WhenAll` で並列実行）

## 注意点・罠

- **初期化順序厳守**: `Initialize` →（必要なら `SetInstallDirectory`）→ `SetAssetBundleFileStreamFactory` → `SetUrl` → `UpdateManifest` → 以後 `LoadAsset` 可。`UpdateManifest` 前に `LoadAsset` すると "AssetInfoManifest is null." エラー（null返却）。`UpdateManifest` は成否を bool で返す（失敗時は例外をログに出して false）。`SetAssetBundleFileStreamFactory` を呼ばない場合はビット反転の `DefaultAssetBundleFileStream` で読む。
- **LoadAsset は throw しない**: 失敗時は null を返し `OnErrorAsObservable` に流れる（利用側の購読で共通エラーハンドリングを行う）。呼び出し側での null チェックは「エラー時に処理を進めない」ためのガードとして書く。`T : UnityEngine.Object` で、Component 派生型を指定すると GameObject 経由で GetComponent される。
- **resourcePath は拡張子付き**（`.spriteatlasv2` / `.png` / `.asset` 等）。`Assets/Resource (External)/` からの相対パスで、大文字小文字も一致必須。存在しないパスは `AssetInfoNotFoundException`（→OnError）。パスに迷ったら `Extension > ExternalAsset > Open AssetNavigationWindow` でアセット選択して LoadPath を確認できる。
- **autoUnload のデフォルトは true**: ロード完了直後に参照カウントを戻す（`Unload(false)` なのでロード済みオブジェクト自体は残る）。SpriteAtlas 等を長期保持して後から `GetSprite` する場合や明示解放したい場合は `autoUnload: false` + 使用後 `UnloadAssetBundle`。`UnloadAssetBundle(path, unloadAllLoadedObjects: true)` は使用中の参照が壊れるため原則使わない。
- **SimulateMode はエディタ専用**（メニュー `Extension > ExternalAsset > Simulate Mode`）。AssetDatabase から直接ロードするため、DL・キャッシュ・暗号化・更新判定が一切通らない（`IsRequireUpdate` は常に false、`GetRequireUpdateAssetInfos` は空配列、`UnloadAsset` は無効）。DL/復号の実挙動確認は Simulate OFF + アセットビルドが必要。
- **CRI系はシンボル定義時のみ有効**: `ExternalAsset.cri.cs`（`GetCueInfo` / `GetMovieInfo`）は `ENABLE_CRIWARE_ADX/ADX_LE/SOFDEC` シンボル定義時のみコンパイルされる。未定義時は縮退し、通常のサウンド/ムービーは `ExternalAsset.LoadAsset<Object>` で AudioClip 等をロードする方式になる。
- **シーンは LoadAsset 不可**: `ExternalAssetSceneUtility.LoadScene` を使う（内部で `LoadAsset<AssetBundle>` → `SceneManager.LoadSceneAsync` → ロード後 `UnloadAssetBundle`）。
- **DL失敗の通知は最終失敗の1回だけ**: リトライ中の失敗はコンソールに警告を出すだけで `OnErrorAsObservable` / `OnTimeOutAsObservable` に流れない。AssetBundle は最後の試行がタイムアウトなら `OnTimeOutAsObservable`、それ以外は `OnErrorAsObservable`。FileAsset は失敗を例外で送出し `OnErrorAsObservable` に流れる。
- **AssetBundle はリネーム前にサイズ・CRCを照合する**: `AssetInfo.Size` / `AssetInfo.CRC`（ビルド時に `.package` から算出）と一時ファイルを `AssetFileValidator` で照合し、不一致なら一時ファイルを消してリトライする。値を持たない `AssetInfo`（マニフェスト自身）は照合しない。FileAsset は照合しない。
- **rootHash（`AssetInfoManifest.VersionHash`）は各 `AssetInfo` の Hash・CRC・サイズから計算する**: キャッシュのファイル名に使う `AssetInfo.Hash` は、AssetBundle では SBP のコンテンツハッシュ由来でシリアライズヘッダー（Unity バージョン）と圧縮設定を含まない。Unity の更新などで `.package` のバイトだけが変わった場合は rootHash だけが変わり、配信ストレージには新しいフォルダとして全ファイルが上がる。端末のキャッシュはファイル名が変わらないのでそのまま使われる。1 つの rootHash のフォルダには常に同じバイトのファイルが置かれるので、マニフェストのサイズ・CRC と配信ファイルが一致する（ダウンロード時の照合の前提）。
- **更新判定はファイル存在のみ**: キャッシュ名 = `Hash先頭32文字+拡張子` なので中身のハッシュ再計算はしない。壊れたキャッシュはロード失敗時に `AssetBundleManager.DeleteOnLoadError = true`（既定）で自動削除→次回再DL。不要キャッシュ（マニフェストに無いファイル。中断で残った `.tmp` を含む）は `UpdateManifest` 後に1日1回自動削除（`Prefs.deleteUnUsedCacheTime`）。`DeleteAllCache` は削除前に `OnReleaseManagedAssetsAsObservable` が発火し、続けて全 AssetBundle を解放する（利用側はここで SpriteAtlasCache 等を解放）。
- **LocalMode はダウンロードもキャッシュ削除もしない**: `SetLocalMode(true)` のあいだ、`UpdateAsset` / `UpdateManifest` はDLせず、`DeleteAllCache` / `DeleteCache` / 不要キャッシュの自動削除は何もしない。保存先に無いアセットを要求するとコンソールにエラーを出す。保存先を StreamingAssets 配下にすると `Contents/{Platform}` を見る。
- **アセット追加時はエディタ登録が必要**: `Resource (External)` に置いただけでは配信されない。`ManageWindow` でグループ登録（→ `ManagedAssets.asset` 変更 → マニフェスト再生成）。マニフェストに載っていないパスのロードは `AssetInfoNotFoundException`。
- **外部参照違反**: 外部アセットが外部アセットフォルダ（`Resource (External)`）以外のアセットを参照していると、`BuildExternalAssets.Execute()` が警告ダイアログ（force build / show detail）を出し、show detail で `InvalidDependantWindow` に表示される（そのままだと参照アセットがAssetBundleに重複格納される）。`.cs` と `ExternalAssetConfig` の検査除外対象（`IgnoreValidateTarget`）は検査しない。共有アセットフォルダ（`Resource (Share)`）への参照も、検査除外対象に入れていなければ違反になる。`BuildManager.Build` を直接呼ぶ場合、この検査は走らない（`BuildManager.AssetDependenciesValidate` を自分で呼ぶ）。
- **同一アセットへの並行呼び出しは安全**: DL・ロードとも実行中タスクを共有（`Observable.Share` / `updateQueueing` 待機）するので、複数箇所から同時に `LoadAsset` してよい。`UpdateAsset` は1フレーム150呼び出しに制限（`FunctionFrameLimiter`）。
- **イベントは R3**（UniRxではない）: 戻り値は `Observable<T>`。購読は `.Subscribe(...).AddTo(...)`。
- **ロード済みAssetBundleのリーク検知**: シーン離脱時に `ExternalAsset.GetLoadedAssets()` で未解放を警告する仕組みを利用側で組める。`autoUnload: false` でロードしたら解放漏れに注意。
- エディタでは `ManifestUpdateAssetPostprocessor` が `Resource (External)` 配下のアセットの追加・削除・移動を検知して再生成をリクエストし、`AssetInfoManifestAutoUpdater` が1秒間隔でリクエストを見て再生成する。エディタが非フォーカス・再生中・コンパイル中・アセットビルド中は再生成を見送る。バッチモードでは動かない。`AssetInfoManifestAutoUpdater.Instance.Enable` で無効にできる。

## 関連

- [Master](../Master/Manual.md) — 同じ「rootHash + 配信ストレージ + TitleData」方式のマスターデータ配信基盤。利用側で本モジュールとマスター更新を統合制御する
- [Network](../Network/Manual.md)（`Modules.Network`） — `DownloadRequest` / `FileDownLoader` / `NetworkConnection.WaitNetworkReachable` を下層で使用
- [Sound](../Sound/Manual.md) / [CriWare](../CriWare/Manual.md) — `CueInfo` / `CriAssetManager` 連携（CRI有効時のみ）
- [R3Extension](../R3Extension/Manual.md) — `ObservableEx.FromUniTask`（DL・ロードのタスク共有）
- [Performance](../Performance/Manual.md) — `FunctionFrameLimiter`（UpdateAsset の呼び出し制限）
- [Devkit](../Devkit/Manual.md) — `UnityConsole`（ロード/DLのログ出力先）、`ProjectResourceFolders`（`ExternalAssetPath` / `ShareResourcesPath` の定義元）、`SimulationModeAssetFileTracker`（Simulate時の使用アセット追跡）

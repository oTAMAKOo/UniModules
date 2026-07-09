# ExternalAsset

> **namespace**: `Modules.ExternalAssets`（AssetBundle低層・暗号化ストリームは `Modules.AssetBundles`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ExternalAsset/`
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`, PathUtility 等） / Modules.Net（WebRequest / WebDownload） / Modules.Devkit.Console / Modules.Performance / Modules.ApplicationEvent / 条件付き: Modules.CriWare・Modules.Sound・Modules.Movie（`ENABLE_CRIWARE_*` 定義時のみ）、Modules.Amazon.S3（`ENABLE_AMAZON_WEB_SERVICE` 定義時のみ）

## 概要

配信アセット（AssetBundle / 生ファイル / CRIアセット）のダウンロード・キャッシュ・バージョン管理・ロード・解放を一元管理する基盤。窓口は `ExternalAsset`（`Singleton<ExternalAsset>`、機能別 partial）。
アセット一覧は `AssetInfoManifest`（ScriptableObject）で管理し、**ローカルキャッシュのファイル名にコンテンツハッシュ(SHA256先頭32文字)を埋め込む**ことで、更新要否判定を `File.Exists` だけで完結させる設計。
アセットをロードする時は原則 `await ExternalAsset.LoadAsset<T>(resourcePath)` の1行（未DLなら自動DL→ロードまで面倒を見る）。

主要クラス: `ExternalAsset`（窓口）/ `AssetInfoManifest`（全 `AssetInfo` + `VersionHash`(rootHash)。それ自体もAssetBundleとして配信され毎回DLされる）/ `AssetBundleManager`（`Modules.AssetBundles`。DL・依存解決・参照カウント・ロード。DL/ロードのタイムアウト(60s/10s)+リトライ(5回/2秒間隔)内蔵）/ `FileAssetManager`（非AssetBundle生ファイルのDL。一時ファイル(.tmp)→リネームで原子的に書き込み）/ `AssetBundleFileStream`（パッケージ復号ストリーム基底。利用側で AES-CTR 等の暗号化方式を実装する）。
エディタ側: `ManageWindow`（グループ登録 → `ManagedAssets.asset`）→ `AssetInfoManifestGenerator`（マニフェスト生成）→ `BuildManager`（ビルド統括。出力先・除外設定は `ExternalAssetConfig`、SBP中間出力は `Library/AssetBundleBuildCache/{Platform}`）→ `ExternalAssetS3Uploader`（差分アップロード）。ビルドパイプライン/暗号化の差し替え点は `BuildManager.BundlePipeline` / `FileStreamFactory`（利用側で `[InitializeOnLoadMethod]` で設定するのが定石）。

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
| AssetBundle暗号化方式の差し替え | `Instance.SetAssetBundleFileStreamFactory(factory)` |
| CRIサウンドのCueInfo取得 | `await ExternalAsset.GetCueInfo(resourcePath, cue)`（`ENABLE_CRIWARE_ADX*` 定義時のみ） |
| CRIムービー情報取得 | `await ExternalAsset.GetMovieInfo(resourcePath)`（`ENABLE_CRIWARE_SOFDEC` 定義時のみ） |
| DL/ロードに外部処理を差し込む | `Instance.SetUpdateAssetHandler(IUpdateAssetHandler)` / `SetLoadAssetHandler(ILoadAssetHandler)` |
| 【Editor】Simulateモード切替 | メニュー `Extension > ExternalAsset > Simulate Mode`（`ExternalAsset.Prefs.isSimulate`） |
| 【Editor】アセットのグループ登録・管理 | メニュー `Extension > ExternalAsset > Open AssetManageWindow` |
| 【Editor】アセットのロードパス確認 | メニュー `Extension > ExternalAsset > Open AssetNavigationWindow` |
| 【Editor】マニフェスト手動生成 | メニュー `Extension > ExternalAsset > Generate AssetInfoManifest` |
| 【Editor】外部アセットビルド | `BuildExternalAssets.Execute()` / `BuildManager.Build(exportPath, manifest)` |
| 【Editor】S3アップロード | `ExternalAssetS3Uploader.Upload(uploader)`（利用側で `IUploader` 実装） |

## 使い方

定型パターン:

- **ロード基本形（最頻出。パス直指定・型指定）**: `var atlas = await ExternalAsset.LoadAsset<SpriteAtlas>("Contents/Item/Icon/ItemIconAtlas.spriteatlasv2");`（SpriteAtlas は `SpriteAtlasCache` と併用）
- **autoUnload: false + 明示解放**: `LoadAsset<SpriteAtlas>(loadPath, false)` → Sprite取得後に `ExternalAsset.UnloadAssetBundle(loadPath)`
- **事前ダウンロード（進捗付き並列DL）**: `GetGroupAssetInfos(group)` → `IsRequireUpdate` でDLが必要なアセットのみに絞り込み → 各 `ExternalAsset.UpdateAsset(resourcePath, progress)` を `UniTask.WhenAll`（進捗は `IProgress<DownloadProgressInfo>` で受けて合算）
- **起動時の一括更新**: `GetRequireUpdateAssetInfos()`（全グループの要更新一覧）→ グループフィルタ後に順次 `ExternalAsset.UpdateAsset(assetInfo.ResourcePath, cancelToken: cancelToken)`
- **初期化（起動時1回）**: `Initialize(externalAssetDirectory, shareAssetDirectory)` → `SetInstallDirectory(UnityPathUtility.PersistentDataPath)` → `SetAssetBundleFileStreamFactory`（利用側の復号ストリーム）→ `OnErrorAsObservable` / `OnTimeOutAsObservable` 購読
- **起動フロー: URL設定 → マニフェスト更新**: `SetUrl(remoteUrl, rootHash)` → `UpdateManifest()`（マスター更新等と `UniTask.WhenAll` で並列実行）

## 注意点・罠

- **初期化順序厳守**: `Initialize` → `SetInstallDirectory` → `SetAssetBundleFileStreamFactory` → `SetUrl` → `UpdateManifest` → 以後 `LoadAsset` 可。`UpdateManifest` 前に `LoadAsset` すると "AssetInfoManifest is null." エラー（null返却）。
- **LoadAsset は throw しない**: 失敗時は null を返し `OnErrorAsObservable` に流れる（利用側の購読で共通エラーハンドリングを行う）。呼び出し側での null チェックは「エラー時に処理を進めない」ためのガードとして書く。`T : UnityEngine.Object` で、Component 派生型を指定すると GameObject 経由で GetComponent される。
- **resourcePath は拡張子付き**（`.spriteatlasv2` / `.png` / `.asset` 等）。`Assets/Resource (External)/` からの相対パスで、大文字小文字も一致必須。存在しないパスは `AssetInfoNotFoundException`（→OnError）。パスに迷ったら `Extension > ExternalAsset > Open AssetNavigationWindow` でアセット選択して LoadPath を確認できる。
- **autoUnload のデフォルトは true**: ロード完了直後に参照カウントを戻す（`Unload(false)` なのでロード済みオブジェクト自体は残る）。SpriteAtlas 等を長期保持して後から `GetSprite` する場合や明示解放したい場合は `autoUnload: false` + 使用後 `UnloadAssetBundle`。`UnloadAssetBundle(path, unloadAllLoadedObjects: true)` は使用中の参照が壊れるため原則使わない。
- **SimulateMode はエディタ専用**（メニュー `Extension > ExternalAsset > Simulate Mode`）。AssetDatabase から直接ロードするため、DL・キャッシュ・暗号化・更新判定が一切通らない（`IsRequireUpdate` は常に false、`GetRequireUpdateAssetInfos` は空配列、`UnloadAsset` は無効）。DL/復号の実挙動確認は Simulate OFF + アセットビルドが必要。
- **CRI系はシンボル定義時のみ有効**: `ExternalAsset.cri.cs`（`GetCueInfo` / `GetMovieInfo`）は `ENABLE_CRIWARE_ADX/ADX_LE/SOFDEC` シンボル定義時のみコンパイルされる。未定義時は縮退し、通常のサウンド/ムービーは `ExternalAsset.LoadAsset<Object>` で AudioClip 等をロードする方式になる。
- **シーンは LoadAsset 不可**: `ExternalAssetSceneUtility.LoadScene` を使う（内部で `LoadAsset<AssetBundle>` → `SceneManager.LoadSceneAsync` → ロード後 `UnloadAssetBundle`）。
- **更新判定はファイル存在のみ**: キャッシュ名 = `Hash先頭32文字+拡張子` なので中身のハッシュ再計算はしない。壊れたキャッシュはロード失敗時に `DeleteOnLoadError = true` で自動削除→次回再DL。不要キャッシュは `UpdateManifest` 後に1日1回自動削除（`Prefs.deleteUnUsedCacheTime`）。`DeleteAllCache` は削除前に `OnReleaseManagedAssetsAsObservable` が発火する（利用側はここで SpriteAtlasCache 等を解放）。
- **アセット追加時はエディタ登録が必要**: `Resource (External)` に置いただけでは配信されない。`ManageWindow` でグループ登録（→ `ManagedAssets.asset` 変更 → マニフェスト自動再生成）。マニフェストに載っていないパスのロードは `AssetInfoNotFoundException`。
- **外部参照違反**: 外部アセットが `Resource (External)` / `Resource (Share)`（+検査除外）以外のアセットを参照するとビルド時に警告され `InvalidDependantWindow` に表示される（そのままだと参照アセットがAssetBundleに重複格納される）。
- **同一アセットへの並行呼び出しは安全**: DL・ロードとも実行中タスクを共有（`Observable.Share` / `updateQueueing` 待機）するので、複数箇所から同時に `LoadAsset` してよい。`UpdateAsset` は1フレーム150呼び出しに制限（`FunctionFrameLimiter`）。
- **イベントは R3**（UniRxではない）: 戻り値は `Observable<T>`。購読は `.Subscribe(...).AddTo(...)`。
- **ロード済みAssetBundleのリーク検知**: シーン離脱時に `ExternalAsset.GetLoadedAssets()` で未解放を警告する仕組みを利用側で組める。`autoUnload: false` でロードしたら解放漏れに注意。
- エディタの `AssetInfoManifestAutoUpdater` は `Resource (External)` 配下の変更で自動再生成をリクエストする。バッチモード・再生中・コンパイル中・ビルド中はスキップされる。

## 関連

- [Master](Master.md) — 同じ「rootHash + 配信ストレージ + TitleData」方式のマスターデータ配信基盤。利用側で本モジュールとマスター更新を統合制御する
- [Network](Network.md)（`Modules.Net`） — `DownloadRequest` / `FileDownLoader` / `NetworkConnection.WaitNetworkReachable` を下層で使用
- [Sound](Sound.md) / [CriWare](CriWare.md) — `CueInfo` / `CriAssetManager` 連携（CRI有効時のみ）
- [Performance](Performance.md) — `FunctionFrameLimiter`（UpdateAsset の呼び出し制限）
- [Devkit](Devkit.md) — `UnityConsole`（ロード/DLのログ出力先）、`ProjectResourceFolders`（`ExternalAssetPath` / `ShareResourcesPath` の定義元）、`SimulationModeAssetFileTracker`（Simulate時の使用アセット追跡）

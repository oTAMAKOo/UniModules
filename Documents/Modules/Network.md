# Network

> **namespace**: `Modules.Net`（到達性） / `Modules.Net.WebRequest`（HTTP API通信） / `Modules.Net.WebDownload`（ファイルDL）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Network/`
> **Client側使用**: 直接 約7ファイル（`using Modules.Net*` 4 + 派生クラス利用 3、2026-07時点）。ExternalAsset / Master / CriWare のDL基盤として間接使用は多数
> **依存**: UniTask / R3（UniRxではない） / Extensions（`Singleton<T>`, `LifetimeDisposable`, `PathUtility`, 圧縮・暗号化拡張） / Modules.R3Extension（`ObservableEx.FromUniTask`） / MessagePack / Newtonsoft.Json / Modules.Devkit.Console

## 概要

HTTP通信の基盤モジュール。3系統に分かれる。

1. **NetworkConnection**（`Modules.Net`）: ネットワーク到達性の待機・オフライン通知（static）。
2. **WebDownload系**（`Modules.Net.WebDownload`）: HTTP GET によるファイルダウンロード基盤。**本プロジェクトで実際に使われている主役**。用途は CDN=CloudFront（`Constants.Urls.CDNUrl`）からのマスターデータ/アセットDL、および PlayFab Entity Files のDL。
3. **WebRequest系**（`Modules.Net.WebRequest`）: GET/POST等のREST API通信基盤（リトライ・暗号化・圧縮・MessagePack/Json）。**本プロジェクトでは具象実装が存在せず未使用**（API通信は PlayFab SDK 経由のため）。ただし拡張メソッド `UnityWebRequest.Send()` / `HasError()` と例外型 `UnityWebRequestErrorException` は WebDownload 系・Devkit から使用されている。

Claude が新規にHTTPダウンロード処理を書く場合、`UnityWebRequest` を素で書かず `FileDownLoader<DownloadRequest>` 継承（キュー・リトライ・並列数制御込み）か、単発なら `DownloadRequest` 直接使用を選ぶ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ファイルをHTTP GETでローカル保存（リトライ・並列制御付き） | `FileDownLoader<DownloadRequest>` を継承（実装例: `MasterFileDownLoader` / `PlayFabFileDownLoader`） |
| 単発の小さなファイルDL（キュー・リトライ不要） | `new DownloadRequest()` → `Initialize(url, filePath)` → `await Download(progress, token)` |
| DLの進捗を取りたい | `Download(..., progress)` の `IProgress<float>`（0〜1） |
| 同時ダウンロード数を制御したい | `FileDownLoader.SetMaxDownloadCount(n)`（デフォルト5） |
| DLのリトライ回数・間隔を変えたい | `FileDownLoader.Initialize(retryCount, retryDelaySeconds)` |
| DLタイムアウトを変えたい | `DownloadRequest.TimeOutSeconds`（デフォルト30秒） |
| ネットワーク接続を待つ/確認する | `await NetworkConnection.WaitNetworkReachable(cancelToken)` |
| オフライン発生を購読したい | `NetworkConnection.OnNotReachableAsObservable()` |
| DL中の全リクエストを中止したい | `FileDownLoader.ForceCancelAll()`（protected。継承側で公開） |
| `UnityWebRequest` を直接使う際の await 送信 | `UnityWebRequestExtensions.Send()`（`UniTask<byte[]>` 化） |
| `UnityWebRequest` の成否判定 | `request.IsSuccess()` / `request.HasError()` |
| HTTPエラーの詳細（ステータスコード等）を取りたい | `UnityWebRequestErrorException`（`StatusCode` / `RawErrorMessage` / `ResponseHeaders`） |
| DLデータをストリームでファイル書き込み | `FileDownloadHandler`（`DownloadHandlerScript` 派生。使用例: `AndroidUtility.CopyStreamingToTemporary`） |
| PlayFab以外のREST APIサーバーと通信したい | `UnityWebRequestManager<T,R>` + `UnityWebRequestClient` を継承（**本プロジェクト未使用。まずPlayFab経由を検討し、必要ならユーザーに相談**） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `NetworkConnection` | static | 到達性待機（タイムアウト付き）・オフライン通知Subject |
| `NetworkReachabilityException` | Exception | 到達性待機タイムアウト時に throw |
| `FileDownLoader<TDownloadRequest>` | abstract（`LifetimeDisposable` 継承） | ファイルDL管理。同一URL重複排除・並列数制御・リトライ・エラーハンドリング |
| `DownloadRequest` | class（`IDisposable`） | 1ファイルのGET DL実行体。`DownloadHandlerFile` 使用、失敗時に部分ファイル自動削除 |
| `FileDownloadHandler` | sealed（`DownloadHandlerScript` 派生） | 受信データを `FileStream` に逐次書き込むDLハンドラ |
| `WebRequestManager<TInstance, TWebRequest>` | abstract（`Singleton<TInstance>` 継承） | REST API通信管理。直列キュー・リトライ・イベントフック ※具象なし |
| `UnityWebRequestManager<TInstance, TWebRequest>` | abstract | 上記 + エディタApiTracker連携・APIログ出力 ※具象なし |
| `UnityWebRequestClient` | abstract（`IWebRequestClient` 実装） | `UnityWebRequest` ベースの通信クライアント。AES暗号化・GZip/LZ4圧縮・Json/MessagePack シリアライズ内蔵 ※具象なし |
| `IWebRequestClient` | interface | 通信クライアント抽象（`WebRequestManager` の型制約） |
| `UnityWebRequestExtensions` | static | `Send()`（UniTask化送信） / `IsSuccess()`（2xx判定） / `HasError()` |
| `UnityWebRequestErrorException` | sealed Exception | HTTPエラー詳細（`StatusCode`, `ResponseHeaders`, `Text`, `Request`） |
| `Method` / `DataFormat` / `DataCompressType` | enum | HTTPメソッド / Json・MessagePack / None・GZip・Deflate・MessagePackLZ4 |

エディタ専用クラスは本モジュール内には無い（`Editor/` サブフォルダ無し）。`UnityWebRequestManager` が参照する `ApiTracker` は `Modules.Devkit.ApiMonitor`（UNITY_EDITOR 時のみ）。

## 使い方(実例)

### 1. FileDownLoader 継承クラスの利用（マスターデータDL）

```csharp
// 引用: Client/Assets/Scripts/Client/Master/Core/Master.cs
if (masterFileDownLoader == null)
{
    masterFileDownLoader = new MasterFileDownLoader();

    masterFileDownLoader.Initialize();

    masterFileDownLoader.SetMaxDownloadCount(25);
}

masterFileDownLoader.SetServerUrl(masterManager.DownloadUrl);   // CDN (Urls.MasterUrl)

var url = PathUtility.Combine(rootHash, downloadFileName);      // ServerUrl との相対
var filePath = PathUtility.Combine(masterManager.InstallDirectory, downloadFileName);

var result = await masterFileDownLoader.Download(url, filePath, cancelToken: cancelToken);
```

### 2. FileDownLoader 継承クラスの定義パターン

```csharp
// 引用: Client/Assets/Scripts/Client/Module/Download/MasterFileDownLoader.cs（抜粋）
public sealed class MasterFileDownLoader : FileDownLoader<DownloadRequest>
{
    public async UniTask<bool> Download(string url, string filePath, IProgress<float> progress = null, CancellationToken cancelToken = default)
    {
        var directory = Directory.GetParent(filePath);

        if (!directory.Exists)
        {
            directory.Create();     // 保存先ディレクトリは呼び出し側で作成.
        }

        var downloadRequest = SetupDownloadRequest(url, filePath);

        var result = await Download(downloadRequest, progress, cancelToken);

        return result;
    }

    protected override void OnComplete(DownloadRequest downloadRequest, double totalMilliseconds) { /* 完了通知 */ }

    protected override UniTask<RequestErrorHandle> OnError(DownloadRequest downloadRequest, Exception ex, CancellationToken cancelToken = default)
    {
        // TimeoutException / UnityWebRequestErrorException / その他 で分岐しログ・通知.
        return UniTask.FromResult(RequestErrorHandle.Retry);    // Retry か Cancel を返す.
    }

    protected override void OnRetryLimit(DownloadRequest downloadRequest)
    {
        ForceCancelAll();
        SystemModel.Instance.ForceTransitionToTitle();          // リトライ上限でタイトルへ.
    }
}
```

### 3. フルURLを直接ダウンロード（PlayFab Entity Files）

```csharp
// 引用: Client/Assets/Scripts/PlayFab/Api/DownloadFile.cs
var fileDownLoader = new PlayFabFileDownLoader();

fileDownLoader.Initialize();

fileDownLoader.SetServerUrl(null);  // null にするとフルURLをそのまま使える.

await fileDownLoader.Download(fileMetadata.DownloadUrl, filePath);  // PlayFab発行の署名付きURL.
```

### 4. DownloadRequest 単体使用（一時ファイル→リネームパターン）

```csharp
// 引用: Client/Assets/UniModules/Scripts/Modules/ExternalAsset/AssetBundle/AssetBundleManager.cs
using (var downloadRequest = new DownloadRequest())
{
    downloadRequest.Initialize(url, tempFilePath);  // 一時ファイルに書き出す.

    downloadRequest.TimeOutSeconds = (int)DownloadTimeout.TotalSeconds;

    try
    {
        await downloadRequest.Download(progressReceiver, cancelToken);
    }
    catch (UnityWebRequestErrorException e)
    {
        throw new Exception($"File download error\nURL:{url}\nResponseCode:{e.Request.responseCode}\n\n{e.Request.error}\n");
    }
}

// DL完了後にリネーム.
ForceDeleteFile(filePath);
File.Move(tempFilePath, filePath);
```

### 5. オフライン検知の購読

```csharp
// 引用: Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs
NetworkConnection.OnNotReachableAsObservable()
    .Subscribe(_ => RequestCancel(UpdateResultStatus.NetworkError))
    .AddTo(dispose.Disposable);
```

## API(主要公開メンバー)

### NetworkConnection（static / `Modules.Net`）

| メンバー | 説明 |
|---|---|
| `static UniTask WaitNetworkReachable(CancellationToken)` | `Application.internetReachability` が回復するまで待機。タイムアウトで `NetworkReachabilityException` を throw + `onNotReachable` 発火 |
| `static Observable<Unit> OnNotReachableAsObservable()` | 到達性タイムアウト発生の通知（R3） |
| `static float TimeOutSeconds { get; set; }` | 待機タイムアウト秒。デフォルト5秒 |

### FileDownLoader\<TDownloadRequest\>（abstract / `Modules.Net.WebDownload`）

| メンバー | 説明 |
|---|---|
| `void Initialize(int retryCount = 3, float retryDelaySeconds = 2)` | 初期化。**呼ばないと内部コレクションが null のまま**（二重呼び出しは無視） |
| `void SetServerUrl(string serverUrl)` | ベースURL設定。`null` 可（その場合 `Download` に渡す url がフルURLになる） |
| `void SetMaxDownloadCount(uint maxDownloadCount)` | 同時ダウンロード数上限。デフォルト5 |
| `protected TDownloadRequest SetupDownloadRequest(string url, string filePath)` | `ServerUrl` と url を `PathUtility.Combine` してリクエスト生成 |
| `protected UniTask<bool> Download(TDownloadRequest, IProgress<float> = null, CancellationToken = default)` | DL実行。同一URLは既存タスクに合流（`Share`）。戻り値は成否 |
| `protected void ForceCancelAll()` | DL中・待機中の全リクエスト強制中止 |
| `protected virtual void OnInitialize()` | 初期化フック |
| `protected abstract void OnComplete(TDownloadRequest, double totalMilliseconds)` | 成功時フック（必須実装） |
| `protected abstract UniTask<RequestErrorHandle> OnError(TDownloadRequest, Exception, CancellationToken)` | エラー時フック（必須実装）。`Retry` / `Cancel` を返す |
| `protected abstract void OnRetryLimit(TDownloadRequest)` | リトライ上限到達フック（必須実装） |
| `string ServerUrl` / `uint MaxDownloadCount` / `int RetryCount` / `float RetryDelaySeconds` | 設定値（読み取り） |

### DownloadRequest（`Modules.Net.WebDownload`）

| メンバー | 説明 |
|---|---|
| `virtual void Initialize(string url, string filePath)` | URL と保存先を設定 |
| `virtual UniTask Download(IProgress<float> = null, CancellationToken = default)` | GET実行。`DownloadHandlerFile` で直接ファイル書き込み。失敗時 `UnityWebRequestErrorException` を throw し部分ファイルを削除 |
| `void Cancel()` | 中断要求（次フレームで `Abort`） |
| `int TimeOutSeconds { get; set; }` | タイムアウト秒。デフォルト30秒 |
| `string Url` / `string FilePath` | リクエストURL / 保存先 |
| `void Dispose()` | 破棄（`using` 推奨） |

### FileDownloadHandler（`Modules.Net.WebDownload`）

| メンバー | 説明 |
|---|---|
| `FileDownloadHandler(string path, byte[] buffer)` | 保存先パスと受信バッファを指定して生成。`webRequest.downloadHandler` に設定して使う |

### WebRequestManager\<TInstance, TWebRequest\>（abstract Singleton / `Modules.Net.WebRequest`）※具象なし

| メンバー | 説明 |
|---|---|
| `virtual void SetHostUrl(string)` / `virtual void SetFormat(DataFormat)` | 接続先・データ形式（デフォルト MessagePack） |
| `virtual void SetRetryCount(int)` / `virtual void SetRetryDelaySeconds(int)` | リトライ設定（デフォルト 3回 / 2秒） |
| `void SetRequestDataCompress(DataCompressType)` / `void SetResponseDataCompress(DataCompressType)` | 送受信圧縮（デフォルト GZip） |
| `protected Task<TResult> Get<TResult>(webRequest, bool parallel = false, IProgress<float> = null)` | GET。`parallel: false` は直列キュー実行 |
| `protected Task<TResult> Post<TResult, TContent>(webRequest, content, ...)` | POST（`Put` / `Patch` / `Delete` も同形） |
| `protected TWebRequest SetupWebRequest(string url, IDictionary<string, object> urlParams)` | `HostUrl` 結合・ヘッダー/URLパラメータ引き継ぎでリクエスト生成 |
| `void CancelAll()` | 通信中の全リクエスト中止 |
| `protected virtual Task<RequestErrorHandle> WaitErrorHandling(TWebRequest)` | エラー処理。デフォルト: `NetworkReachabilityException` → `Exit`、他 → `Retry` |
| `protected virtual void OnStart / OnComplete / OnRetry / OnRetryLimit / OnError / OnCancel` | 各種フック |
| `string HostUrl` / `DataFormat Format` / `int RetryCount` / `IDictionary<string, Tuple<bool, string>> Headers` 他 | 設定値 |

### UnityWebRequestManager\<TInstance, TWebRequest\>（abstract / `Modules.Net.WebRequest`）※具象なし

| メンバー | 説明 |
|---|---|
| `bool LogEnable { get; protected set; }` | true で成功時に URL/Header/Body/Result を `UnityConsole.Event("API")` 出力 |
| （その他は `WebRequestManager` を継承） | UNITY_EDITOR 時は `ApiTracker`（`Modules.Devkit.ApiMonitor`）へ通信履歴を自動記録 |

### UnityWebRequestClient（abstract / `Modules.Net.WebRequest`）※具象なし

| メンバー | 説明 |
|---|---|
| `virtual void Initialize(string hostUrl, DataFormat format = DataFormat.MessagePack)` | 初期化 |
| `static void SetCryptoKey(AesCryptoKey)` | 暗号化キー設定（static共有）。ヘッダー/クエリ/ボディ暗号化に使用 |
| `Func<CancellationToken, Task<TResult>> Get<TResult>(IProgress<float> = null)` | GET実行デリゲート生成（`Post` / `Put` / `Patch` / `Delete` も同形） |
| `void Cancel(bool throwException = false)` | 中断 |
| `virtual int TimeOutSeconds { get; }` | タイムアウト。デフォルト10秒（override で変更） |
| `protected void SetHeader(string key, string value, bool encrypt = false)` | ヘッダー追加（暗号化オプション付き） |
| `protected virtual void RegisterDefaultHeader()` / `RegisterDefaultUrlParams()` | 常時付与するヘッダー/URLパラメータの登録フック |
| `protected virtual DownloadHandler CreateDownloadHandler()` / `UploadHandler CreateUploadHandler<T>(T content)` | ハンドラ差し替えフック（シリアライズ・圧縮・暗号化はここで実施） |
| `string GetUrlParamsString()` / `GetHeaderString()` / `GetBodyString()` | デバッグ用文字列化（復号して返す） |
| `protected bool encryptHeader / encryptUriQuery / encryptBody / decryptResponse` | 暗号化フラグ（**全てデフォルト true**。継承側で無効化可能） |

### UnityWebRequestExtensions（static / `Modules.Net.WebRequest`）

| メンバー | 説明 |
|---|---|
| `static UniTask<byte[]> Send(this UnityWebRequest, IProgress<float> = null, CancellationToken = default)` | `SendWebRequest` を await 可能に。エラー時 throw、キャンセル時 `Abort` |
| `static bool IsSuccess(this UnityWebRequest)` | レスポンスコードが 2xx 系か |
| `static bool HasError(this UnityWebRequest)` | ConnectionError / DataProcessingError / ProtocolError 判定 |

### UnityWebRequestErrorException（`Modules.Net.WebRequest`）

| メンバー | 説明 |
|---|---|
| `UnityWebRequestErrorException(UnityWebRequest request)` | リクエストからエラー情報を退避して生成 |
| `string RawErrorMessage` / `HttpStatusCode StatusCode` / `bool HasResponse` / `string Text` / `Dictionary<string, string> ResponseHeaders` / `UnityWebRequest Request` | エラー詳細 |

## 注意点・罠

- **API通信は PlayFab 経由が原則**。`WebRequestManager` / `UnityWebRequestClient` 系はこのプロジェクトに具象実装が1つも無い（未使用）。新規のREST API通信が必要になっても、まず PlayFab（CloudScript / TitleData 等）で実現できないか検討し、それでも汎用HTTPが必要な場合のみこの基盤の継承を検討する（独断で使わずユーザーに確認）。
- **クラス名の大小文字ゆれ**: ファイル名は `FileDownloader.cs` だがクラス名は `FileDownLoader`（L が大文字）。grep する時は `-i` 推奨。
- **`FileDownLoader.Initialize()` 必須**: 呼び忘れると内部の Dictionary/List が null のまま `Download` で NRE。
- **Rx は R3**: 本モジュールと Client 側 Download 系は `using R3;`（`Observable<T>` / `Subject<T>`）。UniRx の型と混同しない。`FileDownLoader` は `LifetimeDisposable` 継承なので購読は `.AddTo(Disposable)` 可。
- **キャッシュバスター自動付与**: `DownloadRequest.Download` は URL に `t={UnixTimeミリ秒}` クエリを自動付与する（`?` / `&` は自動判定）。CDNキャッシュ回避のための仕様で、無効化オプションは無い。
- **失敗時は部分ファイル自動削除**: `DownloadRequest` は `removeFileOnAbort = true` + catch 内 `SafeDelete(FilePath)` で書きかけファイルを消す。さらに呼び出し側は「一時ファイル（`.tmp`）にDL → 完了後 `File.Move` でリネーム」パターンを踏襲すると安全（実例4、`FileAssetDownLoader` も同様）。
- **保存先ディレクトリは呼び出し側で作成**: `DownloadRequest` はディレクトリを作らない。実例は全て `Directory.GetParent(filePath)` → `Create()` してからDLしている。
- **同一URLの並行DLは1本に合流**: `FileDownLoader` はURLをキーに Observable を `Share` するため、同じURLを同時に要求しても実DLは1回。
- **`OnError` の戻り値がリトライ制御**: `RequestErrorHandle.Retry` を返すと `RetryDelaySeconds` 待って再試行、`Cancel` で即終了。上限（`RetryCount`）到達で `OnRetryLimit`。Client 実装（`MasterFileDownLoader` 等）は上限到達で `ForceCancelAll()` → タイトル画面へ強制遷移している。
- **タイムアウトの既定値**: `DownloadRequest` 30秒（プロパティで変更可） / `NetworkConnection` 5秒 / `UnityWebRequestClient` 10秒（virtual）。
- **`NetworkConnection.WaitNetworkReachable` は自動で呼ばれる**: `FileDownLoader` / `UnityWebRequestClient` のDL・送信前に組み込み済み。タイムアウト時は `NetworkReachabilityException` が飛び、`OnNotReachableAsObservable` が発火する（`ContentsUpdateManager` が購読してエラー画面制御）。
- **`WebRequestManager` はデフォルト直列実行**: `parallel: false`（デフォルト）のリクエストはキューイングされ同時実行されない。並列にしたい場合のみ `parallel: true`。
- **`UnityWebRequestClient` の暗号化フラグはデフォルト全ON**: `SetCryptoKey` を設定せずに使うと暗号化処理でキー無しのまま `Encrypt` が走る。継承時は `encryptHeader` 等の protected フィールドを用途に合わせて設定すること。
- **`ApiTracker` はエディタ専用**: `UnityWebRequestManager` の通信記録は `#if UNITY_EDITOR` 内のみ。実機ログは `LogEnable` を有効化。

## 関連

- [ExternalAsset](ExternalAsset.md) — アセットDLが本基盤を使用（`AssetBundleManager` は `DownloadRequest` 直接使用、`FileAssetDownLoader` は `FileDownLoader` 継承）
- [Master](Master.md) — マスターDL（`MasterFileDownLoader` / `MasterVersionFileDownloader`）が本基盤を使用
- [PlayFab](PlayFab.md) — API通信の主経路（本モジュールの WebRequest 系ではなく PlayFab CSharpSDK）。Entity Files のDLのみ `PlayFabFileDownLoader` 経由
- [CriWare](CriWare.md) — `CriAssetManager` が `NetworkConnection.WaitNetworkReachable` を使用
- [R3Extension](R3Extension.md) — `FileDownLoader` 内部の `ObservableEx.FromUniTask`

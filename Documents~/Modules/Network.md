# Network

> **namespace**: `Modules.Network`（到達性） / `Modules.Network.WebRequest`（HTTP API通信） / `Modules.Network.WebDownload`（ファイルDL）
> **場所**: `Scripts/Modules/Network/`
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`, `LifetimeDisposable`, `PathUtility`, 圧縮・暗号化拡張） / Modules.R3Extension（`ObservableEx.FromUniTask`） / MessagePack / Newtonsoft.Json / Modules.Devkit.Console

## 概要

HTTP通信の基盤モジュール。3系統に分かれる。

1. **NetworkConnection**（`Modules.Network`）: ネットワーク到達性の待機・オフライン通知（static）。
2. **WebDownload系**（`Modules.Network.WebDownload`）: HTTP GET によるファイルダウンロード基盤。CDN からのファイル取得等。主要クラスは `FileDownLoader<TDownloadRequest>`（abstract・DL管理: 同一URL重複排除・並列数制御・リトライ）/ `DownloadRequest`（1ファイルのGET DL実行体）/ `FileDownloadHandler`（ストリーム書込ハンドラ）。
3. **WebRequest系**（`Modules.Network.WebRequest`）: GET/POST等のREST API通信基盤（リトライ・暗号化・圧縮・MessagePack/Json）。拡張メソッド `UnityWebRequest.Send()` / `HasError()` と例外型 `UnityWebRequestErrorException` は WebDownload 系・Devkit から使用されている。

新規にHTTPダウンロード処理を書く場合、`UnityWebRequest` を素で書かず `FileDownLoader<DownloadRequest>` 継承（キュー・リトライ・並列数制御込み）か、単発なら `DownloadRequest` 直接使用を選ぶ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ファイルをHTTP GETでローカル保存（リトライ・並列制御付き） | `FileDownLoader<DownloadRequest>` を継承 |
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
| DLデータをストリームでファイル書き込み | `FileDownloadHandler`（`DownloadHandlerScript` 派生） |
| REST APIサーバーと通信したい | `UnityWebRequestManager<T,R>` + `UnityWebRequestClient` を継承 |
| API ごとにリトライの可否を決めたい | `Get` / `Post` / `Put` / `Patch` / `Delete` の `retry` 引数（`true` / `false`） |
| リトライの既定をプロジェクトで変えたい（例: POST はリトライしない） | `IsRetryable(webRequest)` を override（`retry` 引数が null のときに使われる） |
| 4xx ではリトライしないようにしたい | `IsRetryableError(webRequest)` を override し `!IsNonRetryableClientError(webRequest)` を返す |
| 本文が空の成功応答（204 等）を成功にしたい | `IsEmptyResponseSuccess(webRequest)` を override。返り値は null のままなので成否は `webRequest.IsSuccess` で見る |
| REST 通信の HTTP ステータスコードを数値で取りたい | `IWebRequestClient.ResponseCode`（応答なしは 0）。`StatusCode` は STATUS ヘッダーの文字列 |

## 使い方

定型パターン:

- **FileDownLoader 継承クラスの利用**: `Initialize()` → `SetMaxDownloadCount(n)` → `SetServerUrl(baseUrl)` → `Download(relativePath, filePath)`
- **FileDownLoader 継承クラスの定義パターン**: `OnComplete` / `OnError`（`Retry` / `Cancel` を返す）/ `OnRetryLimit` の3フックを実装
- **フルURLを直接ダウンロード**: `SetServerUrl(null)` にするとフルURLをそのまま使える
- **DownloadRequest 単体使用**（一時ファイルにDL → 完了後 `File.Move` でリネーム）: `Scripts/Modules/ExternalAssets/AssetBundle/AssetBundleManager.cs`

## 注意点・罠

- **クラス名の大小文字ゆれ**: ファイル名は `FileDownloader.cs` だがクラス名は `FileDownLoader`（L が大文字）。grep する時は `-i` 推奨。
- **`FileDownLoader.Initialize()` 必須**: 呼び忘れると内部の Dictionary/List が null のまま `Download` で NRE。
- **Rx は R3**: 本モジュールは `using R3;`（`Observable<T>` / `Subject<T>`）。`FileDownLoader` は `LifetimeDisposable` 継承なので購読は `.AddTo(Disposable)` 可。
- **キャッシュバスター自動付与**: `DownloadRequest.Download` は URL に `t={UnixTimeミリ秒}` クエリを自動付与する（`?` / `&` は自動判定）。CDNキャッシュ回避のための仕様で、無効化オプションは無い。
- **失敗時は部分ファイル自動削除**: `DownloadRequest` は `removeFileOnAbort = true` + catch 内 `SafeDelete(FilePath)` で書きかけファイルを消す。さらに呼び出し側は「一時ファイル（`.tmp`）にDL → 完了後 `File.Move` でリネーム」パターンを踏襲すると安全。
- **保存先ディレクトリは呼び出し側で作成**: `DownloadRequest` はディレクトリを作らない。`Directory.GetParent(filePath)` → `Create()` してからDL。
- **同一URLの並行DLは1本に合流**: `FileDownLoader` はURLをキーに Observable を `Share` するため、同じURLを同時に要求しても実DLは1回。
- **`OnError` の戻り値がリトライ制御**: `RequestErrorHandle.Retry` を返すと `RetryDelaySeconds` 待って再試行、`Cancel` で即終了。上限（`RetryCount`）到達で `OnRetryLimit`。上限到達時の遷移などは継承側で実装する。
- **タイムアウトの既定値**: `DownloadRequest` 30秒（プロパティで変更可） / `NetworkConnection` 5秒 / `UnityWebRequestClient` 10秒（virtual）。
- **`NetworkConnection.WaitNetworkReachable` は自動で呼ばれる**: `FileDownLoader` / `UnityWebRequestClient` のDL・送信前に組み込み済み。タイムアウト時は `NetworkReachabilityException` が飛び、`OnNotReachableAsObservable` が発火する。
- **`WebRequestManager` の既定は、回線なし以外の失敗をすべてリトライする**: 4xx も POST / PATCH も、本文が空の成功応答（204 等）も、`RetryCount` 回まで再送する。タイムアウト後の再送で、非冪等な API がサーバーで二重に実行されうる。止めるには `retry` 引数・`IsRetryable`・`IsRetryableError` を使う（逆引き参照）。
- **リトライの判定順**: 成功（結果あり、または `IsEmptyResponseSuccess` が true の成功応答）→ キャンセルなら何も呼ばずに null → リトライを許可していない（`retry` 引数、省略時は `IsRetryable`）か `IsRetryableError` が false なら `OnError`（`Error` があるとき）→ `OnRetryLimit` で終了 → リトライ回数オーバーなら `OnRetryLimit` で終了 → それ以外は `OnError` → `WaitErrorHandling`（`Retry` / `Cancel` / `Exit`）→ `RetryDelaySeconds` 待って `OnRetry`。`WaitErrorHandling` が `Exit` / `Cancel` を返したときは `OnRetryLimit` を呼ばない。
- **失敗・キャンセル・本文が空の成功は、どれも返り値が null**: 区別が要るときは `IWebRequestClient` の `IsSuccess`（直近の送信が成功ステータスで変換まで完了したか）・`IsCanceled`・`Error` を見る。いずれも送信ごとに初期化され、Dispose 後も読める。
- **`StatusCode` は STATUS ヘッダーの文字列**: タイムアウト・接続失敗などで応答ヘッダーが無いときは null。数値のコードは `ResponseCode` を使う。
- **`WebRequestManager` はデフォルト直列実行**: `parallel: false`（デフォルト）のリクエストはキューイングされ同時実行されない。並列にしたい場合のみ `parallel: true`。
- **`UnityWebRequestClient` の暗号化フラグはデフォルト全ON**: `SetCryptoKey` を設定せずに使うと暗号化処理でキー無しのまま `Encrypt` が走る。継承時は `encryptHeader` 等の protected フィールドを用途に合わせて設定すること。
- **`ApiTracker` はエディタ専用**: `UnityWebRequestManager` の通信記録は `#if UNITY_EDITOR` 内のみ（`Modules.Devkit.ApiMonitor`）。実機ログは `LogEnable` を有効化。

## 関連

- [ExternalAssets](ExternalAssets.md) — アセットDLが本基盤を使用（`AssetBundleManager` は `DownloadRequest` 直接使用、`FileAssetDownLoader` は `FileDownLoader` 継承）
- [Master](Master.md) — マスターDLに本基盤を使用可能
- [PlayFab](PlayFab.md) — Entity Files のDLに `FileDownLoader` 継承を使用可能
- [CriWare](CriWare.md) — `CriAssetManager` が `NetworkConnection.WaitNetworkReachable` を使用
- [R3Extension](R3Extension.md) — `FileDownLoader` 内部の `ObservableEx.FromUniTask`

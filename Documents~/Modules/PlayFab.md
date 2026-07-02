# PlayFab

> **namespace**: `Modules.PlayFabCSharp`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PlayFab/`
> **Client側使用**: 19ファイル（2026-07時点。全て `Client/Assets/Scripts/PlayFab/Api/` 配下）
> **依存**: PlayFab CSharpSDK（`Client/Assets/ThirdParty/PlayFab CSharpSDK/`）/ System.Net.Http。全体が `#if ENABLE_PLAYFAB_CSHARP`（`Client/Assets/csc.rsp` で定義済み）

## 概要

PlayFab CSharpSDK の補助基盤。基盤側は2ファイルのみで、(1) SDKが直接サポートしない生 HTTP GET/PUT（Entity File のアップロード先URL等）を行う `PlayFabHttpEx`、(2) `PlayFabResult<T>` のエラー判定拡張 `HasError()` を提供する。
**実際のAPI呼び出しの入口は Client側ラッパー**（`Client/Assets/Scripts/PlayFab/`、namespace `PlayFabExtensions` / `PlayFabExtensions.Api` / `PlayFab.AzureFunctions`）。新しいAPIやCloudScript呼び出しを追加する時は本ドキュメントの「使い方(実例)」の手順に従う。

### レイヤー構造（呼び出しの流れ）

```
[呼び出し側] UserModel / SystemModel / SaveDataManager / PurchaseManager 等
    ↓ PlayFabManager.Instance.Xxx()          … LoadingScope 付きファサード
[Client] PlayFabExtensions.Api.Xxx.Execute()  … 1 API = 1クラス
    ↓ PlayFabClientAPI.XxxAsync(request)      … Task ベース（CSharpSDK。Unity SDKではない）
[SDK] Client/Assets/ThirdParty/PlayFab CSharpSDK
    ↑ result.HasError()                       … 本モジュール（Modules.PlayFabCSharp）
    ↑ PlayFabHttpEx.DoGet/DoPut               … 本モジュール（SDK外の生HTTPが必要な場合のみ）

CloudScript (Azure Functions) 系:
[呼び出し側] → PlayFab.AzureFunctions.Xxx.CallFunction(...)   … 1 Function = 1クラス
    → PlayFabCloudScript.Execute("FunctionName", body)        … MessagePack+Base64 / 最大3回リトライ
    → PlayFabCloudScriptAPI.ExecuteFunctionAsync(request)
```

### サーバー時間の供給元（プロジェクトルール「`DateTime.Now` 禁止」の根拠）

`SystemModel.LocalTime` / `CurrentUnixTime`（`Client/Assets/Scripts/Client/Model/System/SystemModel.time.cs`）は、
`PlayFabManager.GetTime()` → `PlayFabClientAPI.GetTimeAsync`（`Api/GetTime.cs`）で取得したサーバーUnixTimeを `serverBaseTime` に保持し、取得時からの `RealtimeSinceStartup` 経過秒を加算して算出している。
時刻が必要な処理は PlayFab API を直接呼ばず **`SystemModel.Instance.LocalTime` を使う**こと（端末時計改竄の影響を受けない）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| `PlayFabResult<T>` のエラー判定 | `result.HasError()`（`Modules.PlayFabCSharp.PlayFabResultExtensions`） |
| SDK外URLへ生PUT/GET（ファイルアップロード等） | `PlayFabHttpEx.DoPut(url, bytes)` / `PlayFabHttpEx.DoGet(url)` |
| PlayFab API を呼ぶ（ローディング表示込み） | `PlayFabManager.Instance.Xxx()`（例: `GetUserData()`, `UpdateUserData()`） |
| 新しい PlayFab API 呼び出しを追加 | 「使い方(実例) > 4. 新しい PlayFab API の追加手順」 |
| 新しい CloudScript (Azure Functions) を呼ぶ | 「使い方(実例) > 5. 新しい CloudScript 呼び出しの追加手順」 |
| サーバー時間を使う | `SystemModel.Instance.LocalTime` / `CurrentUnixTime`（PlayFab APIを直接呼ばない） |
| ログイン / アカウント作成 | `UserModel.LoginPlayFab()` → 内部で `PlayFabManager.Login(userCode)` |
| PlayFab専用の色付きコンソールログ | `PlayFabLog.Log()` / `Warning()` / `Error()`（`PlayFabExtensions`） |
| API失敗時の共通エラー処理（ダイアログ表示） | `PlayFabManager.ThrowError(httpCode)` / 購読は `OnErrorAsObservable()` |
| CloudScript リトライ上限時の継続判断 | `PlayFabCloudScript.OnRetryLimitAsObservable()`（`SystemModel` が購読済み） |
| メンテナンス/バージョン/配信ハッシュ取得 | `PlayFabManager.Instance.GetTitleData()`（TitleData キーは `Constants.PlayfabConstants`） |
| エディタから TitleData を書き込む | `SetPlayFabTitleData`（**エディタ専用**。`Api/Editor/`） |
| セーブデータのPlayFabバックアップ | `SaveDataManager.BackupToPlayFab()`（内部で `UploadFile`） |

## 主要クラス

### 基盤側（`Modules.PlayFabCSharp`）

| クラス | 種別 | 役割 |
|---|---|---|
| `PlayFabHttpEx` | static（実体は staticメソッドのみの class） | `HttpClient` による生 GET/PUT。戻り値 `Task<object>` は成功時レスポンス文字列 / 失敗時 `PlayFabError`。PlayFabのシリアライザプラグインとエラーフォーマットを踏襲 |
| `PlayFabResultExtensions` | static（拡張メソッド） | `PlayFabResult<T>.HasError()`。`Error != null` **または `Result == null`** をエラー扱い |

### Client側ラッパー（`Client/Assets/Scripts/PlayFab/`。API追加時の入口）

| クラス | 種別 | 役割 |
|---|---|---|
| `PlayFabManager` | Singleton（`Extensions.Singleton<T>`。`Instance` 初回アクセスで自動生成、`OnCreate` で TitleId 設定） | 全APIのファサード。各メソッドを `LoadingScope` で包む。`EntityId` / `EntityType` / `AuthContext` を保持（Login時に設定）。`ThrowError` で共通エラーダイアログ |
| `PlayFabExtensions.Api.*` | 通常クラス（1 API = 1クラス、`Execute()` を持つ） | `Login` / `Logout` / `GetTime` / `GetTitleData` / `GetTitleNews` / `GetUserData` / `UpdateUserData` / `UploadFile` / `DownloadFile` / `SetTransferCode` / `GetTransferCode` / `TransferLogin` / `GetLeaderboard` / `GetLeaderBoardAroundPlayer` / `UpdateUserTitleDisplayName` / `UpdatePlayerStatistics`（未使用） / `Economy_legacy.*` / `Economy_v2.*` / `Purchase.*` |
| `PlayFabCloudScript` | static | Azure Functions 実行の共通処理。MessagePack→Base64 でパラメータ送信、1秒間隔・最大3回リトライ、上限到達で `RetryLimitAsyncHandler`（`Modules.R3Extension.AsyncHandler` 派生）を通知 |
| `PlayFab.AzureFunctions.*` | 通常クラス（1 Function = 1クラス、static `CallFunction()` を持つ） | `VerifyToken`(HubConnect) / `SyncUserDataAnalytics` / `UpdateArenaStatistics` / `UpdateTowerStatistics` / `UpdateChallengeQuestStatistics`（Arena/Tower/Challenge は定義済み・呼び出し元未実装） / `HelloWorld`（直接 `ExecuteFunctionAsync` を呼ぶサンプル） |
| `PlayFabLog` | static | `UnityConsole`（Modules.Devkit.Console）へ "PlayFab" イベント名・赤色でログ出力 |
| `SetPlayFabTitleData` | **エディタ専用**（`Api/Editor/`） | `PlayFabServerAPI.SetTitleDataAsync` + `DeveloperSecretKey` で TitleData 書き込み。ランタイムコードから使用禁止 |

## 使い方(実例)

### 1. 通常APIの呼び出し（サーバー時間の同期）

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/SystemModel.time.cs
private async UniTask<bool> GetTime()
{
    var playFabManager = PlayFabManager.Instance;

    if (playFabManager.AuthContext == null) { return false; }

    var serverTime = await playFabManager.GetTime();

    if (!serverTime.HasValue){ return false; }

    serverBaseTime = serverTime.Value;
    serverTimeFetch = UnityUtility.RealtimeSinceStartup;

    return true;
}
```

### 2. Api クラスの実装パターン（`HasError` + `ThrowError` + `PlayFabLog`）

```csharp
// 引用元: Client/Assets/Scripts/PlayFab/Api/UpdateUserData.cs
using Modules.PlayFabCSharp;    // HasError() 拡張のため必須.

namespace PlayFabExtensions.Api
{
    /// <summary> ユーザーデータ更新 </summary>
    public sealed class UpdateUserData
    {
        public async UniTask<bool> Execute(Dictionary<string, string> records, UserDataPermission permission = UserDataPermission.Private)
        {
            var request = new UpdateUserDataRequest
            {
                Data = records,
                Permission = permission,
            };

            var result = await PlayFabClientAPI.UpdateUserDataAsync(request);

            if (result.HasError())
            {
                PlayFabManager.ThrowError(result.Error.HttpCode);

                PlayFabLog.Error($"PlayFab updateUserData failed.\n { result.Error.ToJson(true) }");

                return false;
            }

            PlayFabLog.Log($"PlayFab updateUserData success.\n Records : { records.ToJson(true) }");

            return true;
        }
    }
}
```

### 3. CloudScript 呼び出しクラスのパターン

```csharp
// 引用元: Client/Assets/Scripts/PlayFab/CloudScript/UpdateArenaStatistics.cs
namespace PlayFab.AzureFunctions
{
    public sealed class UpdateArenaStatistics
    {
        [MessagePackObject(true)]
        public sealed class RequestBody
        {
            public PublishType publishType = PublishType.None;

            public string statisticName = string.Empty;

            public uint? version = null;

            public int score = 0;
        }

        public static async UniTask<bool> CallFunction(string statisticName, int score, uint? version)
        {
            var appConfig = AppConfig.Instance;

            var body = new RequestBody()
            {
                publishType = appConfig.GetPublishType(),
                statisticName = statisticName,
                version = version,
                score = score,
            };

            var result = await PlayFabCloudScript.Execute("UpdateArenaStatistics", body);

            return result;
        }
    }
}
```

呼び出し側の実例:

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/SystemModel.cs (SyncUserAnalytics)
var argument = new SyncUserDataAnalytics.Argument()
{
    gem = userResourceModel.Gem,
    gold = userResourceModel.Gold,
    adsSkipTicket = userContentsManager.GetAmount(ContentType.Item, ItemConstants.AdSenseSkipTicketId),
};

await SyncUserDataAnalytics.CallFunction(argument);
```

### 4. 新しい PlayFab API の追加手順

1. `Client/Assets/Scripts/PlayFab/Api/` に `sealed class Xxx`（namespace `PlayFabExtensions.Api`）を作成。上記2の `UpdateUserData` を雛形にする
   - `PlayFabClientAPI.XxxAsync(request)` を `await` → `result.HasError()` で判定
   - 失敗時: `PlayFabManager.ThrowError(result.Error.HttpCode)` + `PlayFabLog.Error(...)` → `null` / `false` を返す（例外は投げない）
   - 成功時: `PlayFabLog.Log(...)` → 結果を返す
2. `PlayFabManager`（`Client/Assets/Scripts/PlayFab/PlayFabManager.cs`）の `#region API` にファサードメソッドを追加:

```csharp
// 引用元パターン: Client/Assets/Scripts/PlayFab/PlayFabManager.cs
public async UniTask<bool> UpdateUserData(Dictionary<string, string> records, UserDataPermission permission = UserDataPermission.Private)
{
    using (new LoadingScope())
    {
        return await new UpdateUserData().Execute(records, permission);
    }
}
```

3. 呼び出し側は `PlayFabManager.Instance.Xxx()` を使う（Api クラスを直接 new しない）

### 5. 新しい CloudScript (Azure Functions) 呼び出しの追加手順

1. `Client/Assets/Scripts/PlayFab/CloudScript/` に `sealed class Xxx`（namespace `PlayFab.AzureFunctions`）を作成。上記3の `UpdateArenaStatistics` を雛形にする
2. `[MessagePackObject(true)]` 付き `RequestBody` を内部クラスで定義し、**`publishType` フィールドを必ず含める**（`AppConfig.Instance.GetPublishType()` で環境判別。Azure Functions 側が参照）
3. `public static async UniTask<bool> CallFunction(...)` で body を組み立て `PlayFabCloudScript.Execute("<AzureFunction名>", body)` を呼ぶ
4. パラメータは `PlayFabCloudScript.Execute` 内で MessagePack シリアライズ → Base64 文字列化されて送信される（`MessagePackUtility.Serialize` + `Convert.ToBase64String`）。**Azure Functions 側もこの形式でデコードする実装が必要**
5. 戻り値が必要な場合は現状 `Execute` が bool（成否）のみを返す点に注意（`FunctionResult` は未返却。必要なら `HelloWorld.cs` のように `PlayFabCloudScriptAPI.ExecuteFunctionAsync` を直接呼ぶ形を検討）

### 6. 基盤 `PlayFabHttpEx` の使用例（Entity File アップロード）

```csharp
// 引用元: Client/Assets/Scripts/PlayFab/Api/UploadFile.cs (抜粋)
var result = await PlayFabDataAPI.InitiateFileUploadsAsync(request);
// ... result.HasError() チェック後 ...
var uploadUrl = response.UploadDetails[0].UploadUrl;

var bytes = await File.ReadAllBytesAsync(filePath);

await PlayFabHttpEx.DoPut(uploadUrl, bytes);    // SDK外URLへの生PUT.

var finalize = await PlayFabDataAPI.FinalizeFileUploadsAsync(finalizeRequest);
```

## API(主要公開メンバー)

### `PlayFabHttpEx`（基盤・static）

| メンバー | 説明 |
|---|---|
| `Task<object> DoGet(string fullUrl)` | 生GET。戻り値は成功時レスポンス文字列 / 失敗時 `PlayFabError`（型判定して使う） |
| `Task<object> DoPut(string fullUrl, byte[] bytes, Dictionary<string, string> extraHeaders = null)` | バイト列の生PUT（ファイルアップロード用）。`Authorization` ヘッダは Bearer として特別扱い |
| `Task<object> DoPut(string fullUrl, object request, Dictionary<string, string> extraHeaders = null)` | request を PlayFab シリアライザでJSON化してPUT。null なら `{}` |

### `PlayFabResultExtensions`（基盤・拡張メソッド）

| メンバー | 説明 |
|---|---|
| `bool HasError<TResult>(this PlayFabResult<TResult> self)` | `Error != null \|\| Result == null`。API呼び出し後は必ずこれで判定する |

### `PlayFabManager`（Client・Singleton）

| メンバー | 説明 |
|---|---|
| `string EntityId` / `string EntityType` | Login 時に `SetUserInfo` で設定。CloudScript / Entity File API の Entity 指定に使用 |
| `PlayFabAuthenticationContext AuthContext` | Login 時に `SetAuthContext` で設定。未ログイン判定に使える（`SystemModel.GetTime` 参照） |
| `UniTask<ulong?> GetTime()` | サーバーUnixTime取得（**直接使わず `SystemModel` 経由で時刻参照**） |
| `UniTask<GetTitleData.TitleData> GetTitleData()` | メンテフラグ・アプリ/審査バージョン・アセット/マスター rootHash・開発者ID |
| `UniTask<TitleNewsItem[]> GetTitleNews()` | お知らせ取得 |
| `UniTask<Login.Result> Login(string userCode)` | CustomID ログイン。userCode 空なら新規アカウント作成（ID衝突時は自動リトライ）。失敗は `Result.error` に格納（ThrowError しない） |
| `UniTask<bool> Logout()` | `ForgetAllCredentials`（通信なし） |
| `UniTask<Dictionary<string, UserDataRecord>> GetUserData(string[] keys = null, string playFabId = null)` | ユーザーデータ取得 |
| `UniTask<bool> UpdateUserData(Dictionary<string, string> records, UserDataPermission permission = UserDataPermission.Private)` | ユーザーデータ更新 |
| `UniTask<bool> UploadFile(string filePath)` / `UniTask<bool> DownloadFile(string filePath)` | Entity File のアップロード/ダウンロード（要ログイン。セーブデータバックアップ用） |
| `UniTask<bool> SetTransferCode(string)` / `UniTask<string> GetTransferCode()` / `UniTask<bool> DeleteTransferCode()` / `UniTask<bool> TransferLogin(string userCode)` | 引き継ぎコード関連 |
| `UniTask<GetLeaderboardResult> GetLeaderboard(string statisticName, int? maxResultsCount = null)` / `GetLeaderBoardAroundPlayer(...)` | ランキング取得 |
| `UniTask<UpdateUserTitleDisplayNameResult> UpdateUserTitleDisplayName(string displayName)` | 表示名変更 |
| `GetCatalogItems()` / `GetUserInventory()` / `ConsumeItem(...)` | Economy(legacy) カタログ/インベントリ/消費 |
| `GetItems(string[] itemIds)` / `GetInventoryItem()` | Economy v2 |
| `PurchaseAppleStore(PurchaseResult)` / `PurchaseGooglePlay(PurchaseResult)` | 課金レシート検証（`Modules.InAppPurchasing.PurchaseResult` を渡す） |
| `static void ThrowError(int? errorCode = null)` | 共通エラーダイアログ（タイトルへ戻る）表示 + `onError` 通知 |
| `Observable<int?> OnErrorAsObservable()` | API エラーの購読 |

### `PlayFabCloudScript`（Client・static）

| メンバー | 説明 |
|---|---|
| `UniTask<bool> Execute<T>(string functionName, T contentData) where T : class` | Azure Function 実行。contentData を MessagePack+Base64 化。失敗時1秒間隔で最大3回リトライ |
| `Observable<RetryLimitAsyncHandler> OnRetryLimitAsObservable()` | リトライ上限到達の通知。購読者が `requestRetry = true` にして handler を完了させると再リトライ（`SystemModel.InitializeModel` が購読済み） |
| `RetryLimitAsyncHandler`（`AsyncHandler` 派生） | `functionName` / `requestRetry` を持つ応答待ちハンドラ |

### `PlayFabLog`（Client・static）

| メンバー | 説明 |
|---|---|
| `Log(string)` / `Warning(string)` / `Error(string)` / `Assert(string)` | UnityConsole の "PlayFab" イベント（赤色）としてログ出力。PlayFab関連処理では `Debug.Log` でなくこちらを使う |

## 注意点・罠

- **SDK は Unity SDK ではなく CSharpSDK**（`Assets/ThirdParty/PlayFab CSharpSDK/`）。API は `Task<PlayFabResult<T>>` を返し、**失敗しても例外を投げず `Error` プロパティに入る**。呼び出し後は必ず `result.HasError()`（`using Modules.PlayFabCSharp` が必要）で判定する
- `HasError()` は `Result == null` もエラー扱い（`Error` だけ見ると null 参照する場面を防ぐ）
- 全コードが `#if ENABLE_PLAYFAB_CSHARP` 依存。define は `Client/Assets/csc.rsp` で定義済み（`ENABLE_PLAYFABSERVER_API` も同ファイル。エディタの `PlayFabServerAPI` 用）
- CloudScript のパラメータは **JSONではなく MessagePack → Base64 文字列**。`RequestBody` には `[MessagePackObject(true)]` を付け、`publishType` を必ず含める（Azure Functions 側が環境判別に使用）
- `PlayFabCloudScript.Execute` はリトライ上限到達時、`OnRetryLimit` 購読者がいなければ即 `false`。購読者（`SystemModel`）の応答があるまで `await` でブロックする
- Entity 系 API（`UploadFile` / `DownloadFile` / CloudScript）は **ログイン済みが前提**（`EntityId` / `EntityType` は `Login` 成功時に設定される）。`UploadFile` / `DownloadFile` は `PlayFabClientAPI.IsClientLoggedIn()` を内部チェック
- `PlayFabManager` の各ファサードは `using (new LoadingScope())` でローディング表示を出す。UIを出したくない文脈では Api クラス直呼びも可（`SystemModel.time.cs` は `PlayFabManager.GetTime()` 経由なのでローディングが出る点は仕様）
- API 失敗時は `ThrowError` により**エラーダイアログ（タイトルへ戻る）が自動で開く**。`Login` だけは例外で、`Result.error` を返して呼び出し側（`UserModel.LoginPlayFab`）が処理する
- 統計（ランキング）更新はクライアント直更新の `Api/UpdatePlayerStatistics`（現在未使用）ではなく **CloudScript 経由**（`UpdateArenaStatistics` 等）がプロジェクトのパターン
- `SetPlayFabTitleData`（`Api/Editor/`）は `DeveloperSecretKey` を使う**エディタ専用**。ランタイムコードから参照しない。TitleId / キー定数は `Constants.PlayfabConstants`（`Client/Assets/Scripts/Constants/Product/PlayFab.cs`、SecretKey は `.editor.cs` 側）
- 基盤 `PlayFabHttpEx` の戻り値は `Task<object>`（成功: string / 失敗: `PlayFabError`）。呼び出し側で型判定が必要（`UploadFile.cs` は成否を Finalize API の結果で判定している）
- 時刻は `DateTime.Now` ではなく `SystemModel.Instance.LocalTime` を使う（本モジュールの `GetTime` がその供給元）

## 関連

- [LocalData](LocalData.md) — セーブデータ管理。`SaveDataManager.BackupToPlayFab()` が `UploadFile` でバックアップ
- [Master](Master.md) — マスター rootHash を TitleData（`GetTitleData`）から取得
- [ExternalAsset](ExternalAsset.md) — アセット rootHash を TitleData から取得
- [TextData](TextData.md) — エラーダイアログ文言（`TextData.General.Error_PlayFabFailed`）

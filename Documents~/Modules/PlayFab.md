# PlayFab

> **namespace**: `Modules.PlayFabCSharp`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PlayFab/`
> **Client側使用**: 19ファイル（2026-07時点。全て `Client/Assets/Scripts/PlayFab/Api/` 配下）
> **依存**: PlayFab CSharpSDK（`Client/Assets/ThirdParty/PlayFab CSharpSDK/`）/ System.Net.Http。全体が `#if ENABLE_PLAYFAB_CSHARP`（`Client/Assets/csc.rsp` で定義済み）

## 概要

PlayFab CSharpSDK の補助基盤。基盤側は2ファイルのみで、(1) SDKが直接サポートしない生 HTTP GET/PUT（Entity File のアップロード先URL等）を行う `PlayFabHttpEx`、(2) `PlayFabResult<T>` のエラー判定拡張 `HasError()` を提供する。
**実際のAPI呼び出しの入口は Client側ラッパー**（`Client/Assets/Scripts/PlayFab/`、namespace `PlayFabExtensions` / `PlayFabExtensions.Api` / `PlayFab.AzureFunctions`）。新しいAPIやCloudScript呼び出しを追加する時は「使い方」の手順に従う。
主要クラス（Client側）: `PlayFabManager`（Singleton・全APIのファサード。`EntityId` / `EntityType` / `AuthContext` を保持＝Login時に設定）/ `PlayFabExtensions.Api.*`（1 API = 1クラス、`Execute()`）/ `PlayFabCloudScript`（static・Azure Functions 実行の共通処理）/ `PlayFab.AzureFunctions.*`（1 Function = 1クラス、static `CallFunction()`）/ `PlayFabLog`（UnityConsole "PlayFab" イベントへのログ出力。PlayFab関連処理では `Debug.Log` でなくこちらを使う）/ `SetPlayFabTitleData`（エディタ専用）

レイヤー構造（呼び出しの流れ）:

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

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| `PlayFabResult<T>` のエラー判定 | `result.HasError()`（`Modules.PlayFabCSharp.PlayFabResultExtensions`） |
| SDK外URLへ生PUT/GET（ファイルアップロード等） | `PlayFabHttpEx.DoPut(url, bytes)` / `PlayFabHttpEx.DoGet(url)` |
| PlayFab API を呼ぶ（ローディング表示込み） | `PlayFabManager.Instance.Xxx()`（例: `GetUserData()`, `UpdateUserData()`） |
| 新しい PlayFab API 呼び出しを追加 | 「使い方 > 新しい PlayFab API の追加手順」 |
| 新しい CloudScript (Azure Functions) を呼ぶ | 「使い方 > 新しい CloudScript 呼び出しの追加手順」 |
| サーバー時間を使う | `SystemModel.Instance.LocalTime` / `CurrentUnixTime`（PlayFab APIを直接呼ばない） |
| ログイン / アカウント作成 | `UserModel.LoginPlayFab()` → 内部で `PlayFabManager.Login(userCode)` |
| PlayFab専用の色付きコンソールログ | `PlayFabLog.Log()` / `Warning()` / `Error()`（`PlayFabExtensions`） |
| API失敗時の共通エラー処理（ダイアログ表示） | `PlayFabManager.ThrowError(httpCode)` / 購読は `OnErrorAsObservable()` |
| CloudScript リトライ上限時の継続判断 | `PlayFabCloudScript.OnRetryLimitAsObservable()`（`SystemModel` が購読済み） |
| メンテナンス/バージョン/配信ハッシュ取得 | `PlayFabManager.Instance.GetTitleData()`（TitleData キーは `Constants.PlayfabConstants`） |
| エディタから TitleData を書き込む | `SetPlayFabTitleData`（**エディタ専用**。`Api/Editor/`） |
| セーブデータのPlayFabバックアップ | `SaveDataManager.BackupToPlayFab()`（内部で `UploadFile`） |

## 使い方

定型パターン（コードは引用元を読む）:

- 通常APIの呼び出し（サーバー時間の同期）: `Client/Assets/Scripts/Client/Model/System/SystemModel.time.cs` の `GetTime`（`AuthContext == null` なら未ログインとして中断）
- Api クラスの実装パターン（`HasError` + `ThrowError` + `PlayFabLog`。`using Modules.PlayFabCSharp` が必須）: `Client/Assets/Scripts/PlayFab/Api/UpdateUserData.cs`
- CloudScript 呼び出しクラスのパターン: `Client/Assets/Scripts/PlayFab/CloudScript/UpdateArenaStatistics.cs`（呼び出し側実例: `Client/Assets/Scripts/Client/Model/System/SystemModel.cs` の `SyncUserAnalytics`）
- 基盤 `PlayFabHttpEx` の使用例（Entity File アップロード）: `Client/Assets/Scripts/PlayFab/Api/UploadFile.cs`（`InitiateFileUploads` → `DoPut` → `FinalizeFileUploads`）

### 新しい PlayFab API の追加手順

1. `Client/Assets/Scripts/PlayFab/Api/` に `sealed class Xxx`（namespace `PlayFabExtensions.Api`）を作成。`UpdateUserData.cs` を雛形にする
   - `PlayFabClientAPI.XxxAsync(request)` を `await` → `result.HasError()` で判定
   - 失敗時: `PlayFabManager.ThrowError(result.Error.HttpCode)` + `PlayFabLog.Error(...)` → `null` / `false` を返す（例外は投げない）
   - 成功時: `PlayFabLog.Log(...)` → 結果を返す
2. `PlayFabManager`（`Client/Assets/Scripts/PlayFab/PlayFabManager.cs`）の `#region API` に、`using (new LoadingScope())` で Api クラスの `Execute` を包むファサードメソッドを追加
3. 呼び出し側は `PlayFabManager.Instance.Xxx()` を使う（Api クラスを直接 new しない）

### 新しい CloudScript (Azure Functions) 呼び出しの追加手順

1. `Client/Assets/Scripts/PlayFab/CloudScript/` に `sealed class Xxx`（namespace `PlayFab.AzureFunctions`）を作成。`UpdateArenaStatistics.cs` を雛形にする
2. `[MessagePackObject(true)]` 付き `RequestBody` を内部クラスで定義し、**`publishType` フィールドを必ず含める**（`AppConfig.Instance.GetPublishType()` で環境判別。Azure Functions 側が参照）
3. `public static async UniTask<bool> CallFunction(...)` で body を組み立て `PlayFabCloudScript.Execute("<AzureFunction名>", body)` を呼ぶ
4. パラメータは `PlayFabCloudScript.Execute` 内で MessagePack シリアライズ → Base64 文字列化されて送信される（`MessagePackUtility.Serialize` + `Convert.ToBase64String`）。**Azure Functions 側もこの形式でデコードする実装が必要**
5. 戻り値が必要な場合は現状 `Execute` が bool（成否）のみを返す点に注意（`FunctionResult` は未返却。必要なら `HelloWorld.cs` のように `PlayFabCloudScriptAPI.ExecuteFunctionAsync` を直接呼ぶ形を検討）

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
- `PlayFab.AzureFunctions` の `UpdateArenaStatistics` / `UpdateTowerStatistics` / `UpdateChallengeQuestStatistics` は定義済みだが呼び出し元未実装。`HelloWorld` は `PlayFabCloudScriptAPI.ExecuteFunctionAsync` を直接呼ぶサンプル
- `SetPlayFabTitleData`（`Api/Editor/`）は `DeveloperSecretKey` を使う**エディタ専用**。ランタイムコードから参照しない。TitleId / キー定数は `Constants.PlayfabConstants`（`Client/Assets/Scripts/Constants/Product/PlayFab.cs`、SecretKey は `.editor.cs` 側）
- 基盤 `PlayFabHttpEx` の戻り値は `Task<object>`（成功: string / 失敗: `PlayFabError`）。呼び出し側で型判定が必要（`UploadFile.cs` は成否を Finalize API の結果で判定している）
- 時刻は `DateTime.Now` ではなく `SystemModel.Instance.LocalTime` を使う（本モジュールの `GetTime` がその供給元。取得したサーバーUnixTime + 取得時からの経過秒で算出され、端末時計改竄の影響を受けない）

## 関連

- [LocalData](LocalData.md) — セーブデータ管理。`SaveDataManager.BackupToPlayFab()` が `UploadFile` でバックアップ
- [Master](Master.md) — マスター rootHash を TitleData（`GetTitleData`）から取得
- [ExternalAsset](ExternalAsset.md) — アセット rootHash を TitleData から取得
- [TextData](TextData.md) — エラーダイアログ文言（`TextData.General.Error_PlayFabFailed`）

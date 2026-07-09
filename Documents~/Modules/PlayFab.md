# PlayFab

> **namespace**: `Modules.PlayFabCSharp`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PlayFab/`
> **依存**: PlayFab CSharpSDK / System.Net.Http。全体が `#if ENABLE_PLAYFAB_CSHARP`（利用側で定義）

## 概要

PlayFab CSharpSDK の補助基盤。基盤側は2ファイルのみで、(1) SDKが直接サポートしない生 HTTP GET/PUT（Entity File のアップロード先URL等）を行う `PlayFabHttpEx`、(2) `PlayFabResult<T>` のエラー判定拡張 `HasError()` を提供する。
実際のAPI呼び出しの入口は利用側でラッパー（ファサード）を用意することを推奨する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| `PlayFabResult<T>` のエラー判定 | `result.HasError()`（`Modules.PlayFabCSharp.PlayFabResultExtensions`） |
| SDK外URLへ生PUT/GET（ファイルアップロード等） | `PlayFabHttpEx.DoPut(url, bytes)` / `PlayFabHttpEx.DoGet(url)` |

## 使い方

### API 呼び出しラッパーの設計指針（推奨）

- 1 API = 1クラス（`sealed class`）を作り、`PlayFabClientAPI.XxxAsync(request)` を `await` → `result.HasError()`（`using Modules.PlayFabCSharp`）で判定する
- 失敗時: 例外は投げず、共通エラー処理（ダイアログ表示など）に流し、`null` / `false` を返す
- 成功時: 結果を返す
- API ファサードクラス（Singleton 等）を用意し、各 API クラスの実行を包む。ローディング表示などの横断的な処理はファサード側で `using` スコープ化するとよい

### CloudScript（Azure Functions）呼び出しの設計指針

- 1 Function = 1クラス（`static CallFunction()`）を作り、CloudScript 実行の共通処理（リトライ・エンコード）を1箇所に集約する
- パラメータは **MessagePack シリアライズ → Base64 文字列化** して送信するのがこの基盤の想定（`[MessagePackObject(true)]` 付きの `RequestBody` を定義）。**Azure Functions 側もこの形式でデコードする実装が必要**
- 環境判別のためのフィールド（`publishType` 等）を RequestBody に含めるかは利用側の設計次第

## 注意点・罠

- **SDK は Unity SDK ではなく CSharpSDK**。API は `Task<PlayFabResult<T>>` を返し、**失敗しても例外を投げず `Error` プロパティに入る**。呼び出し後は必ず `result.HasError()`（`using Modules.PlayFabCSharp` が必要）で判定する
- `HasError()` は `Result == null` もエラー扱い（`Error` だけ見ると null 参照する場面を防ぐ）
- 全コードが `#if ENABLE_PLAYFAB_CSHARP` 依存。利用側でシンボル定義が必要
- Entity 系 API（`UploadFile` / `DownloadFile` / CloudScript）は **ログイン済みが前提**（`EntityId` / `EntityType` はログイン成功時に設定される）
- 基盤 `PlayFabHttpEx` の戻り値は `Task<object>`（成功: string / 失敗: `PlayFabError`）。呼び出し側で型判定が必要

## 関連

- [LocalData](LocalData.md) — セーブデータ管理。PlayFab へのバックアップは Entity Files を利用
- [Master](Master.md) — マスター rootHash 等を TitleData から取得
- [ExternalAsset](ExternalAsset.md) — アセット rootHash 等を TitleData から取得
- [TextData](TextData.md) — エラーダイアログ文言

# AmazonWebService

> **namespace**: `Modules.Amazon.S3`（フォルダ名 `AmazonWebService/` と不一致に注意）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/AmazonWebService/Editor/`（**全ファイルがエディタ専用**）
> **Client側使用**: 2ファイル（2026-07時点: `Scripts/Editor/ExternalAsset/Uploader.cs` / `Scripts/Editor/Master/Uploader.cs`）+ 基盤内2ファイル（ExternalAsset / MasterGenerator の各 `S3Uploader`）
> **依存**: AWS SDK for .NET（`Amazon.S3` / `Amazon.CognitoIdentity` / `Amazon.Runtime`） / Extensions / コンパイルシンボル `ENABLE_AMAZON_WEB_SERVICE`（`Assets/csc.rsp` で定義済み）

## 概要

AWS S3 への薄いラッパー（`S3Client`）と、アップロードツールの基底クラス（`S3UploaderBase`）。
本プロジェクトでの実用途は **エディタから配信データを S3（バケット `game-dominion`・東京リージョン）へアップロードする**こと。
具体的には (1) ExternalAsset（アセットバンドル等の配信アセット）→ `assets/` 配下、(2) マスターデータ → `master/` 配下、の2系統。ランタイム（実機）からは使わない。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 配信アセットをS3にアップロードしたい | `Dominion.Editor.ExternalAsset.Uploader`（既存。ビルドフローから使用） |
| マスターデータをS3にアップロードしたい | `Dominion.Editor.Master.Uploader`（既存） |
| 新しいアップロードツールを作りたい | `S3UploaderBase` を継承 + `IBasicCredentials`（or `ICognitoCredentials`）実装 |
| S3上のファイル一覧を取りたい | `S3Client.GetObjectList(prefix, maxKeys)`（ページング自動追従） |
| S3からダウンロードしたい | `S3Client.GetObject(objectPath)` |
| ファイル/フォルダをアップロードしたい | `S3Client.Upload(filePath, objectPath)` / `UploadDirectory(directoryPath)` |
| S3上のファイルを消したい | `S3Client.DeleteObject(s)` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `S3Client` | **エディタ専用** sealed class | `AmazonS3Client` のラッパー。一覧/メタ取得/取得/アップロード/Put/削除。バケット名はコンストラクタで固定 |
| `S3UploaderBase` | **エディタ専用** abstract class | アップローダー基底。資格情報 interface を実装した派生クラスから `CreateS3Client()` で接続 |
| `IBasicCredentials` | interface | `GetAccessKey()` / `GetSecretKey()`（アクセスキー方式） |
| `ICognitoCredentials` | interface | `GetIdentityPoolId()` / `GetCredentialsRegion()`（Cognito Identity Pool 方式） |

派生関係（本プロジェクト）:

```
S3UploaderBase (本モジュール)
├─ Modules.ExternalAssets.S3Uploader (ExternalAsset/Editor) ─ Dominion.Editor.ExternalAsset.Uploader (Client)
└─ Modules.Master.Editor.S3Uploader (Devkit/MasterGenerator/Editor) ─ Dominion.Editor.Master.Uploader (Client)
```

## 使い方(実例)

### アップローダー定義（マスターデータ用）

```csharp
// 引用元: Client/Assets/Scripts/Editor/Master/Uploader.cs（キー値はマスク）
public sealed class Uploader : Modules.Master.Editor.S3Uploader, IBasicCredentials
{
    protected override S3CannedACL UploadFileCannedACL
    {
        get { return S3CannedACL.Private; }
    }

    public string GetAccessKey()
    {
        return "AKIA****************";  // 実コードに直書きされている.
    }

    public string GetSecretKey()
    {
        return "****";  // 実コードに直書きされている.
    }

    public override string GetBucketName()
    {
        return "game-dominion";
    }

    public override RegionEndpoint GetBucketRegion()
    {
        return RegionEndpoint.APNortheast1;
    }

    protected override string BucketFolderOverride(string bucketFolder)
    {
        return $"master/{bucketFolder}";
    }
}
```

ExternalAsset 用（`Scripts/Editor/ExternalAsset/Uploader.cs`）も同じ形で、`BucketFolderOverride` が `$"assets/{bucketFolder}"` になる。

## API(主要公開メンバー)

### S3Client（全メソッド `System.Threading.Tasks.Task` ベース）

| メンバー | 説明 |
|---|---|
| `S3Client(bucketName, bucketRegion, IBasicCredentials)` | アクセスキー方式で生成 |
| `S3Client(bucketName, bucketRegion, ICognitoCredentials)` | Cognito 方式で生成 |
| `string BucketName { get; }` | 対象バケット名 |
| `Task<S3Object[]> GetObjectList(string prefix = null, int? maxKeys = null)` | オブジェクト一覧。`IsTruncated` を自動追従して全件取得（Request 版オーバーロードあり） |
| `Task<GetObjectMetadataResponse> GetObjectMetaData(string objectPath)` | メタ情報取得 |
| `Task<byte[]> GetObject(string objectPath, Action<GetObjectResponse> onComplete = null)` | ダウンロード（byte[]） |
| `Task Upload(string uploadFilePath, string objectPath = null)` | 1ファイルアップロード（objectPath 省略時はファイル名がキー） |
| `Task Upload(TransferUtilityUploadRequest uploadRequest)` | 詳細指定アップロード（ACL 等） |
| `Task UploadDirectory(string uploadDirectoryPath)` | ディレクトリ一括アップロード |
| `Task<PutObjectResponse> Put(PutObjectRequest request)` | PutObject 直接実行 |
| `Task<DeleteObjectResponse> DeleteObject(string objectPath)` | 1件削除（Request 版あり） |
| `Task<DeleteObjectsResponse> DeleteObjects(string[] objectPaths, string[] versionIds = null)` | 複数削除（Request 版あり） |

※ Request 版オーバーロードでも `BucketName` は S3Client 側の値で上書きされる。

### S3UploaderBase（継承して使う）

| メンバー | 説明 |
|---|---|
| `abstract string GetBucketName()` / `abstract RegionEndpoint GetBucketRegion()` | 接続先の定義（派生クラスで必須） |
| `protected void CreateS3Client()` | `s3Client` フィールドを生成。**自身が `IBasicCredentials` / `ICognitoCredentials` のどちらかを実装していないと例外** |
| `protected virtual S3CannedACL UploadFileCannedACL` | アップロードACL。**既定は `PublicRead`**（本プロジェクトの派生は全て `Private` にオーバーライド） |
| `protected virtual string BucketFolderOverride(string bucketFolder)` | アップロード先フォルダの加工フック（`assets/` / `master/` プレフィックス付与に使用） |

## 注意点・罠

- **エディタ専用**（`Editor/` 配下のみ + `#if ENABLE_AMAZON_WEB_SERVICE`）。ランタイムコードから参照するとビルドが通らない。実機のアセットダウンロードは S3 直ではなく [ExternalAsset](ExternalAsset.md) / [Network](Network.md) 経由
- namespace は `Modules.Amazon.S3`（`Modules.AmazonWebService` ではない）
- 非同期は UniTask ではなく **`System.Threading.Tasks.Task`**（AWS SDK 準拠）。呼び出し側（ExternalAsset の S3Uploader 等）で UniTask に変換している
- `S3UploaderBase.UploadFileCannedACL` の既定値が `PublicRead`。新規アップローダーでは明示的に `Private` へオーバーライドすること（既存2実装とも Private）
- AWS アクセスキー/シークレットが Client 側 Uploader の**ソースコードに直書き**されている。値の転記・ログ出力・外部共有はしない
- `GetObjectList` の prefix 正規化は「セパレータで終わっている場合にさらにセパレータを足す」実装（`prefix.EndsWith(separator)` で `+= separator`）になっており、末尾 `/` 付きで渡すと `//` になる。**prefix は末尾セパレータなしで渡す**のが安全
- バケット等の実配置: バケット `game-dominion`（東京 `APNortheast1`）、ExternalAsset → `assets/`、マスター → `master/`

## 関連

- [ExternalAsset](ExternalAsset.md) — アップロードした配信アセットのランタイム側ダウンロード/管理
- [Master](Master.md) — マスターデータの配信と読み込み
- [Devkit](Devkit.md) — MasterGenerator（マスター生成 → S3 アップロードの流れ）

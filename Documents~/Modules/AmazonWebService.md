# AmazonWebService

> **namespace**: `Modules.Amazon.S3`（フォルダ名 `AmazonWebService/` と不一致に注意）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/AmazonWebService/Editor/`（**全ファイルがエディタ専用**）
> **依存**: AWS SDK for .NET（`Amazon.S3` / `Amazon.CognitoIdentity` / `Amazon.Runtime`） / Extensions / コンパイルシンボル `ENABLE_AMAZON_WEB_SERVICE`（利用側で定義）

## 概要

AWS S3 への薄いラッパー（`S3Client`）と、アップロードツールの基底クラス（`S3UploaderBase`）。
主用途は **エディタから配信データを S3 へアップロードする**こと（アセットバンドル・マスターデータ等）。ランタイム（実機）からは使わない。
主要クラス: `S3Client`（`AmazonS3Client` のラッパー。一覧/メタ取得/取得/アップロード/Put/削除）/ `S3UploaderBase`（アップローダー基底。`IBasicCredentials` or `ICognitoCredentials` を実装した派生クラスから `CreateS3Client()` で接続）。

基盤内の派生:

```
S3UploaderBase (本モジュール)
├─ Modules.ExternalAssets.S3Uploader (ExternalAsset/Editor)
└─ Modules.Master.Editor.S3Uploader (Devkit/MasterGenerator/Editor)
```

利用側でさらに派生させ、認証情報（`IBasicCredentials` 実装）とバケット設定を与える。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しいアップロードツールを作りたい | `S3UploaderBase` を継承 + `IBasicCredentials`（or `ICognitoCredentials`）実装 |
| S3上のファイル一覧を取りたい | `S3Client.GetObjectList(prefix, maxKeys)`（ページング自動追従） |
| S3からダウンロードしたい | `S3Client.GetObject(objectPath)` |
| ファイル/フォルダをアップロードしたい | `S3Client.Upload(filePath, objectPath)` / `UploadDirectory(directoryPath)` |
| S3上のファイルを消したい | `S3Client.DeleteObject(s)` |

## 使い方

- アップローダー定義パターン: 用途別の `S3Uploader` 基底を継承 + `IBasicCredentials` を実装し、`GetBucketName` / `GetBucketRegion` / `UploadFileCannedACL` / `BucketFolderOverride` をオーバーライドする

## 注意点・罠

- **エディタ専用**（`Editor/` 配下のみ + `#if ENABLE_AMAZON_WEB_SERVICE`）。ランタイムコードから参照するとビルドが通らない。実機のアセットダウンロードは S3 直ではなく [ExternalAsset](ExternalAsset.md) / [Network](Network.md) 経由
- namespace は `Modules.Amazon.S3`（`Modules.AmazonWebService` ではない）
- 非同期は UniTask ではなく **`System.Threading.Tasks.Task`**（AWS SDK 準拠）。呼び出し側（ExternalAsset の S3Uploader 等）で UniTask に変換している
- `S3UploaderBase.UploadFileCannedACL` の既定値が `PublicRead`。新規アップローダーでは明示的に `Private` へオーバーライドすること
- AWS アクセスキー/シークレットを派生クラスのソースコードに直書きする場合、値の転記・ログ出力・外部共有はしない
- `GetObjectList` の prefix 正規化は「セパレータで終わっている場合にさらにセパレータを足す」実装（`prefix.EndsWith(separator)` で `+= separator`）になっており、末尾 `/` 付きで渡すと `//` になる。**prefix は末尾セパレータなしで渡す**のが安全
- `S3Client` の Request 版オーバーロード（GetObjectList / Upload / Put / Delete 系）でも `BucketName` は S3Client 側の値で上書きされる

## 関連

- [ExternalAsset](ExternalAsset.md) — アップロードした配信アセットのランタイム側ダウンロード/管理
- [Master](Master.md) — マスターデータの配信と読み込み
- [Devkit](Devkit.md) — MasterGenerator（マスター生成 → S3 アップロードの流れ）

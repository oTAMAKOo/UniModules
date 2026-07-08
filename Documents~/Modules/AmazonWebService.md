# AmazonWebService

> **namespace**: `Modules.Amazon.S3`（フォルダ名 `AmazonWebService/` と不一致に注意）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/AmazonWebService/Editor/`（**全ファイルがエディタ専用**）
> **Client側使用**: 2ファイル（2026-07時点: `Scripts/Editor/ExternalAsset/Uploader.cs` / `Scripts/Editor/Master/Uploader.cs`）+ 基盤内2ファイル（ExternalAsset / MasterGenerator の各 `S3Uploader`）
> **依存**: AWS SDK for .NET（`Amazon.S3` / `Amazon.CognitoIdentity` / `Amazon.Runtime`） / Extensions / コンパイルシンボル `ENABLE_AMAZON_WEB_SERVICE`（`Assets/csc.rsp` で定義済み）

## 概要

AWS S3 への薄いラッパー（`S3Client`）と、アップロードツールの基底クラス（`S3UploaderBase`）。
本プロジェクトでの実用途は **エディタから配信データを S3（バケット `game-dominion`・東京リージョン）へアップロードする**こと。
具体的には (1) ExternalAsset（アセットバンドル等の配信アセット）→ `assets/` 配下、(2) マスターデータ → `master/` 配下、の2系統。ランタイム（実機）からは使わない。
主要クラス: `S3Client`（`AmazonS3Client` のラッパー。一覧/メタ取得/取得/アップロード/Put/削除）/ `S3UploaderBase`（アップローダー基底。`IBasicCredentials` or `ICognitoCredentials` を実装した派生クラスから `CreateS3Client()` で接続）。

派生関係（本プロジェクト）:

```
S3UploaderBase (本モジュール)
├─ Modules.ExternalAssets.S3Uploader (ExternalAsset/Editor) ─ Dominion.Editor.ExternalAsset.Uploader (Client)
└─ Modules.Master.Editor.S3Uploader (Devkit/MasterGenerator/Editor) ─ Dominion.Editor.Master.Uploader (Client)
```

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

## 使い方

- アップローダー定義パターン: 用途別の `S3Uploader` 基底を継承 + `IBasicCredentials` を実装し、`GetBucketName` / `GetBucketRegion` / `UploadFileCannedACL` / `BucketFolderOverride` をオーバーライド。実例: `Client/Assets/Scripts/Editor/Master/Uploader.cs`（`BucketFolderOverride` が `master/` プレフィックス付与）/ `Client/Assets/Scripts/Editor/ExternalAsset/Uploader.cs`（同じ形で `assets/` プレフィックス）

## 注意点・罠

- **エディタ専用**（`Editor/` 配下のみ + `#if ENABLE_AMAZON_WEB_SERVICE`）。ランタイムコードから参照するとビルドが通らない。実機のアセットダウンロードは S3 直ではなく [ExternalAsset](ExternalAsset.md) / [Network](Network.md) 経由
- namespace は `Modules.Amazon.S3`（`Modules.AmazonWebService` ではない）
- 非同期は UniTask ではなく **`System.Threading.Tasks.Task`**（AWS SDK 準拠）。呼び出し側（ExternalAsset の S3Uploader 等）で UniTask に変換している
- `S3UploaderBase.UploadFileCannedACL` の既定値が `PublicRead`。新規アップローダーでは明示的に `Private` へオーバーライドすること（既存2実装とも Private）
- AWS アクセスキー/シークレットが Client 側 Uploader の**ソースコードに直書き**されている。値の転記・ログ出力・外部共有はしない
- `GetObjectList` の prefix 正規化は「セパレータで終わっている場合にさらにセパレータを足す」実装（`prefix.EndsWith(separator)` で `+= separator`）になっており、末尾 `/` 付きで渡すと `//` になる。**prefix は末尾セパレータなしで渡す**のが安全
- `S3Client` の Request 版オーバーロード（GetObjectList / Upload / Put / Delete 系）でも `BucketName` は S3Client 側の値で上書きされる
- バケット等の実配置: バケット `game-dominion`（東京 `APNortheast1`）、ExternalAsset → `assets/`、マスター → `master/`

## 関連

- [ExternalAsset](ExternalAsset.md) — アップロードした配信アセットのランタイム側ダウンロード/管理
- [Master](Master.md) — マスターデータの配信と読み込み
- [Devkit](Devkit.md) — MasterGenerator（マスター生成 → S3 アップロードの流れ）

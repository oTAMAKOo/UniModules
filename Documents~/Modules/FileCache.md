# FileCache

> **namespace**: `Modules.FileCache`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/FileCache/`
> **Client側使用**: 2ファイル（2026-07時点: `InitializeObject.manager.cs`＝初期化 / `CacheManagement.cs`＝全削除）。**Save/Load の実利用箇所は現状なし**（HTML等の配信ファイルキャッシュ用に初期化だけ済んでいる状態）
> **依存**: Extensions（`Singleton`, `AesCryptoKey`, `PathUtility`, ハッシュ/暗号化拡張） / MessagePack / **Modules.LocalData（メタ情報の保存先）**

## 概要

ダウンロードしたファイル（バイト列）を **AES 暗号化 + 有効期限付き** でディスクに保存するキャッシュ基盤。
「source（URL等の元識別子）+ updateAt（サーバー側更新日時）」で鮮度を判定し、期限切れ・更新済みならキャッシュ無効と判定できる。
メモリキャッシュ（[Cache](Cache.md)）/ 永続セーブ（[LocalData](LocalData.md)）との使い分けは Cache.md 冒頭の比較表を参照:

| 基盤 | 保存先 | 寿命 | 主な用途 |
|---|---|---|---|
| [Cache](Cache.md) | メモリ | Dispose / Clear まで | ロード済みアセットの使い回し |
| **FileCache（本モジュール）** | ディスク（暗号化・有効期限付き） | 期限切れまで | ダウンロードしたファイルのキャッシュ |
| [LocalData](LocalData.md) | ディスク（暗号化セーブデータ） | 永続 | セーブデータ・ユーザー設定 |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ダウンロードしたバイト列をキャッシュしたい | `FileCacheManager.Instance.Save(bytes, source, updateAt, expireAt)` |
| キャッシュを読みたい | `FileCacheManager.Instance.Load(source)` |
| 有効なキャッシュがあるか調べたい | `HasCache(source, updateAt)`（存在 + 期限内 + 更新日時一致 + ファイル実在） |
| 期限切れファイルを掃除したい | `CleanExpiredFiles()`（Client ではアプリ初期化時に実行済み） |
| キャッシュを全部消したい | `DirectoryUtility.Clean(fileCacheManager.CacheDirectory)`（`CacheManagement.DeleteAll` の実装） |
| 用途別に独立したキャッシュ置き場を作りたい | `FileCacheManagerBase<TInstance>` を継承した新 Manager を定義 |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `FileCacheManager` | `Singleton`（`FileCacheManagerBase<FileCacheManager>`） | 既定のファイルキャッシュ。`Save` / `Load` を公開する最小実装 |
| `FileCacheManagerBase<TInstance>` | abstract / `Singleton<TInstance>` | 本体。暗号化保存・鮮度判定・期限掃除。継承して用途別 Manager を作れる（保存先はクラス名ハッシュで分離） |
| `CacheData` | `ILocalData` / `[FileName("__FileCache_")]` | キャッシュメタ情報（全ファイルの source / updateAt / expireAt）。**LocalData 基盤に保存される** |
| `CacheFileData` | `[MessagePackObject(true)]` class | 1ファイル分のメタ情報。`Alive()` で期限判定 |

## 使い方(実例)

### 初期化（アプリ起動時に1度。暗号鍵とディレクトリ設定が必須）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
private void InitializeFileCacheManager()
{
    var fileCacheManager = FileCacheManager.Instance;
    var keyFileManager = KeyFileManager.Instance;

    var keyData = keyFileManager.Get(KeyFileManager.KeyType.LocalData);

    var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

    fileCacheManager.SetCryptoKey(cryptoKey);
    fileCacheManager.SetBaseDirectory(PathUtility.Combine(Application.temporaryCachePath, "/Cache/"));

    fileCacheManager.CleanExpiredFiles();
}
```

### キャッシュ全削除（設定画面の「キャッシュ削除」）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Cahce/CacheManagement.cs
var fileCacheManager = FileCacheManager.Instance;

var fileCacheDirectory = fileCacheManager.CacheDirectory;

DirectoryUtility.Clean(fileCacheDirectory);
```

### 保存と読み込み（想定例。実 API に基づく最小コード — Client 側に現役の呼び出しは無い）

```csharp
var fileCacheManager = FileCacheManager.Instance;

// updateAt / expireAt は UnixTime(秒).
if (!fileCacheManager.HasCache(url, updateAt))
{
    var bytes = await DownloadBytes(url);

    fileCacheManager.Save(bytes, url, updateAt, expireAt);
}

var cached = fileCacheManager.Load(url);
```

## API(主要公開メンバー)

### FileCacheManager（`FileCacheManager.Instance`）

| メンバー | 説明 |
|---|---|
| `void Save(byte[] bytes, string source, ulong updateAt, ulong expireAt)` | AES 暗号化してディスク保存し、メタ情報（LocalData）を更新 |
| `byte[] Load(string source)` | 復号して返す。ファイルが無ければ null。**期限チェックはしない**（必要なら先に `HasCache`） |

### FileCacheManagerBase&lt;TInstance&gt;（継承メンバー）

| メンバー | 説明 |
|---|---|
| `void SetCryptoKey(AesCryptoKey cryptoKey)` | 暗号鍵設定。**Save/Load 前に必須** |
| `void SetBaseDirectory(string directory)` | 保存先設定。`<directory>/<クラス名FullNameのハッシュ>/` が実ディレクトリになる。**未設定だと Save は黙って無視 / Load は null** |
| `string CacheDirectory { get; }` | 実際の保存先ディレクトリ |
| `bool HasCache(string source, ulong updateAt)` | メタ情報に存在 + 期限内 + updateAt 一致 + 実ファイル存在、の全条件で true |
| `void CleanExpiredFiles()` | 期限切れファイルを削除しメタ情報を保存し直す |
| `string GetFileName(string source)` | source のハッシュ（実ファイル名） |
| `protected CreateCache / LoadCache` | 派生クラス用の実処理（`Save` / `Load` の実体） |

## 注意点・罠

- **初期化必須**: `SetCryptoKey` と `SetBaseDirectory` を呼ぶ前の Save/Load は例外にならず**黙って失敗**する（Save: no-op / Load: null）。Client では `InitializeObject.manager.cs` が初期化済みなので通常意識不要
- メタ情報（`CacheData`）は **LocalData 基盤に保存**されるため、[LocalData](LocalData.md) 側の初期化（ディレクトリ・暗号鍵）が先に済んでいる必要がある。`CacheData` 型を触る場合は MessagePack コード生成対象になる点も同様（[MessagePack](MessagePack.md)）
- 期限判定 `CacheFileData.Alive()` は **`DateTime.Now`（端末時計）基準**。サーバー時間（`systemModel.LocalTime`）ではないため、端末の時計変更で期限がずれる
- `Load` は期限切れでもファイルが残っていれば返す。鮮度が重要なら **必ず `HasCache(source, updateAt)` → Save/Load** の順で使う
- 保存先は `Application.temporaryCachePath` 配下（Client 設定）。**OS がいつ消しても文句を言えない領域**なので、消えて困るデータは [LocalData](LocalData.md) へ
- ファイル名・メタ情報とも source はハッシュ化される。ディスク上から元 URL は辿れない（デバッグ時はメタ情報の `CacheData.files` を見る）
- 暗号鍵は Client では LocalData と同じ鍵（`KeyFileManager.KeyType.LocalData`）を使用している

## 関連

- [Cache](Cache.md) — メモリキャッシュ。使い分け比較表あり
- [LocalData](LocalData.md) — メタ情報（`CacheData`）の保存基盤 / 永続データ
- [MessagePack](MessagePack.md) — `CacheData` / `CacheFileData` のシリアライズ
- [Crypto](Crypto.md) — `AesCryptoKey`

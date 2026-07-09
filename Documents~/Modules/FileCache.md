# FileCache

> **namespace**: `Modules.FileCache`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/FileCache/`
> **依存**: Extensions（`Singleton`, `AesCryptoKey`, `PathUtility`, ハッシュ/暗号化拡張） / MessagePack / **Modules.LocalData（メタ情報の保存先）**

## 概要

ダウンロードしたファイル（バイト列）を **AES 暗号化 + 有効期限付き** でディスクに保存するキャッシュ基盤。
「source（URL等の元識別子）+ updateAt（サーバー側更新日時）」で鮮度を判定し、期限切れ・更新済みならキャッシュ無効と判定できる。
主要クラス: `FileCacheManager`（既定のファイルキャッシュ Singleton。`Save` / `Load` を公開する最小実装）/ `FileCacheManagerBase<TInstance>`（本体。暗号化保存・鮮度判定・期限掃除。継承して用途別 Manager を作れる。保存先はクラス名ハッシュで分離）/ `CacheData`・`CacheFileData`（キャッシュメタ情報。**LocalData 基盤に保存される**。`CacheFileData.Alive()` で期限判定）。

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
| 期限切れファイルを掃除したい | `CleanExpiredFiles()` |
| キャッシュを全部消したい | `DirectoryUtility.Clean(fileCacheManager.CacheDirectory)` |
| 用途別に独立したキャッシュ置き場を作りたい | `FileCacheManagerBase<TInstance>` を継承した新 Manager を定義 |

## 使い方

- 初期化（アプリ起動時に1度。暗号鍵とディレクトリ設定が必須）: `SetCryptoKey(new AesCryptoKey(keyData.Key, keyData.Iv))` → `SetBaseDirectory(Application.temporaryCachePath + "/Cache/")` → `CleanExpiredFiles()`
- キャッシュ全削除: `DirectoryUtility.Clean(fileCacheManager.CacheDirectory)`
- 保存と読み込み: `HasCache(source, updateAt)` で鮮度確認 → 無ければダウンロードして `Save(bytes, source, updateAt, expireAt)` → `Load(source)`。updateAt / expireAt は UnixTime（秒）

## 注意点・罠

- **初期化必須**: `SetCryptoKey` と `SetBaseDirectory` を呼ぶ前の Save/Load は例外にならず**黙って失敗**する（Save: no-op / Load: null）
- メタ情報（`CacheData`）は **LocalData 基盤に保存**されるため、[LocalData](LocalData.md) 側の初期化（ディレクトリ・暗号鍵）が先に済んでいる必要がある。`CacheData` 型を触る場合は MessagePack コード生成対象になる点も同様（[MessagePack](MessagePack.md)）
- 期限判定 `CacheFileData.Alive()` は **`DateTime.Now`（端末時計）基準**。サーバー時間ではないため、端末の時計変更で期限がずれる
- `Load` は期限切れでもファイルが残っていれば返す（期限チェックはしない）。鮮度が重要なら **必ず `HasCache(source, updateAt)` → Save/Load** の順で使う
- 保存先は利用側の指定次第だが、`Application.temporaryCachePath` 配下は **OS がいつ消しても文句を言えない領域**なので、消えて困るデータは [LocalData](LocalData.md) へ
- ファイル名・メタ情報とも source はハッシュ化される。ディスク上から元 URL は辿れない（デバッグ時はメタ情報の `CacheData.files` を見る）

## 関連

- [Cache](Cache.md) — メモリキャッシュ。使い分け比較表あり
- [LocalData](LocalData.md) — メタ情報（`CacheData`）の保存基盤 / 永続データ
- [MessagePack](MessagePack.md) — `CacheData` / `CacheFileData` のシリアライズ
- [Crypto](Crypto.md) — `AesCryptoKey`

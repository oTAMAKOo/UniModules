# FileCache

ファイルをディスクにキャッシュする機能。
バイト列を暗号化して保存し、更新日時と有効期限でキャッシュが使えるかを判定できる。
暗号鍵と保存先の設定、LocalData の初期化が済んでいる必要がある。

## 基本情報

> **namespace**: `Modules.FileCache`
> **場所**: `Modules/FileCache/`
> **依存**: Extensions（`Singleton`, `AesCryptoKey`, `PathUtility`, ハッシュ/暗号化拡張） / MessagePack / **Modules.LocalData（メタ情報の保存先）**

## 概要

ダウンロードしたファイル（バイト列）を **AES 暗号化 + 有効期限付き** でディスクに保存するキャッシュ基盤。
「source（URL等の元識別子）+ updateAt（サーバー側更新日時）」で鮮度を判定し、期限切れ・更新済みならキャッシュ無効と判定できる。
主要クラス: `FileCacheManager`（既定のファイルキャッシュ Singleton。`Save` / `Load` を公開する最小実装）/ `FileCacheManagerBase<TInstance>`（本体。暗号化保存・鮮度判定・期限掃除。継承して用途別 Manager を作れる。保存先はクラス名ハッシュで分離）/ `CacheData`・`CacheFileData`（キャッシュメタ情報。**LocalData 基盤に保存される**。`CacheFileData.Alive()` で期限判定）。

| 基盤 | 保存先 | 寿命 | 主な用途 |
|---|---|---|---|
| [Cache](../Cache/Manual.md) | メモリ | Dispose / Clear まで | ロード済みアセットの使い回し |
| **FileCache（本モジュール）** | ディスク（暗号化・有効期限付き） | 期限切れまで | ダウンロードしたファイルのキャッシュ |
| [LocalData](../LocalData/Manual.md) | ディスク（暗号化セーブデータ） | 永続 | セーブデータ・ユーザー設定 |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ダウンロードしたバイト列をキャッシュしたい | `FileCacheManager.Instance.Save(bytes, source, updateAt, expireAt)` |
| キャッシュを読みたい | `FileCacheManager.Instance.Load(source)` |
| 有効なキャッシュがあるか調べたい | `HasCache(source, updateAt)`（存在 + 期限内 + 更新日時一致 + ファイル実在） |
| 期限切れファイルを掃除したい | `CleanExpiredFiles()` |
| source に対応するキャッシュのファイル名を知りたい | `GetFileName(source)`（source のハッシュ） |
| キャッシュを全部消したい | `DirectoryUtility.Clean(fileCacheManager.CacheDirectory)` |
| 用途別に独立したキャッシュ置き場を作りたい | `FileCacheManagerBase<TInstance>` を継承した新 Manager を定義 |

## 使い方

- 初期化（アプリ起動時に1度。暗号鍵とディレクトリ設定が必須）: `SetCryptoKey(new AesCryptoKey(keyData.Key, keyData.Iv))` → `SetBaseDirectory(Application.temporaryCachePath + "/Cache/")` → `CleanExpiredFiles()`
- キャッシュ全削除: `DirectoryUtility.Clean(fileCacheManager.CacheDirectory)`
- 保存と読み込み: `HasCache(source, updateAt)` で鮮度確認 → 無ければダウンロードして `Save(bytes, source, updateAt, expireAt)` → `Load(source)`。updateAt / expireAt は UnixTime（秒）

## 注意点・罠

- **初期化必須**: `SetBaseDirectory` を呼ぶ前の Save/Load は例外にならず**黙って失敗**する（Save: no-op / Load: null）。`SetBaseDirectory` 済みで `SetCryptoKey` が未設定のときは、Save と（ファイルがある場合の）Load が暗号化・復号で例外になる
- 保存先は `SetBaseDirectory` に渡したパスの下の、Manager のクラス名（FullName）のハッシュのフォルダ（`CacheDirectory`）。`FileCacheManagerBase` の `CreateCache` / `LoadCache` は protected なので、継承クラスで `Save` / `Load` 相当の公開メソッドを用意する
- 空のバイト列は保存されない（`Save` は no-op）
- メタ情報（`CacheData`）は Manager の種類によらず 1 つを共有する。複数の Manager で同じ source を使うと、更新日時・有効期限の情報が上書きし合う
- メタ情報（`CacheData`）は **LocalData 基盤に保存**されるため、[LocalData](../LocalData/Manual.md) 側の初期化（ディレクトリ・暗号鍵）が先に済んでいる必要がある。`CacheData` 型を触る場合は MessagePack コード生成対象になる点も同様（[MessagePack](../MessagePack/Manual.md)）
- 期限判定 `CacheFileData.Alive()` は **`DateTime.Now`（端末時計）基準**。サーバー時間ではないため、端末の時計変更で期限がずれる
- `Load` は期限切れでもファイルが残っていれば返す（期限チェックはしない）。鮮度が重要なら **必ず `HasCache(source, updateAt)` → Save/Load** の順で使う
- 保存先は利用側の指定次第だが、`Application.temporaryCachePath` 配下は **OS がいつ消しても文句を言えない領域**なので、消えて困るデータは [LocalData](../LocalData/Manual.md) へ
- キャッシュのファイル名は source のハッシュで、ファイル名から元 URL は辿れない。メタ情報の `CacheFileData.Source` には source がそのまま入る（デバッグ時は `CacheData.files` を見る）
- `CleanExpiredFiles()` が消すのは、メタ情報に載っていて期限が切れたファイルだけ。メタ情報に無いファイルは残る

## 関連

- [Cache](../Cache/Manual.md) — メモリキャッシュ。使い分け比較表あり
- [LocalData](../LocalData/Manual.md) — メタ情報（`CacheData`）の保存基盤 / 永続データ
- [MessagePack](../MessagePack/Manual.md) — `CacheData` / `CacheFileData` のシリアライズ
- [Crypto](../Crypto/Manual.md) — `AesCryptoKey`

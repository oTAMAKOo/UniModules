# Prefs

PlayerPrefs を暗号化して使う機能。
フラグ・数値・文字列・日時などの小さな値を、キー名と値の両方を暗号化して端末に保存できる。

## 基本情報

> **namespace**: `Extensions`（**`Modules.Prefs` ではない**。フォルダ名と不一致）
> **場所**: `Modules/Prefs/SecurePrefs.cs`（1ファイルのみ）
> **依存**: UnityEngine（PlayerPrefs） / Newtonsoft.Json / Extensions（`AesCryptoKey`, `string.Encrypt/Decrypt` 拡張）

## 概要

`PlayerPrefs` の**暗号化ラッパー** `SecurePrefs`（static。唯一のクラス）。キー名・値の両方を AES 暗号化して保存するため、端末上で中身を覗かれても読めない。
起動時に `SetCryptoKey()` で鍵を設定し、以降は各レイヤー（利用側や基盤の ExternalAssets・Notifications・Devkit.Diagnosis、SecureStorage の非対応環境向けの保存先 等）が軽量なキー値の永続化に使う。

### ストレージ使い分け（[Cache](../Cache/Manual.md) 冒頭の比較表と対応）

| 基盤 | 保存先 | 寿命 | 主な用途 |
|---|---|---|---|
| [Cache](../Cache/Manual.md)（`Modules.Cache`） | メモリ | アプリ終了まで | ロード済みアセットの使い回し |
| [FileCache](../FileCache/Manual.md)（`Modules.FileCache`） | ディスク（暗号化・有効期限付き） | 期限切れまで | ダウンロードしたファイルのキャッシュ |
| [LocalData](../LocalData/Manual.md)（`Modules.LocalData`） | ディスク（暗号化セーブデータ） | 永続 | セーブデータ・ユーザー設定 |
| **SecurePrefs（本モジュール）** | PlayerPrefs（キー・値とも AES 暗号化） | 永続 | 軽量なキー値（フラグ・日時・設定値） |
| [SecureStorage](../SecureStorage/Manual.md)（`Modules.SecureStorage`） | iOS Keychain / Android Keystore で暗号化したファイル | 永続 | OS が鍵を管理する秘密の値 |

※ namespace は `Extensions`（using 追加は不要なことが多い）。ストレージの使い分けは [Cache](../Cache/Manual.md) の表を参照。

### `ProjectPrefs`（`Modules.Devkit.Prefs`・[Devkit](../Devkit/Manual.md)）との違い

| | SecurePrefs（本モジュール） | ProjectPrefs |
|---|---|---|
| 用途 | **ランタイム**の永続キー値 | **エディタ専用**の設定保存（`#if UNITY_EDITOR`） |
| 保存先 | PlayerPrefs | EditorPrefs（プロジェクト識別子付きキー） |
| 暗号化 | あり（AES・キー名も暗号化） | なし |
| 実機 | 動作する | 参照するとコンパイルエラー |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| フラグ・数値・文字列を永続化したい | `SecurePrefs.SetBool / SetInt / SetFloat / SetString` |
| 日時・色・Enum を永続化したい | `SecurePrefs.SetDateTime / SetColor / SetEnum` |
| 任意クラスを JSON で永続化したい | `SecurePrefs.Set<T>(key, value)` / `Get<T>(key)` |
| 存在確認・削除したい | `HasKey(key)` / `DeleteKey(key)` / `DeleteAll()` |
| すぐにディスクへ書き出したい | `SecurePrefs.Save()`（`PlayerPrefs.Save()`） |
| 暗号鍵を差し替えたい（起動時初期化） | `SecurePrefs.SetCryptoKey(aesCryptoKey)` |
| すべてのキー名に共通の接頭辞を付けたい | `SecurePrefs.SetKeyPrefix(prefix)`（キー名が `prefix-キー名` になる） |

## 使い方

- 起動時の鍵初期化: `KeyFileManager<TInstance, TKeyType>` を継承した Manager の `Get(keyType)` で得た `KeyData`（`Key` / `Iv`）から `AesCryptoKey` を生成し `SecurePrefs.SetCryptoKey()` を呼ぶ（[Crypto](../Crypto/Manual.md)）
- ネスト static class `Prefs` パターン（基盤内の慣例）: 利用クラス内に `private static class Prefs` を定義し、プロパティ get/set で `SecurePrefs.GetXxx / SetXxx` を包む。キー名は `typeof(Prefs).FullName + "-項目名"` で衝突を回避するのが慣例。実例: `Modules/ExternalAssets/ExternalAsset.cache.cs`

## 注意点・罠

- **`DeleteAll()` は `PlayerPrefs.DeleteAll()` を直接呼ぶ**。SecurePrefs 管轄外の生 PlayerPrefs（Unity・プラグインが書いたもの）も全消しになる
- **鍵を変えると既存データは実質消える**: キー名自体を暗号化して PlayerPrefs キーにするため、鍵が変わると `HasKey` が false になり defaultValue が返る（例外にはならない）
- `SetCryptoKey` 前に読み書きするとデフォルト鍵（ソースにハードコード）で動く。**書き込みが初期化より先行しないよう注意**（起動シーケンスの最初期に設定するのが原則）
- int / float / DateTime も文字列として保存され、`Parse` はカルチャ依存（InvariantCulture 未指定）。端末言語で日時・小数の書式が変わる環境をまたぐと復元に失敗して defaultValue になりうる
- **`SetKeyPrefix` を途中で変えると既存データが読めなくなる**（接頭辞込みのキー名を暗号化するため、鍵の変更と同じく別のキーになる）。設定するなら読み書きより前に 1 回だけ
- `Get<T>` は保存した JSON を `JsonConvert.DeserializeObject<T>` で戻す。型が合わない JSON では例外になる（ほかの `GetXxx` と違い defaultValue に落ちない）
- `GetEnum<T>` は**名前**保存。Enum メンバーのリネームで復元不能（defaultValue）になる
- 暗号化のオーバーヘッドがあるため、大きなデータは [LocalData](../LocalData/Manual.md)、ファイルは [FileCache](../FileCache/Manual.md) を使う
- エディタ設定の保存に使わない（それは `ProjectPrefs`。[Devkit](../Devkit/Manual.md) 参照）

## 関連

- [Cache](../Cache/Manual.md) / [FileCache](../FileCache/Manual.md) / [LocalData](../LocalData/Manual.md) — ストレージ使い分け（上表）
- [Devkit](../Devkit/Manual.md) — エディタ専用の `ProjectPrefs`（`Modules.Devkit.Prefs`）、`Diagnosis` の設定保存が SecurePrefs 利用
- [Crypto](../Crypto/Manual.md) — `AesCryptoKey` / `Encrypt` / `Decrypt` 拡張の本体
- [Notifications](../Notifications/Manual.md) — `LocalPushNotification` が通知IDの永続化に SecurePrefs 利用

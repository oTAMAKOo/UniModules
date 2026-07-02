# Prefs（SecurePrefs）

> **namespace**: `Extensions`（**`Modules.Prefs` ではない**。フォルダ名と不一致）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Prefs/SecurePrefs.cs`（1ファイルのみ）
> **Client側使用**: 2ファイル + 基盤内4ファイル（2026-07時点・使用中）
> **依存**: UnityEngine（PlayerPrefs） / Newtonsoft.Json / Extensions（`AesCryptoKey`, `string.Encrypt/Decrypt` 拡張）

## 概要

`PlayerPrefs` の**暗号化ラッパー** `SecurePrefs`（static）。キー名・値の両方を AES 暗号化して保存するため、端末上で中身を覗かれても読めない。
アプリ起動時に `KeyFileManager` 由来の鍵で初期化され（`InitializeObject.core.cs`）、以降は全レイヤー（Client / 基盤の ExternalAsset・Notifications・Devkit.Diagnosis）が軽量なキー値の永続化に使う。

### ストレージ使い分け（[Cache](Cache.md) 冒頭の比較表と対応）

| 基盤 | 保存先 | 寿命 | 主な用途 |
|---|---|---|---|
| [Cache](Cache.md)（`Modules.Cache`） | メモリ | アプリ終了まで | ロード済みアセットの使い回し |
| [FileCache](FileCache.md)（`Modules.FileCache`） | ディスク（暗号化・有効期限付き） | 期限切れまで | ダウンロードしたファイルのキャッシュ |
| [LocalData](LocalData.md)（`Modules.LocalData`） | ディスク（暗号化セーブデータ） | 永続 | セーブデータ・ユーザー設定 |
| **SecurePrefs（本モジュール）** | PlayerPrefs（キー・値とも AES 暗号化） | 永続 | 軽量なキー値（フラグ・日時・設定値） |

※ Cache.md の表では `Modules.Prefs` と表記されているが、実際の namespace は `Extensions`（using 追加は不要なことが多い）。

### `ProjectPrefs`（`Modules.Devkit.Prefs`・[Devkit](Devkit.md)）との違い

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
| 存在確認・削除したい | `HasKey(key)` / `DeleteKey(key)` |
| 暗号鍵を差し替えたい（起動時初期化） | `SecurePrefs.SetCryptoKey(aesCryptoKey)` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `SecurePrefs` | static class | 唯一のクラス。全 API を提供 |

## 使い方(実例)

### 起動時の鍵初期化（必須フロー・実装済み）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
private void InitializeSecurePrefs()
{
    var keyData = KeyFileManager.Instance.Get(KeyFileManager.KeyType.SecurePrefs);

    var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

    SecurePrefs.SetCryptoKey(cryptoKey);
}
```

### ネスト static class `Prefs` パターン（基盤内の慣例）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/ExternalAsset/ExternalAsset.cache.cs
private static class Prefs
{
    public static DateTime deleteUnUsedCacheTime
    {
        get { return SecurePrefs.GetDateTime(typeof(Prefs).FullName + "-deleteUnUsedCacheTime", null); }
        set { SecurePrefs.SetDateTime(typeof(Prefs).FullName + "-deleteUnUsedCacheTime", value); }
    }
}
```

キー名は `typeof(Prefs).FullName + "-項目名"` で衝突を回避するのが慣例（Devkit の `Diagnosis` / `LocalPushNotification` も同パターン）。

## API(主要公開メンバー)

### SecurePrefs（static）

| メンバー | 説明 |
|---|---|
| `void SetCryptoKey(AesCryptoKey)` | 暗号鍵設定。未設定時はソース埋め込みのデフォルト鍵を使用 |
| `void SetKeyPrefix(string)` | 全キー名に付く接頭辞（`"prefix-keyName"`）を設定 |
| `bool HasKey(string)` / `void DeleteKey(string)` | 存在確認 / 個別削除 |
| `void DeleteAll()` | **PlayerPrefs.DeleteAll そのもの**（下記罠参照） |
| `void Save()` | `PlayerPrefs.Save()`（即時フラッシュ） |
| `Set/GetString(key, value / defaultValue = "")` | 文字列（値は AES + Base64 で保存） |
| `Set/GetInt(key, ...)` / `Set/GetFloat(key, ...)` / `Set/GetBool(key, ...)` | 数値・フラグ（内部は全て文字列保存。パース失敗時は defaultValue） |
| `Set/GetDateTime(key, ...)` | 日時（`DateTime.ToString()` / `Parse`） |
| `Set/GetColor(key, ...)` | Color（"r,g,b,a" 文字列） |
| `SetEnum(key, Enum)` / `GetEnum<T>(key, default)` | Enum 名文字列で保存（名前一致で復元） |
| `Set<T>(key, value)` / `Get<T>(key, default)` | 任意型を JsonConvert でシリアライズして保存 |

## 注意点・罠

- **`DeleteAll()` は `PlayerPrefs.DeleteAll()` を直接呼ぶ**。SecurePrefs 管轄外の生 PlayerPrefs（Unity・プラグインが書いたもの）も全消しになる
- **鍵を変えると既存データは実質消える**: キー名自体を暗号化して PlayerPrefs キーにするため、鍵が変わると `HasKey` が false になり defaultValue が返る（例外にはならない）
- `SetCryptoKey` 前に読み書きするとデフォルト鍵（ソースにハードコード）で動く。**書き込みが初期化より先行しないよう注意**（正規フローでは `InitializeObject` が最初期に設定）
- int / float / DateTime も文字列として保存され、`Parse` はカルチャ依存（InvariantCulture 未指定）。端末言語で日時・小数の書式が変わる環境をまたぐと復元に失敗して defaultValue になりうる
- `GetEnum<T>` は**名前**保存。Enum メンバーのリネームで復元不能（defaultValue）になる
- 暗号化のオーバーヘッドがあるため、大きなデータは [LocalData](LocalData.md)、ファイルは [FileCache](FileCache.md) を使う
- エディタ設定の保存に使わない（それは `ProjectPrefs`。[Devkit](Devkit.md) 参照）

## 関連

- [Cache](Cache.md) / [FileCache](FileCache.md) / [LocalData](LocalData.md) — ストレージ使い分け（上表）
- [Devkit](Devkit.md) — エディタ専用の `ProjectPrefs`（`Modules.Devkit.Prefs`）、`Diagnosis` の設定保存が SecurePrefs 利用
- [Crypto](Crypto.md) — `AesCryptoKey` / `Encrypt` / `Decrypt` 拡張の本体
- [Notifications](Notifications.md) — `LocalPushNotification` が通知IDの永続化に SecurePrefs 利用

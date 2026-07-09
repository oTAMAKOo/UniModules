# Crypto

> **namespace**: `Modules.Crypto`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Crypto/`
> **依存**: UniTask / Extensions（Singleton, AesCryptoKey, AESExtension, PathUtility, UnityPathUtility, AndroidUtility）/ Modules.Devkit（Editor: KeyFileWindow）

## 概要

AES 鍵（Key/Iv）を**暗号化キーファイル**として StreamingAssets に同梱し、実行時にロード・キャッシュして各サブシステムへ供給する鍵管理基盤。
暗号化・復号の処理そのものは `Extensions.AESExtension`（`str.Encrypt(aesKey)` 等）が担い、本モジュールの責務は「**鍵をコードに直書きしないための鍵の保管・配布**」。
利用側は `KeyFileManager<TInstance, TKeyType>` を継承し、用途別 KeyType enum・保存ディレクトリ・エンコード方式を定義する。
主要クラス: `KeyFileManager<TInstance, TKeyType>`（abstract Singleton 本体） / `KeyData`（復号済み鍵ペア Key/Iv） / `KeyFileWindow`（エディタ専用生成ウィンドウ）。

## AESExtension（Extensions）との使い分け

| | **Modules.Crypto（KeyFileManager）** | **Extensions.AESExtension / AesCryptoKey** |
|---|---|---|
| 役割 | 鍵（Key/Iv）の永続化・ロード・キャッシュ | 実際の AES-256/CBC 暗号化・復号処理 |
| 使い方 | `Get(KeyType.Xxx)` で用途別の鍵を引く | `KeyData` から `new AesCryptoKey(key, iv)` を作り `Encrypt`/`Decrypt` 拡張へ渡す |
| 鍵の出所 | StreamingAssets 配下の暗号化ファイル（KeyFileWindow で生成・コミット） | 呼び出し側が用意（パスワード派生 or key+iv 直指定） |

典型パターン:

```csharp
var keyData = KeyFileManager.Instance.Get(KeyFileManager.KeyType.Xxx);

var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

// 以降は AESExtension（str.Encrypt(cryptoKey) 等）や各モジュールの SetCryptoKey へ.
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 用途別の AES 鍵を取得したい | `KeyFileManager.Instance.Get(KeyFileManager.KeyType.Xxx)` → `new AesCryptoKey(keyData.Key, keyData.Iv)` |
| 全鍵を一括ロードしたい | `await KeyFileManager.Instance.Load()` |
| 1種類だけロードしたい | `await KeyFileManager.Instance.LoadKeyFile(keyType)` |
| 新しい鍵種別を追加したい | 派生クラスの `KeyType` enum に追加 → KeyFileWindow で生成 |
| 鍵ファイルを生成・更新したい | メニュー `Extension/Tools/Open KeyFileWindow`（Key 32文字 / Iv 16文字） |
| 鍵ファイルの配置先を知りたい | `KeyFileManager.Instance.GetLoadPath(keyType)`（`StreamingAssets/<FileDirectory>/{enum名のハッシュ}`） |
| 文字列・byte[] を暗号化/復号したい | `AESExtension`（[../Extensions/Methods.md](../Extensions/Methods.md)。本モジュールではない） |

## 使い方

- 派生クラスで KeyType enum・`FileDirectory`・`Separator`・`CustomEncode/Decode` を定義する
- 起動時に `await KeyFileManager.Instance.Load()` で一括ロードし、以降は `Get(keyType)` で鍵を引くだけの運用が典型

## 注意点・罠

- **`Get` の前に `Load` 必須**: 実行時は起動シーケンスで一括ロードするのが基本。エディタツール・テストでは都度 `await KeyFileManager.Instance.Load()` を呼ぶ（`SingletonManager.ReleaseAll()` 後も再ロードが必要）。未ロードだと `Get` は null を返し NullReference の温床になる
- **鍵ファイルは StreamingAssets 同梱・git 管理**: ファイル名は enum 名のハッシュ。ビルドに同梱されるため「秘匿」ではなく「難読化」である点を理解して使う
- **Key 32 文字 / Iv 16 文字固定**: `AesCryptoKey(key, iv)` が UTF8 バイト列をそのまま AES-256/CBC の Key/IV にするため。KeyFileWindow がバリデーションする（32/16 文字以外は入力を弾く）
- **`LoadKeyFile` はファイルが無くても例外を出さない**: 黙ってスキップされ、`Get` が null になって初めて気付く。新 KeyType 追加時は KeyFileWindow での生成を忘れない
- **ファイル破損・形式不一致は `InvalidDataException`**: `Separator` や `CustomEncode/Decode` の実装を変更すると既存ファイルが読めなくなる（全ファイル再生成が必要）
- **enum 名の変更 = ファイル名の変更**: `GetLoadPath` が enum 名のハッシュを使うため、`KeyType` のリネームで既存ファイルは参照不能になる
- **Android は初回ロードにコピー I/O**: StreamingAssets が WebRequest 経由でしか読めないため `AndroidUtility.CopyStreamingToTemporary` を挟む（`#if UNITY_ANDROID && !UNITY_EDITOR`）
- **`Load()` は `UnityPathUtility.Initialized` を待つ**: メインスレッド初期化前に await すると進まない可能性がある。通常の起動フローでは問題にならない
- **暗号処理を自前実装しない**: 暗号化・復号は `AESExtension`（`Encrypt`/`Decrypt` 拡張）を使う。本モジュールは鍵の供給のみ

## 関連

- [../Extensions/Methods.md](../Extensions/Methods.md) — `AESExtension` / `AesCryptoKey`（実際の暗号化・復号）、`GetHash`
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（AES 対応ファイル IO）
- [Bugsnag](Bugsnag.md) — ApiKey ファイル復号に利用可
- [TextData](TextData.md) — テキスト暗号キーの供給元として利用可
- [LocalData](LocalData.md) — セーブデータ暗号キーの供給元として利用可
- [Master](Master.md) — マスター暗号キーの供給元として利用可

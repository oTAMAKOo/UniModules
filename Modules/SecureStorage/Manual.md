# SecureStorage

OS が鍵を管理する領域に値を保存する機能。
文字列・バイト列・任意のクラスを、iOS は Keychain、Android は Keystore の鍵で暗号化して読み書きできる。
有効化シンボルの定義が必要で、エディタとその他の環境では安全な保存にならない。

## 基本情報

> **namespace**: `Modules.SecureStorage`
> **場所**: `Modules/SecureStorage/`（ネイティブ: `Plugins/iOS/SecureStorage.mm` / `Plugins/Android/SecureStorage.java`、ビルドに含める判定: `Editor/SecureStoragePluginFilter.cs`）
> **依存**: UnityEngine / Newtonsoft.Json / Extensions（`SecurePrefs`。エディタ・その他の環境の保存先）
> **有効化シンボル**: `ENABLE_SECURE_STORAGE`（`csc.rsp` / Scripting Define Symbols のどちらでもよい。下記「有効化」）

## 概要

OS の安全な保存領域にキー値を保存する static クラス `SecureStorage`（ランタイムの唯一の公開クラス。ほかにエディタ用の `SecureStoragePluginFilter` がある）。
保存先は環境ごとに切り替わり、呼び出し側は同じ API で読み書きする。

| 環境 | 保存先 | アプリ削除後 |
|---|---|---|
| iOS | Keychain の汎用パスワード項目（`kSecClassGenericPassword`。service = 区分名、account = キー）。`kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly`（端末の初回ロック解除後に読める・別端末へ移さない・iCloud 同期なし） | **残る**（OS の仕様。保証はされない） |
| Android | Keystore の AES-256-GCM 鍵（区分名ごとに 1 つ）で暗号化し、`noBackupFilesDir/SecureStorage/<区分名の SHA-256>/<キーの SHA-256>` にファイルで保存（先頭 12 バイトが IV、続けて暗号文）。キーを AAD に使い、ファイルの入れ替えでは復号できない | 消える（鍵もファイルも消える） |
| エディタ・その他 | `SecurePrefs`（鍵がアプリに入っているため、安全な保存ではない）。エディタ以外では初回に警告ログを出す | — |

`SecurePrefs` との違い: `SecurePrefs` はアプリに同梱した鍵で暗号化する（難読化）。本モジュールは鍵を OS が管理するため、アプリを解析しても鍵を取り出せない。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 文字列を保存・読み込みしたい | `SetString(key, value)` / `GetString(key, defaultValue)` / `TryGetString(key, out value)` |
| バイト列を保存・読み込みしたい | `SetBytes` / `GetBytes` / `TryGetBytes` |
| 任意のクラスを JSON で保存したい | `Set<T>(key, value)` / `Get<T>(key, defaultValue)` / `TryGet<T>(key, out value)` |
| 存在確認・削除したい | `HasKey(key)` / `DeleteKey(key)` |
| この区分名の値を全部消したい | `DeleteAll()`（今の区分名の値だけ。iOS は service が同じ汎用パスワード項目をすべて消す。Android は鍵も消す） |
| 保存先を用途ごとに分けたい | `SetServiceName(name)`（既定は `"SecureStorage"`）。今の値は `ServiceName` |
| 再インストール後に iOS の値を引き継がない | 起動時に `ClearOnFreshInstall()` |

## 使い方

- 書き込み（`Set*` / `DeleteKey` / `DeleteAll`）は成否を `bool` で返す。失敗時は `Debug.LogError`（Android は logcat にも）を出す
- 読み込みは「値が無い」「読めない」のどちらも `false` / `defaultValue`。読めない理由はログで確認する。`TryGet<T>` / `Get<T>` は JSON を戻せないときも `Debug.LogError` を出して `false` / `defaultValue`
- 引数の誤りは例外になる: キーが null・空文字は `ArgumentException`、`SetBytes` / `SetString` の値が null は `ArgumentNullException`、`SetServiceName` の null・空文字は `ArgumentException`
- `HasKey` は値を実際に読んで判定する（`TryGetBytes` と同じ処理）
- 区分名を変えるときは、最初の読み書きより前に `SetServiceName` を呼ぶ
- `ClearOnFreshInstall()` は PlayerPrefs（暗号化なし）に印 `SecureStorage-Installed-<区分名>` を置き、印が無いときだけ `DeleteAll()` する。削除に失敗したときは印を置かず、次の呼び出しでもう一度消す。iOS はアプリ削除で PlayerPrefs が消え Keychain が残るため、この呼び出しで Android と同じ「再インストールで消える」挙動になる

## 有効化

1. `ENABLE_SECURE_STORAGE` を定義する（`csc.rsp` の `-define` か、Player Settings の Scripting Define Symbols）
2. Android のコード縮小（Minify）を有効にしているときは、カスタム ProGuard ファイルに `-keep class com.unimodules.securestorage.** { *; }` を足す（C# から名前で呼ぶため、縮小で消える・名前が変わる）

シンボルが無い環境では C# がコンパイルされず、`.mm` / `.java` もビルドに入らない。

- `.mm` / `.java` をビルドに含めるかは `SecureStoragePluginFilter`（エディタ用）が `PluginImporter.SetIncludeInBuildDelegate` で決める。判定はこのスクリプトのコンパイル時のシンボルで決まるので、`csc.rsp` の定義も効く
- `.meta` の Define Constraints は使わない（Define Constraints の判定には `csc.rsp` の定義が含まれない）
- 判定はアセットのパスに登録され、スクリプトの読み込み（`[InitializeOnLoadMethod]`）のたびに登録し直す。`.mm` / `.java` は GUID で探すので、フォルダを移動してもよい
- 判定が登録されていないとき（`SecureStoragePluginFilter` を消した等）は、シンボルが無くても `.mm` / `.java` がビルドに入る（呼ぶ C# が無いので動作には影響しない）

## 注意点・罠

- **メインスレッドから呼ぶ**（Android は JNI 経由で呼ぶ。別スレッドからの呼び出しは考慮していない）
- 動作環境: iOS は Security.framework（`.meta` で依存を指定済み）・ARC（`-fobjc-arc`）。Android は API 23 以上（`KeyGenParameterSpec`）
- **iOS は端末の初回ロック解除前（再起動直後のバックグラウンド起動など）に読み書きできない**。失敗として `false` が返る
- **Android の値はアプリ削除・データ消去で消える**。端末間の引き継ぎ・再インストール後の復元には使えない
- Android で鍵が無効になった値（鍵の消失・IV の長さに満たないファイル・認証タグの不一致・`KeyPermanentlyInvalidatedException`）は、読み込み時に削除して「値が無い」として扱う。Keystore の一時的なエラーでは削除しない
- iOS で同じ区分名・キーに書くと上書きする（`SecItemUpdate` → 無ければ `SecItemAdd`）
- 大きなデータには使わない（1 回の読み書きごとに Keychain / Keystore を呼ぶ）。大きいセーブデータは [LocalData](../LocalData/Manual.md)
- エディタの保存先は `SecurePrefs` なので、`SecurePrefs.DeleteAll()`（PlayerPrefs 全消し）で消える

## 関連

- [Prefs](../Prefs/Manual.md) — `SecurePrefs`（アプリ同梱の鍵で暗号化する軽量キー値。エディタでの保存先）
- [Crypto](../Crypto/Manual.md) — アプリ同梱の AES 鍵の管理
- [LocalData](../LocalData/Manual.md) — 暗号化セーブデータ

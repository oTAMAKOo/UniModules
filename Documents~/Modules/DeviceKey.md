# DeviceKey

> **namespace**: `Modules.DeviceKey`
> **場所**: `Modules/DeviceKey/`（ネイティブ: `Plugins/iOS/DeviceKey.mm` / `Plugins/Android/DeviceKey.java`、ビルドに含める判定: `Editor/DeviceKeyPluginFilter.cs`）
> **依存**: UnityEngine のみ
> **有効化シンボル**: `ENABLE_DEVICE_KEY`（`csc.rsp` / Scripting Define Symbols のどちらでもよい）

## 概要

端末のセキュリティチップに P-256 の EC 鍵を作り、秘密鍵を外に出さずに ECDSA（SHA-256）の署名を作る static クラス `DeviceKey`（唯一の公開クラス）。
C# に返るのは公開鍵と署名のバイト列だけ。通信内容に署名を付け、サーバーが登録済みの公開鍵で検証する用途を想定する（サーバー側の検証は本モジュールの範囲外）。

| 環境 | 鍵の場所 | 署名 | アプリ削除後 |
|---|---|---|---|
| iOS | Secure Enclave（`kSecAttrTokenIDSecureEnclave`）。`kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly` + `kSecAccessControlPrivateKeyUsage`。シミュレーターは Secure Enclave が無いためソフトウェアの鍵 | `SecKeyCreateSignature`（`kSecKeyAlgorithmECDSASignatureMessageX962SHA256`） | 残る（Keychain の項目。保証はされない） |
| Android | AndroidKeyStore（`PURPOSE_SIGN`・`secp256r1`・`DIGEST_SHA256`。TEE、端末によってはソフトウェア） | `Signature("SHA256withECDSA")` | 消える |
| エディタ・その他 | 対応しない（`IsSupported` が false。鍵を作れない） | — | — |

両 OS とも出力の形式は同じ:

- **公開鍵**: X.509 SubjectPublicKeyInfo（DER・91 バイト）。iOS は X9.63（65 バイト）に固定の前置きを付けて変換する
- **署名**: ECDSA の DER（ASN.1 SEQUENCE of r, s。可変長・最大 72 バイト程度）。メッセージ全体を SHA-256 でハッシュしてから署名する（呼び出し側でハッシュしない）

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 鍵があるか調べたい | `DeviceKey.HasKey()` |
| 鍵を作りたい（作り直したい） | `DeviceKey.CreateKey()`（同じ名前の鍵は削除して作り直す） |
| サーバーに登録する公開鍵が欲しい | `DeviceKey.GetPublicKey()`（SubjectPublicKeyInfo の DER） |
| データに署名したい | `DeviceKey.Sign(data)`（DER の ECDSA 署名） |
| 鍵を消したい | `DeviceKey.DeleteKey()` |
| 用途ごとに別の鍵を持ちたい | `DeviceKey.SetKeyName(name)`（既定は `"Default"`） |
| この環境で使えるか調べたい | `DeviceKey.IsSupported` |

## 使い方

- 典型的な流れ: 起動時に `HasKey()` が false なら `CreateKey()` → `GetPublicKey()` をサーバーに登録。通信のたびに `Sign(送るデータ)` の結果を付けて送る
- `CreateKey` / `DeleteKey` は成否を `bool`、`GetPublicKey` / `Sign` は失敗時に null を返す。失敗時は `Debug.LogError`（iOS は NSLog、Android は logcat にも）を出す
- 鍵の名前を変えるときは、ほかの呼び出しより前に `SetKeyName` を呼ぶ

## 有効化

1. `ENABLE_DEVICE_KEY` を定義する（`csc.rsp` の `-define` か、Player Settings の Scripting Define Symbols）
2. Android のコード縮小（Minify）を有効にしているときは、カスタム ProGuard ファイルに `-keep class com.unimodules.devicekey.** { *; }` を足す（C# から名前で呼ぶため）

- `.mm` / `.java` をビルドに含めるかは `DeviceKeyPluginFilter`（エディタ用）が `PluginImporter.SetIncludeInBuildDelegate` で決める。判定はこのスクリプトのコンパイル時のシンボルで決まるので、`csc.rsp` の定義も効く。`.meta` の Define Constraints は使わない（判定に `csc.rsp` の定義が含まれない）
- 判定はアセットのパスに登録され、スクリプトの読み込み（`[InitializeOnLoadMethod]`）のたびに登録し直す。`.mm` / `.java` は GUID で探す

## 注意点・罠

- **署名で証明できるのは「この鍵で署名された」ことだけ**。改造したアプリが正規の API で署名させることは防げない。アプリ・端末の正当性まで確かめるには、別途 App Attest（iOS）・Key Attestation / Play Integrity（Android）を組み合わせる
- **鍵は消えることがある**（Android のアプリ削除・データ消去、iOS の機種変更（この端末だけの鍵）・端末のリセット）。鍵が無ければ作り直してサーバーに再登録する流れを用意する
- Android は端末によって鍵がソフトウェアに置かれることがある（TEE の無い端末）。ハードウェアかどうかは本モジュールでは判定しない
- **iOS は端末の初回ロック解除前に署名できない**（再起動直後のバックグラウンド起動など）
- 署名は 1 回ごとにセキュリティチップを呼ぶ。毎フレームのような頻度では呼ばない
- **メインスレッドから呼ぶ**
- 動作環境: iOS は Security.framework（`.meta` で依存を指定済み）・ARC（`-fobjc-arc`）。Android は API 23 以上

## 関連

- [SecureStorage](SecureStorage.md) — OS の安全な保存領域へのキー値の保存（同じ有効化の仕組み）
- [Crypto](Crypto.md) — アプリ同梱の AES 鍵の管理

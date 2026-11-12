# Bugsnag

Bugsnag へクラッシュ・エラーレポートを送る機能。
例外やエラーの送信、操作履歴（パン屑）の記録、メタデータの付与ができる。
実機でのみ動き、エディタでは送信しない。

## 基本情報

> **namespace**: `Modules.Bugsnag`
> **場所**: `Modules/Bugsnag/`
> **依存**: Bugsnag Unity SDK（`BugsnagUnity`。全コードが `#if ENABLE_BUGSNAG`。利用側で定義）/ UniTask / MessagePack / Extensions（Singleton, AesCryptoKey, MessagePackFileUtility, PathUtility, Label 拡張）/ エディタ側のみ Extensions.Devkit・Modules.Devkit.Console

## 概要

クラッシュ・エラーレポートサービス **Bugsnag** の初期化と送信ラッパー。API キーを平文で持たず、AES 暗号化した MessagePack ファイル（配置ディレクトリは派生クラスが決める）から実行時にロードして `Bugsnag.Start` する。
利用側は `BugsnagManager<TInstance, TBugsnagType>` を継承した Manager を1つ用意し、`BugsnagType`（Dev/Production × Android/iOS 等）の選択と鍵・配置ディレクトリの解決を実装する。手動 API は追加コンテキスト付きレポートやパン屑用。
主要クラス: `BugsnagManager<TInstance, TBugsnagType>`（abstract Singleton 本体。Initialize / Notify / Info / Breadcrumb / AddGlobalMetadata。abstract は `GetCryptoKey()` と `GetFileDirectory(bugsnagType)`、差し替え用の virtual は `SetupConfiguration()` / `OnAfterStart()` / `OnErrorCallback(IEvent)` / `IsEnable`） / `IBugsnagManager<TKeyType>`（ファイル名・配置ディレクトリ・復号キーの取得口。生成ウィンドウが使う） / `BugsnagApiKeyData`（ApiKey ファイルの中身） / `Section` enum（メタデータセクション App / Device / User。`[Label]` で実名解決） / `IEventExtensions`（`IEvent.AddMetadata` 拡張） / `BugsnagKeyFileGenerateWindow<TInstance, TBugsnagType>`（エディタ専用 ApiKey 生成ウィンドウ基底）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 例外を手動レポートしたい | `bugsnagManager.Notify(ex, extraData, severity)` |
| 情報レベルの通知を送りたい | `bugsnagManager.Info(name, message, extraData)` |
| パン屑（直前の操作履歴）を残したい | `bugsnagManager.Breadcrumb(message, extraData)` |
| 全レポート共通のメタデータを付けたい | `bugsnagManager.AddGlobalMetadata(Section.User, key, value)` |
| レポート単位でメタデータを付けたい | `Notify(ex, extraData)` の `extraData`（"Extra" セクションに入る）、または `IEvent.AddMetadata(Section, key, value)` 拡張 |
| 送信可能か判定したい | `bugsnagManager.IsEnable`（各 API 内部でもガード済み） |
| 送信するレポートを選別・加工したい | 派生クラスで `OnErrorCallback(IEvent)` を override（`Bugsnag.AddOnError` に登録される） |
| Bugsnag の設定を変えたい | 派生クラスで `SetupConfiguration()` を override（既定は `BugsnagSettingsObject.LoadConfiguration()`） |
| ApiKey ファイルを生成したい | `BugsnagKeyFileGenerateWindow` 派生のエディタウィンドウで生成 |

## 使い方

- **派生 Manager の実装**: `UNITY_IOS/ANDROID × DEVELOPMENT/PRODUCTION` 等で `BugsnagType` を決定、`GetCryptoKey()` で復号キーを供給、`GetFileDirectory(bugsnagType)` で ApiKey ファイルの配置ディレクトリを返す
- **起動時初期化**: `CreateInstance()` → `await Initialize(bugsnagType)`。ApiKey を読み込み、`IsEnable` なら `SetupConfiguration()` → `Bugsnag.Start` → `AddOnError(OnErrorCallback)` → `OnAfterStart()` の順に実行する
- **自動レポートの配線**: 本モジュールは `Debug.LogError` や未捕捉例外を自分では購読しない。自動送信したい場合は、利用側でログの通知を購読して `Notify` / `Info` を呼ぶ
- **グローバルメタデータの付与**: ログイン成功時等にユーザー識別子を全レポートへ付与
- **ApiKey 生成ウィンドウ（エディタ）**: `BugsnagKeyFileGenerateWindow<TInstance, TBugsnagType>` を継承した派生ウィンドウを利用側で用意し、`Open(bugsnagManager)` で開く。`TBugsnagType` の値ごとに ApiKey を入力して Generate すると、暗号化したファイルを書き出す

## 注意点・罠

- **`ENABLE_BUGSNAG` シンボル必須**: モジュール全体（Editor 含む）が `#if ENABLE_BUGSNAG`。利用側でシンボル定義が必要
- **エディタでは送信されない**: `IsEnable` は「apiKey が空でなく、かつ非エディタ」のため、エディタ実行では `Initialize` は ApiKey の読み込みだけ行って `Bugsnag.Start` をせず、送信系の API は何もしない。動作確認は実機ビルドで行う
- **初期化は `CreateInstance()` → `Initialize(bugsnagType)` の順**（`Singleton<T>` 継承のため生成は CreateInstance）。`Initialize` に二重呼び出しのガードはないので、起動時に1回だけ呼ぶ
- **`Initialize` 前提として鍵ファイルが必要**: `GetCryptoKey()` が復号キーを返す。ApiKey ファイル自体も事前生成して、`GetFileDirectory` が返すディレクトリ（実機で読める場所）に置いておく（読めないと `ApiKey load failed.` のエラーログ + 空文字で無効化）
- **自動送信を仕込んだ場合は二重送信に注意**: 利用側で `Debug.LogError` / 未捕捉例外を購読して送る構成では、catch して LogError + `Notify` すると2件飛ぶ。手動 `Notify` は「例外を握りつぶすが記録は残したい」「extraData を添えたい」場合に使う
- **送信対象の除外リストは持たない**: ノイズになるエラーを止めたい場合は、利用側の購読で絞るか `OnErrorCallback` を override する
- **ビルド種別ごとに ApiKey ファイルが分かれる**: `BugsnagType` で配置ディレクトリ・ファイル名（`"{enum型FullName}_{enum名}"` のハッシュ）が変わる。どのシンボルも未定義のビルド構成では default 値になる点に注意
- **`Section` は `[Label]` 必須**: 独自 enum でセクションを増やす場合は `[Label("xxx")]` を付けないと `AddGlobalMetadata` がエラーログ / `AddMetadata` 拡張が ArgumentException

## 関連

- [Crypto](../Crypto/Manual.md) — ApiKey ファイル復号キーの供給元（`KeyFileManager` 派生 + 利用側で定義した KeyType 値）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（ApiKey ファイル読み書き）
- [Extensions/Methods.md](../../Documents~/Extensions/Methods.md) — `AesCryptoKey` / `GetHash`（ファイル名ハッシュ）/ `ToLabelName`

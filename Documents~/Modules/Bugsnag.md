# Bugsnag

> **namespace**: `Modules.Bugsnag`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Bugsnag/`
> **依存**: Bugsnag Unity SDK（`BugsnagUnity`。全コードが `#if ENABLE_BUGSNAG`。利用側で定義）/ UniTask / MessagePack / Extensions（Singleton, AesCryptoKey, MessagePackFileUtility, Label 拡張）

## 概要

クラッシュ・エラーレポートサービス **Bugsnag** の初期化と送信ラッパー。API キーを平文で持たず、AES 暗号化した MessagePack ファイル（StreamingAssets 同梱）から実行時にロードして `Bugsnag.Start` する。
利用側は `BugsnagManager<TInstance, TBugsnagType>` を継承した Manager を1つ用意し、`BugsnagType`（Dev/Production × Android/iOS 等）の選択と鍵・配置ディレクトリの解決を実装する。手動 API は追加コンテキスト付きレポートやパン屑用。
主要クラス: `BugsnagManager<TInstance, TBugsnagType>`（abstract Singleton 本体。Initialize / Notify / Info / Breadcrumb / AddGlobalMetadata） / `Section` enum（メタデータセクション App / Device / User。`[Label]` で実名解決） / `IEventExtensions`（`IEvent.AddMetadata` 拡張） / `BugsnagKeyFileGenerateWindow`（エディタ専用 ApiKey 生成ウィンドウ基底）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 例外を手動レポートしたい | `bugsnagManager.Notify(ex, extraData, severity)` |
| 情報レベルの通知を送りたい | `bugsnagManager.Info(name, message, extraData)` |
| パン屑（直前の操作履歴）を残したい | `bugsnagManager.Breadcrumb(message, extraData)` |
| 全レポート共通のメタデータを付けたい | `bugsnagManager.AddGlobalMetadata(Section.User, key, value)` |
| レポート単位でメタデータを付けたい | `Notify(ex, extraData)` の `extraData`（"Extra" セクションに入る）、または `IEvent.AddMetadata(Section, key, value)` 拡張 |
| 送信可能か判定したい | `bugsnagManager.IsEnable`（各 API 内部でもガード済み） |
| ApiKey ファイルを生成したい | `BugsnagKeyFileGenerateWindow` 派生のエディタウィンドウで生成 |

## 使い方

- **派生 Manager の実装**: `UNITY_IOS/ANDROID × DEVELOPMENT/PRODUCTION` 等で `BugsnagType` を決定、`GetCryptoKey()` で復号キーを供給、鍵ファイル配置ディレクトリを解決
- **起動時初期化 + 自動レポート配線**: `CreateInstance()` → `Setup()`。ログハンドラの `OnErrorReceivedAsObservable` → `Debug.LogError` を Severity.Warning、`OnExceptionReceivedAsObservable` → 未捕捉例外を Severity.Error として自動送信する購読を仕込む
- **グローバルメタデータの付与**: ログイン成功時等にユーザー識別子を全レポートへ付与
- **ApiKey 生成ウィンドウ（エディタ）**: `BugsnagKeyFileGenerateWindow` を継承した派生ウィンドウを利用側で用意

## 注意点・罠

- **`ENABLE_BUGSNAG` シンボル必須**: モジュール全体（Editor 含む）が `#if ENABLE_BUGSNAG`。利用側でシンボル定義が必要
- **エディタでは送信されない**: `IsEnable` は「apiKey が空でなく、かつ非エディタ」のためエディタ実行では `Initialize` 含め全 API が no-op。動作確認は実機ビルドで行う
- **初期化は `CreateInstance()` → `Setup()` の順**（`Singleton<T>` 継承のため生成は CreateInstance）。新規コードで再初期化しない
- **`Initialize` 前提として鍵ファイルが必要**: `GetCryptoKey()` が復号キーを返す。ApiKey ファイル自体も事前生成して StreamingAssets にコミットしておく（未配置だと `ApiKey load failed.` ログ + 空文字で無効化）
- **`Debug.LogError` / 未捕捉例外は自動送信**（購読を仕込んだ場合）: 手動 `Notify` は「例外を握りつぶすが記録は残したい」「extraData を添えたい」場合に使う。二重送信に注意（catch して LogError + Notify すると2件飛ぶ）
- **除外リストで送信対象を絞れる**: 前方一致リストに登録したメッセージは自動送信から除外できる。ノイズになるエラーはここに追加する運用
- **ビルド種別ごとに ApiKey ファイルが分かれる**: `BugsnagType` で配置ディレクトリ・ファイル名（`"{enum型FullName}_{enum名}"` のハッシュ）が変わる。どのシンボルも未定義のビルド構成では default 値になる点に注意
- **`Section` は `[Label]` 必須**: 独自 enum でセクションを増やす場合は `[Label("xxx")]` を付けないと `AddGlobalMetadata` がエラーログ / `AddMetadata` 拡張が ArgumentException

## 関連

- [Crypto](Crypto.md) — ApiKey ファイル復号キーの供給元（`KeyFileManager` / `KeyType.Bugsnag`）
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（ApiKey ファイル読み書き）
- [../Extensions/Methods.md](../Extensions/Methods.md) — `AesCryptoKey` / `GetHash`（ファイル名ハッシュ）/ `ToLabelName`

# Bugsnag

> **namespace**: `Modules.Bugsnag`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Bugsnag/`
> **Client側使用**: 3ファイル（2026-07時点）
> **依存**: Bugsnag Unity SDK（`BugsnagUnity`。全コードが `#if ENABLE_BUGSNAG`、Dominion では `Client/Assets/csc.rsp` で常時定義）/ UniTask / MessagePack / Extensions（Singleton, AesCryptoKey, MessagePackFileUtility, Label 拡張）

## 概要

クラッシュ・エラーレポートサービス **Bugsnag** の初期化と送信ラッパー。API キーを平文で持たず、AES 暗号化した MessagePack ファイル（StreamingAssets 同梱）から実行時にロードして `Bugsnag.Start` する。
Client 側の入口は `Dominion.Core.Bugsnag.BugsnagManager`（`Client/Assets/Scripts/Client/Core/BugsnagManager.cs`）。起動時に `InitializeObject` が Setup 済みで、**Unity のエラーログ・未捕捉例外は自動で送信される**。手動 API は追加コンテキスト付きレポートやパン屑用。
主要クラス: `BugsnagManager<TInstance, TBugsnagType>`（abstract Singleton 本体。Initialize / Notify / Info / Breadcrumb / AddGlobalMetadata） / Client側 `Dominion.Core.Bugsnag.BugsnagManager`（`BugsnagType` = Dev/Production × Android/iOS の選択と鍵・配置ディレクトリの解決） / `Section` enum（メタデータセクション App / Device / User。`[Label]` で実名解決） / `IEventExtensions`（`IEvent.AddMetadata` 拡張） / `BugsnagKeyFileGenerateWindow`（エディタ専用 ApiKey 生成ウィンドウ基底）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 例外を手動レポートしたい | `BugsnagManager.Instance.Notify(ex, extraData, severity)` |
| 情報レベルの通知を送りたい | `BugsnagManager.Instance.Info(name, message, extraData)` |
| パン屑（直前の操作履歴）を残したい | `BugsnagManager.Instance.Breadcrumb(message, extraData)` |
| 全レポート共通のメタデータを付けたい | `BugsnagManager.Instance.AddGlobalMetadata(Section.User, key, value)` |
| レポート単位でメタデータを付けたい | `Notify(ex, extraData)` の `extraData`（"Extra" セクションに入る）、または `IEvent.AddMetadata(Section, key, value)` 拡張 |
| 送信可能か判定したい | `BugsnagManager.Instance.IsEnable`（各 API 内部でもガード済み） |
| ApiKey ファイルを生成したい | メニュー `Dominion/Tools/Open BugsnagApiKeyWindow` |

## 使い方

- **Client 側 Manager 実装**（abstract 実装。UNITY_IOS/ANDROID × DEVELOPMENT/PRODUCTION で `BugsnagType` を決定、`GetCryptoKey()` は `KeyFileManager` の KeyType.Bugsnag、配置は `StreamingAssets/Bugsnag`・Dev 系は `/Devkit` 配下）: `Client/Assets/Scripts/Client/Core/BugsnagManager.cs`
- **起動時初期化 + 自動レポート配線**（実施済み・新規実装で呼ぶ必要はない。`DebugLog.OnErrorReceivedAsObservable` → `Debug.LogError` を Severity.Warning、`OnExceptionReceivedAsObservable` → 未捕捉例外を Severity.Error（既定）として自動送信）: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs` の `CreateBugsnagManager()`
- **グローバルメタデータの付与**（ログイン成功時にユーザー識別子を全レポートへ付与）: `Client/Assets/Scripts/Client/Model/User/UserModel.cs`
- **ApiKey 生成ウィンドウ（エディタ）**: `Client/Assets/Scripts/Editor/Bugsnag/BugsnagApiKeyWindow.cs`（メニュー登録は `Client/Assets/Scripts/Editor/ProductEditorMenu.cs`）

## 注意点・罠

- **`ENABLE_BUGSNAG` シンボル必須**: モジュール全体（Editor 含む）が `#if ENABLE_BUGSNAG`。Dominion では `Client/Assets/csc.rsp` の `-define:ENABLE_BUGSNAG` で常時有効（Client 側継承クラスに `#if` は無い）
- **エディタでは送信されない**: `IsEnable` は「apiKey が空でなく、かつ非エディタ」のためエディタ実行では `Initialize` 含め全 API が no-op。動作確認は実機ビルドで行う
- **初期化は `InitializeObject.CreateBugsnagManager()` が実施済み**: `CreateInstance()` → `Setup()` の順（`Singleton<T>` 継承のため生成は CreateInstance）。新規コードで再初期化しない
- **`Initialize` 前提として鍵ファイルが必要**: `GetCryptoKey()` が `KeyFileManager.LoadKeyFile(KeyType.Bugsnag)` を呼ぶ。ApiKey ファイル自体も `BugsnagApiKeyWindow` で事前生成して StreamingAssets にコミットしておく（未配置だと `ApiKey load failed.` ログ + 空文字で無効化）
- **`Debug.LogError` / 未捕捉例外は自動送信**: 手動 `Notify` は「例外を握りつぶすが記録は残したい」「extraData を添えたい」場合に使う。二重送信に注意（catch して LogError + Notify すると2件飛ぶ）
- **除外リストは `InitializeObject.IgnoreNotifyErrorMessage`**: Timeout / `Load master failed.` 等、前方一致するものは自動送信から除外されている。ノイズになるエラーはここに追加する運用
- **ビルド種別ごとに ApiKey ファイルが分かれる**: `BugsnagType`（Dev/Production × Android/iOS）で配置ディレクトリ・ファイル名（`"{enum型FullName}_{enum名}"` のハッシュ）が変わる。DEVELOPMENT/PRODUCTION どちらも未定義のビルド構成では default（DevAndroid 扱い）になる点に注意
- **`Section` は `[Label]` 必須**: 独自 enum でセクションを増やす場合は `[Label("xxx")]` を付けないと `AddGlobalMetadata` がエラーログ / `AddMetadata` 拡張が ArgumentException

## 関連

- [Crypto](Crypto.md) — ApiKey ファイル復号キーの供給元（`KeyFileManager` / `KeyType.Bugsnag`）
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（ApiKey ファイル読み書き）
- [../Extensions/Methods.md](../Extensions/Methods.md) — `AesCryptoKey` / `GetHash`（ファイル名ハッシュ）/ `ToLabelName`

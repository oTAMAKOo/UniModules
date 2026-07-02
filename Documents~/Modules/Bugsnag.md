# Bugsnag

> **namespace**: `Modules.Bugsnag`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Bugsnag/`
> **Client側使用**: 3ファイル（2026-07時点）
> **依存**: Bugsnag Unity SDK（`BugsnagUnity`。全コードが `#if ENABLE_BUGSNAG`、Dominion では `Client/Assets/csc.rsp` で常時定義）/ UniTask / MessagePack / Extensions（Singleton, AesCryptoKey, MessagePackFileUtility, Label 拡張）

## 概要

クラッシュ・エラーレポートサービス **Bugsnag** の初期化と送信ラッパー。API キーを平文で持たず、AES 暗号化した MessagePack ファイル（StreamingAssets 同梱）から実行時にロードして `Bugsnag.Start` する。
Client 側の入口は `Dominion.Core.Bugsnag.BugsnagManager`（`Client/Assets/Scripts/Client/Core/BugsnagManager.cs`）。起動時に `InitializeObject` が Setup 済みで、**Unity のエラーログ・未捕捉例外は自動で送信される**。手動 API は追加コンテキスト付きレポートやパン屑用。

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

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `BugsnagManager<TInstance, TBugsnagType>` | abstract（`Singleton<TInstance>`、TBugsnagType: Enum） | 本体。`Initialize`（ApiKey ロード → `Bugsnag.Start` → `AddOnError`）と `Notify` / `Info` / `Breadcrumb` / `AddGlobalMetadata` |
| `IBugsnagManager<TKeyType>` | interface | ApiKey ファイル名・ディレクトリ・暗号キー解決の IF（Editor ウィンドウが参照） |
| `Section` | enum | メタデータセクション（`App` / `Device` / `User`、`[Label("app")]` 等で実名解決） |
| `BugsnagApiKeyData` | class（`[MessagePackObject(true)]`） | ApiKey ファイルの中身（`apiKey` 1フィールド） |
| `IEventExtensions` | static | `IEvent.AddMetadata(Section, key, value)` 拡張（`OnErrorCallback` 内などで使用） |
| `BugsnagKeyFileGenerateWindow<TInstance, TBugsnagType>` | SingletonEditorWindow・**エディタ専用** | enum 全種別分の ApiKey 暗号化ファイルを生成するウィンドウ基底 |
| `Dominion.Core.Bugsnag.BugsnagManager` | sealed（Client 側継承） | `BugsnagType`（DevAndroid / DevIOS / ProductionAndroid / ProductionIOS）の選択と鍵・配置ディレクトリの解決 |

## 使い方(実例)

### 1. Client 側 Manager（abstract 実装 + ビルド種別の解決）

```csharp
// Client/Assets/Scripts/Client/Core/BugsnagManager.cs
public sealed class BugsnagManager : BugsnagManager<BugsnagManager, BugsnagType>
{
    public async UniTask Setup()
    {
        BugsnagType bugsnagType = default;

        // UNITY_IOS/UNITY_ANDROID × DEVELOPMENT/PRODUCTION で BugsnagType を決定.

        await Initialize(bugsnagType);
    }

    public override async UniTask<AesCryptoKey> GetCryptoKey()
    {
        var keyFileManager = KeyFileManager.Instance;

        await keyFileManager.LoadKeyFile(KeyFileManager.KeyType.Bugsnag);

        var keyData = keyFileManager.Get(KeyFileManager.KeyType.Bugsnag);

        return new AesCryptoKey(keyData.Key, keyData.Iv);
    }

    public override string GetFileDirectory(BugsnagType bugsnagType)
    {
        var directory = PathUtility.Combine(UnityPathUtility.StreamingAssetsPath, "Bugsnag");

        // Dev 系は "Bugsnag/Devkit" 配下.

        return directory;
    }
}
```

### 2. 起動時初期化（自動レポート配線。実施済み・新規実装で呼ぶ必要はない）

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
private async UniTask CreateBugsnagManager()
{
    var bugsnagManager = BugsnagManager.CreateInstance();

    await bugsnagManager.Setup();

    void OnError(string error)
    {
        if (IgnoreNotifyErrorMessage.Any(x => error.StartsWith(x))) { return; }

        bugsnagManager.Notify(new Exception(error), severity: Severity.Warning);
    }

    DebugLog.OnErrorReceivedAsObservable().Subscribe(x => OnError(x));

    // 例外も同様に DebugLog.OnExceptionReceivedAsObservable() → bugsnagManager.Notify(ex).
}
```

`Debug.LogError` はここで Severity.Warning として、未捕捉例外は Severity.Error（既定）として自動送信される。`IgnoreNotifyErrorMessage`（Timeout 等）に前方一致するものは除外。

### 3. グローバルメタデータの付与

```csharp
// Client/Assets/Scripts/Client/Model/User/UserModel.cs
var bugsnagManager = BugsnagManager.Instance;

// ログイン成功時にユーザー識別子を全レポートへ付与.
bugsnagManager.AddGlobalMetadata(Section.User, "UserCode", result.userCode);
```

### 4. ApiKey 生成ウィンドウ（エディタ）

```csharp
// Client/Assets/Scripts/Editor/Bugsnag/BugsnagApiKeyWindow.cs
public sealed class BugsnagApiKeyWindow : BugsnagKeyFileGenerateWindow<BugsnagApiKeyWindow, BugsnagType> { }
```

```csharp
// Client/Assets/Scripts/Editor/ProductEditorMenu.cs
[MenuItem(itemName: ToolsMenu + "Open BugsnagApiKeyWindow", priority = 13)]
public static void OpenBugsnagApiKeyWindow()
{
    BugsnagApiKeyWindow.Open(BugsnagManager.Instance).Forget();
}
```

## API(主要公開メンバー)

### BugsnagManager&lt;TInstance, TBugsnagType&gt;（Singleton: `BugsnagManager.Instance`）

| メンバー | 説明 |
|---|---|
| `UniTask Initialize(TBugsnagType bugsnagType)` | ApiKey ロード → `Bugsnag.Start(config)` → `AddOnError` 登録。`IsEnable` が false なら Start しない |
| `virtual bool IsEnable { get; }` | 送信可能か。**apiKey が空でなく、かつ非エディタ**（エディタでは常に false） |
| `void Notify(Exception e, Dictionary<string, object> extraData = null, Severity severity = Severity.Error)` | エラー通知。extraData は "Extra" セクションとして添付 |
| `void Info(string name, string message, Dictionary<string, object> extraData = null)` | 情報通知（現在のスタックトレース付きで `Bugsnag.Notify`） |
| `void Breadcrumb(string message, Dictionary<string, object> extraData = null)` | パン屑記録（`LeaveBreadcrumb`。以後のレポートに履歴として付く） |
| `void AddGlobalMetadata(Section section, string key, object value)` | 全レポート共通メタデータ（`Enum` 版オーバーロードあり。Label 名でセクション解決） |
| `UniTask<string> LoadApiKey()` | 暗号化 MessagePack ファイル（`MessagePackFileUtility.ReadAsync<BugsnagApiKeyData>`）から ApiKey 取得。失敗時は LogError + 空文字 |
| `string GetApiKeyFileName(TBugsnagType)` | ファイル名 = `"{enum型FullName}_{enum名}"` のハッシュ（`GetHash()`） |
| `protected virtual Configuration SetupConfiguration()` | 既定は `BugsnagSettingsObject.LoadConfiguration()`（SDK の設定アセット） |
| `protected virtual void OnAfterStart()` / `protected virtual bool OnErrorCallback(IEvent e)` | Start 後フック / 送信前フック（false 返却で送信破棄） |
| `abstract UniTask<AesCryptoKey> GetCryptoKey()` | ApiKey ファイルの復号キー（**Client 実装必須**。Dominion は `KeyFileManager` KeyType.Bugsnag） |
| `abstract string GetFileDirectory(TBugsnagType)` | ApiKey ファイルの配置ディレクトリ（**Client 実装必須**。Dominion は `StreamingAssets/Bugsnag`、Dev 系は `/Devkit` 付き） |

### IEventExtensions

| メンバー | 説明 |
|---|---|
| `void AddMetadata(this IEvent e, Section section, string key, object value)` | レポート単位のメタデータ追加（`Enum` 版あり。Label 未定義 enum は ArgumentException） |

### BugsnagKeyFileGenerateWindow&lt;TInstance, TBugsnagType&gt;（エディタ専用）

| メンバー | 説明 |
|---|---|
| `static UniTask Open(IBugsnagManager<TBugsnagType> bugsnagManager)` | enum 全種別分の ApiKey 入力欄 + Generate ボタンのウィンドウを開く。既存ファイルは復号して表示 |

## 注意点・罠

- **`ENABLE_BUGSNAG` シンボル必須**: モジュール全体（Editor 含む）が `#if ENABLE_BUGSNAG`。Dominion では `Client/Assets/csc.rsp` の `-define:ENABLE_BUGSNAG` で常時有効（Client 側継承クラスに `#if` は無い）
- **エディタでは送信されない**: `IsEnable` が `!UnityUtility.IsEditor` を含むため、エディタ実行では `Initialize` 含め全 API が no-op。動作確認は実機ビルドで行う
- **初期化は `InitializeObject.CreateBugsnagManager()` が実施済み**: `CreateInstance()` → `Setup()` の順（`Singleton<T>` 継承のため生成は CreateInstance）。新規コードで再初期化しない
- **`Initialize` 前提として鍵ファイルが必要**: `GetCryptoKey()` が `KeyFileManager.LoadKeyFile(KeyType.Bugsnag)` を呼ぶ。ApiKey ファイル自体も `BugsnagApiKeyWindow` で事前生成して StreamingAssets にコミットしておく（未配置だと `ApiKey load failed.` ログ + 無効化）
- **`Debug.LogError` / 未捕捉例外は自動送信**: 手動 `Notify` は「例外を握りつぶすが記録は残したい」「extraData を添えたい」場合に使う。二重送信に注意（catch して LogError + Notify すると2件飛ぶ）
- **除外リストは `InitializeObject.IgnoreNotifyErrorMessage`**: Timeout / `Load master failed.` 等は自動送信から除外されている。ノイズになるエラーはここに追加する運用
- **ビルド種別ごとに ApiKey ファイルが分かれる**: `BugsnagType`（Dev/Production × Android/iOS）で配置ディレクトリ・ファイル名が変わる。DEVELOPMENT/PRODUCTION どちらも未定義のビルド構成では default（DevAndroid 扱い）になる点に注意
- **`Section` は `[Label]` 必須**: 独自 enum でセクションを増やす場合は `[Label("xxx")]` を付けないと `AddGlobalMetadata` がエラーログ / `AddMetadata` 拡張が ArgumentException

## 関連

- [Crypto](Crypto.md) — ApiKey ファイル復号キーの供給元（`KeyFileManager` / `KeyType.Bugsnag`）
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（ApiKey ファイル読み書き）
- [../Extensions/Methods.md](../Extensions/Methods.md) — `AesCryptoKey` / `GetHash`（ファイル名ハッシュ）/ `ToLabelName`

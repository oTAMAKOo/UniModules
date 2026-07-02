# Crypto

> **namespace**: `Modules.Crypto`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Crypto/`
> **Client側使用**: 1ファイル（2026-07時点、`using Modules.Crypto` ベース。継承先 `KeyFileManager` の参照は Client 全域 15+ ファイル）
> **依存**: UniTask / Extensions（Singleton, AesCryptoKey, AESExtension, PathUtility, UnityPathUtility, AndroidUtility）/ Modules.Devkit（Editor: KeyFileWindow）

## 概要

AES 鍵（Key/Iv）を**暗号化キーファイル**として StreamingAssets に同梱し、実行時にロード・キャッシュして各サブシステムへ供給する鍵管理基盤。
暗号化・復号の処理そのものは `Extensions.AESExtension`（`str.Encrypt(aesKey)` 等）が担い、本モジュールの責務は「**鍵をコードに直書きしないための鍵の保管・配布**」。
Client 側の入口は `Dominion.Client.Core.KeyFileManager`（`Client/Assets/Scripts/Client/Core/Key/KeyFileManager.cs`）。起動時に全鍵ロード済みで、利用側は `Get(KeyType.Xxx)` で鍵を引くだけ。

## AESExtension（Extensions）との使い分け

| | **Modules.Crypto（KeyFileManager）** | **Extensions.AESExtension / AesCryptoKey** |
|---|---|---|
| 役割 | 鍵（Key/Iv）の永続化・ロード・キャッシュ | 実際の AES-256/CBC 暗号化・復号処理 |
| 使い方 | `Get(KeyType.Xxx)` で用途別の鍵を引く | `KeyData` から `new AesCryptoKey(key, iv)` を作り `Encrypt`/`Decrypt` 拡張へ渡す |
| 鍵の出所 | `StreamingAssets/Files/` の暗号化ファイル（KeyFileWindow で生成・コミット） | 呼び出し側が用意（パスワード派生 or key+iv 直指定） |

典型パターン（このプロジェクトの標準）:

```csharp
var keyData = KeyFileManager.Instance.Get(KeyFileManager.KeyType.Xxx);

var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

// 以降は AESExtension（str.Encrypt(cryptoKey) 等）や各モジュールの SetCryptoKey へ.
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 用途別の AES 鍵を取得したい | `KeyFileManager.Instance.Get(KeyFileManager.KeyType.Xxx)` → `new AesCryptoKey(keyData.Key, keyData.Iv)` |
| 全鍵を一括ロードしたい | `await KeyFileManager.Instance.Load()`（起動時 `InitializeObject` 実施済み。Editor ツールでは都度呼ぶ） |
| 1種類だけロードしたい | `await KeyFileManager.Instance.LoadKeyFile(keyType)` |
| 新しい鍵種別を追加したい | Client の `KeyFileManager.KeyType` enum に追加 → KeyFileWindow で生成 |
| 鍵ファイルを生成・更新したい | メニュー `Extension/Tools/Open KeyFileWindow`（Key 32文字 / Iv 16文字） |
| 鍵ファイルの配置先を知りたい | `KeyFileManager.Instance.GetLoadPath(keyType)`（`StreamingAssets/Files/{enum名のハッシュ}`） |
| 文字列・byte[] を暗号化/復号したい | `AESExtension`（[../Extensions/Methods.md](../Extensions/Methods.md)。本モジュールではない） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `KeyFileManager<TInstance, TKeyType>` | abstract（`Singleton<TInstance>`、TKeyType: Enum） | 本体。`Create` / `Load` / `LoadKeyFile` / `Get` / `GetLoadPath` / `ClearCache`。`FileDirectory` / `Separator` / `CustomEncode` / `CustomDecode` を継承先が定義 |
| `IKeyFileManager<TKeyType>` | interface | Editor ウィンドウ向けの操作 IF |
| `KeyData` | class | 復号済みの鍵ペア（`string Key` / `string Iv`） |
| `KeyFileWindow<TInstance, TKeyType>` | SingletonEditorWindow・**エディタ専用** | enum 全種別分の Key/Iv 入力 + Generate ボタン。Key は 32 文字・Iv は 16 文字をバリデーション |
| `Dominion.Client.Core.KeyFileManager` | sealed（Client 側継承） | `KeyType` enum（SecurePrefs / LocalData / TextData / MasterData / CriWare / Bugsnag / PlayFabData / AssetFile / BugLogServer / BattleTrace）。`FileDirectory = "Files"`、`Separator = ":*"`、CustomEncode/Decode はビット反転 |

### キーファイルの中身（Create が書き込む形式）

`ランダム GUID をパスワードとした AesCryptoKey` で Key と Iv をそれぞれ暗号化し、`暗号化Key + Separator + 暗号化Iv + Separator + GUID` を UTF8 バイト化 → `CustomEncode`（Dominion はビット反転）して書き込む。ロード時は逆順に復元する。

## 使い方(実例)

### 1. Client 側実装（KeyType 定義 + 難読化フック）

```csharp
// Client/Assets/Scripts/Client/Core/Key/KeyFileManager.cs
public sealed class KeyFileManager : KeyFileManager<KeyFileManager, KeyFileManager.KeyType>
{
    public enum KeyType
    {
        SecurePrefs, LocalData, TextData, MasterData, CriWare,
        Bugsnag, PlayFabData, AssetFile, BugLogServer, BattleTrace,
    }

    protected override string Separator { get { return ":*"; } }

    public override string FileDirectory { get { return "Files"; } }

    protected override byte[] CustomEncode(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)~bytes[i];
        }

        return bytes;
    }

    // CustomDecode も同一のビット反転.
}
```

### 2. 起動時の一括ロード（実施済み。新規実装で呼ぶ必要はない）

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
// 鍵情報読み込み.
await KeyFileManager.Instance.Load();
```

### 3. 鍵の取得 → AesCryptoKey 生成（SecurePrefs 初期化の例）

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
private void InitializeSecurePrefs()
{
    var keyData = KeyFileManager.Instance.Get(KeyFileManager.KeyType.SecurePrefs);

    var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

    SecurePrefs.SetCryptoKey(cryptoKey);
}
```

### 4. 個別ロード（他モジュールからの利用: Bugsnag）

```csharp
// Client/Assets/Scripts/Client/Core/BugsnagManager.cs
public override async UniTask<AesCryptoKey> GetCryptoKey()
{
    var keyFileManager = KeyFileManager.Instance;

    await keyFileManager.LoadKeyFile(KeyFileManager.KeyType.Bugsnag);

    var keyData = keyFileManager.Get(KeyFileManager.KeyType.Bugsnag);

    return new AesCryptoKey(keyData.Key, keyData.Iv);
}
```

### 5. エディタツールでの利用（生成ウィンドウ / ツール前の再ロード）

```csharp
// Client/Assets/Scripts/Editor/EditorMenu.cs
[MenuItem(itemName: ToolsMenu + "Open KeyFileWindow", priority = 14)]
public static void OpenKeyFileWindow()
{
    KeyFileWindow.Open(KeyFileManager.Instance).Forget();
}
```

```csharp
// Client/Assets/Scripts/Editor/ProductEditorMenu.cs (SaveData 変換メニュー)
SingletonManager.ReleaseAll();

await KeyFileManager.Instance.Load();    // ReleaseAll 後は再ロードが必要.
```

## API(主要公開メンバー)

### KeyFileManager&lt;TInstance, TKeyType&gt;（Singleton: `KeyFileManager.Instance`）

| メンバー | 説明 |
|---|---|
| `UniTask Load()` | 全 `TKeyType` 分を並列ロードしキャッシュ（`UnityPathUtility.Initialized` 待ちあり） |
| `UniTask LoadKeyFile(TKeyType keyType)` | 1種別だけロード。**Android は StreamingAssets → temporaryCachePath へコピーしてから読む**。ファイルが無ければ何もしない |
| `KeyData Get(TKeyType keyType)` | キャッシュから取得。**未ロード・ファイル無しなら null** |
| `string GetLoadPath(TKeyType keyType)` | `{FileDirectory}/{enum名のハッシュ(GetHash)}`（StreamingAssets からの相対パス） |
| `void Create(string filePath, string key, string iv)` | 鍵ファイル生成（エディタの KeyFileWindow が使用。ランタイムでは呼ばない） |
| `void ClearCache()` | キャッシュ破棄 |
| `abstract string FileDirectory { get; }` | 配置ディレクトリ名（Dominion: `"Files"`） |
| `protected abstract string Separator { get; }` | ファイル内の区切り文字列（Dominion: `":*"`） |
| `protected virtual byte[] CustomEncode(byte[])` / `CustomDecode(byte[])` | ファイル全体への追加難読化フック（既定は素通し。Dominion はビット反転） |

### KeyData

| メンバー | 説明 |
|---|---|
| `string Key { get; }` / `string Iv { get; }` | 復号済みの AES Key（32文字）/ Iv（16文字）。`new AesCryptoKey(Key, Iv)` に渡す |

### KeyFileWindow&lt;TInstance, TKeyType&gt;（エディタ専用）

| メンバー | 説明 |
|---|---|
| `static UniTask Open(IKeyFileManager<TKeyType> keyFileManager)` | 生成ウィンドウを開く。既存ファイルは復号して Key/Iv を表示。**Key は 32 文字・Iv は 16 文字以外は入力を弾く**。Generate で `Create` + ImportAsset |

## 注意点・罠

- **`Get` の前に `Load` 必須**: 実行時は `InitializeObject.core.cs` が起動直後に一括ロード済み。エディタツール・テストでは都度 `await KeyFileManager.Instance.Load()` を呼ぶ（`SingletonManager.ReleaseAll()` 後も再ロードが必要）。未ロードだと `Get` は null を返し NullReference の温床になる
- **鍵ファイルは StreamingAssets 同梱・git 管理**: `Client/Assets/StreamingAssets/Files/`（ファイル名は enum 名のハッシュ）。ビルドに同梱されるため「秘匿」ではなく「難読化」である点を理解して使う
- **Key 32 文字 / Iv 16 文字固定**: `AesCryptoKey(key, iv)` が UTF8 バイト列をそのまま AES-256/CBC の Key/IV にするため。KeyFileWindow がバリデーションする
- **`LoadKeyFile` はファイルが無くても例外を出さない**: 黙ってスキップされ、`Get` が null になって初めて気付く。新 KeyType 追加時は KeyFileWindow での生成を忘れない
- **ファイル破損・形式不一致は `InvalidDataException`**: `Separator` や `CustomEncode/Decode` の実装を変更すると既存ファイルが読めなくなる（全ファイル再生成が必要）
- **enum 名の変更 = ファイル名の変更**: `GetLoadPath` が enum 名のハッシュを使うため、`KeyType` のリネームで既存ファイルは参照不能になる
- **Android は初回ロードにコピー I/O**: StreamingAssets が WebRequest 経由でしか読めないため `AndroidUtility.CopyStreamingToTemporary` を挟む（`#if UNITY_ANDROID && !UNITY_EDITOR`）
- **`Load()` は `UnityPathUtility.Initialized` を待つ**: メインスレッド初期化前に await すると進まない可能性がある。通常の起動フローでは問題にならない
- **暗号処理を自前実装しない**: 暗号化・復号は `AESExtension`（`Encrypt`/`Decrypt` 拡張）を使う。本モジュールは鍵の供給のみ

## 関連

- [../Extensions/Methods.md](../Extensions/Methods.md) — `AESExtension` / `AesCryptoKey`（実際の暗号化・復号）、`GetHash`
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `MessagePackFileUtility`（AES 対応ファイル IO）
- [Bugsnag](Bugsnag.md) — `KeyType.Bugsnag`（ApiKey ファイル復号）
- [TextData](TextData.md) — `KeyType.TextData`（テキスト暗号キー）
- [LocalData](LocalData.md) — `KeyType.LocalData` / `KeyType.SecurePrefs`（セーブデータ暗号キー）
- [Master](Master.md) — `KeyType.MasterData`（マスター暗号キー）

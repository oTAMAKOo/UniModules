# LocalData

> **namespace**: `Modules.LocalData`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/LocalData/`
> **Client側使用**: 約32ファイル（2026-07時点）
> **依存**: UniTask / R3 / MessagePack / Extensions（`Singleton`, `AesCryptoKey`, `MessagePackFileUtility`, `UnityPathUtility`） / Modules.Devkit.Console（ログ出力） / Modules.MessagePack（`UnityCustomResolver`）

## 概要

端末ローカルに永続化するデータ（セーブデータ・ユーザー設定・履歴）の型ベース Load / Save 基盤。
「1クラス = 1ファイル」で、`ILocalData` を実装したクラスを `LocalDataManager.Get<T>()` で読み、`data.Save()` で書く。
シリアライズは MessagePack（LZ4 圧縮）、本文とファイル名は AES 暗号化。サーバー保存ではない点に注意（PlayFab 連携は別基盤）。

### 保存の仕組み

| 項目 | 内容 |
|---|---|
| 保存先 | `UnityPathUtility.GetPrivateDataPath() + "/LocalData/"`（Android: `getFilesDir` / その他: `Application.persistentDataPath`）。`SetFileDirectory()` で変更可 |
| シリアライズ | MessagePack + LZ4BlockArray 圧縮（`MessagePackFileUtility` 経由、`StandardResolverAllowPrivate` + `UnityCustomResolver`） |
| 本文の暗号化 | `SetCryptoKey()` で設定した `AesCryptoKey` でバイナリ全体を AES 暗号化（鍵未設定なら平文） |
| ファイル名 | `[FileName("名前")]` で指定。`encrypt: true`（デフォルト）なら「名前を暗号化してハッシュ化」した文字列がディスク上のファイル名になる（`encrypt: false` は平文ファイル名。本文暗号化とは独立） |
| 書き込みタイミング | デフォルトはフレーム末尾（`PlayerLoopTiming.LastPostLateUpdate`）に集約し、スレッドプールで書き込み。`immediate: true` で即時 |
| メモリキャッシュ | `Get<T>()` は初回のみファイルを Load し、以降は型ごとのキャッシュを返す。ファイルが無ければ `new T()` |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ローカル保存データを読みたい | `LocalDataManager.Get<T>()` |
| 保存したい | `data.Save()`（拡張メソッド）/ `LocalDataManager.Save(data, immediate)` |
| 新しい保存データ型を追加したい | `ILocalData` + `[FileName]` + `[MessagePackObject(true)]`（下記手順参照） |
| ファイルが存在するか確認したい | `LocalDataManager.FileExist<T>()` |
| 保存ファイルを削除したい | `LocalDataManager.Delete<T>()` / `LocalDataManager.DeleteAll()` |
| ロード / セーブをフックしたい | `OnLoadAsObservable()` / `OnSaveAsObservable()` |
| 起動時に暗号鍵を設定したい | `localDataManager.SetCryptoKey(cryptoKey)` |
| 実ファイルパスを知りたい（デバッグ） | `localDataManager.GetFilePath<T>()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `LocalDataManager` | Singleton（`Extensions.Singleton<T>`。`Instance` 初回アクセスで自動生成） | Load / Save / Delete と型別メモリキャッシュの管理 |
| `ILocalData` | interface | 保存データ型のマーカー（メンバーなし） |
| `FileNameAttribute` | Attribute（class 対象） | 保存ファイル名の指定。**必須**（無いと `GetFilePath` で例外） |
| `LocalDataExtension` | static class | `data.Save()` 拡張メソッドを提供 |

## 使い方(実例)

### データクラスの定義（Client 実物）

```csharp
// 引用元: Client/Assets/Scripts/Client/LocalData/ServiceData/ServiceData.cs
using MessagePack;
using Modules.LocalData;

namespace Dominion.Client.LocalData
{
    [FileName("Dominion-ServiceData")]
    [MessagePackObject(true)]
    public sealed class ServiceData : ILocalData
    {
        /// <summary> 規約確認バージョン </summary>
        public uint? ServiceConfirmVersion { get; set; } = null;
    }
}
```

```csharp
// 引用元: Client/Assets/Scripts/Client/LocalData/PlayData/PlayData.cs (抜粋)
[FileName("Dominion-PlayData")]
[MessagePackObject(true)]
public sealed class PlayData : ILocalData
{
    /// <summary> 言語設定 </summary>
    public Language? Language { get; set; }

    public float MasterVolume { get; set; } = 1f;

    public float BgmVolume { get; set; } = 0.5f;

    /// <summary> 戦闘の演出速度倍率 </summary>
    public BattleSpeedType BattleSpeed { get; set; } = BattleSpeedType.X1;
}
```

ネストするデータ型（`ILocalData` 直下でないクラス）にも `[MessagePackObject(true)]` を付ける:

```csharp
// 引用元: Client/Assets/Scripts/Client/LocalData/SaveData/Data/UserSaveData.cs (抜粋)
[MessagePackObject(true)]
public sealed class UserSaveData
```

### 新しいローカル保存データを追加する手順（Claude向け）

1. `Client/Assets/Scripts/Client/LocalData/` 配下にクラスを新規作成する（既存: `SaveData` / `PlayData` / `ServiceData` / `BattleHistoryData` / `WorldMapEnemyBattleData`。namespace は `Dominion.Client.LocalData`）
2. クラスに以下を揃える:
   - `[FileName("Dominion-Xxx")]` — プロジェクト内で一意な名前。既存はすべて `Dominion-` プレフィックス
   - `[MessagePackObject(true)]` — キー文字列（map）モード
   - `ILocalData` 実装 + `sealed class`
3. メンバーは **public プロパティ（get; set;）+ 初期値** で定義する（ファイル未作成時は `new T()` が返るため、初期値がそのままデフォルト値になる）
4. ネストクラスにも `[MessagePackObject(true)]` を付ける（`ILocalData` と `[FileName]` はルートのクラスのみ）
5. 読み書きは `LocalDataManager.Get<T>()` → プロパティ変更 → `data.Save()`
6. **既存クラスへのプロパティ追加は後方互換**（map モードのため既存ファイルはそのまま読める）。プロパティの**リネーム・型変更は既存データが読めなくなる／消える**ので原則行わない
7. IL2CPP 実機ビルドでは MessagePack の生成コード（GeneratedResolver）が必要（`UnityCustomResolver` は「Editor時はDynamicResolver、実行時はGeneratedResolverの振る舞い」）。生成は Source Generator（csc.rsp の `MESSAGEPACK_ANALYZER_CODE`）で自動化済みのため手動生成は不要。ただし **`[MessagePackObject(true)]` の付け忘れは生成対象から漏れ、エディタ上では Dynamic で動いてしまうため実機で初めてクラッシュする**（詳細は [MessagePack](MessagePack.md)）

### 読み込み → 変更 → 保存

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Window/SoundVolume/SoundVolumeWindow.cs (抜粋)
protected override async UniTask OnClose()
{
    var changed = false;

    var playData = LocalDataManager.Get<PlayData>();

    if (masterVolumeSlider.value != playData.MasterVolume)
    {
        playData.MasterVolume = masterVolumeSlider.value / 100f;
        changed = true;
    }

    if (changed)
    {
        playData.Save();    // LocalDataExtension の拡張メソッド. フレーム末尾に書き込まれる.
    }

    await base.OnClose();
}
```

### 起動時の初期化（暗号鍵の設定）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
public static void InitializeLocalDataManager()
{
    var localDataManager = LocalDataManager.Instance;

    var keyFileManager = KeyFileManager.Instance;

    var keyData = keyFileManager.Get(KeyFileManager.KeyType.LocalData);

    var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

    localDataManager.SetCryptoKey(cryptoKey);
}
```

### ロード / セーブのフック（SaveDataManager が購読）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/SaveData/SaveDataManager.cs (抜粋)
var localDataManager = LocalDataManager.Instance;

localDataManager.OnLoadAsObservable()
    .Subscribe(x => OnLoad(x))      // x は ILocalData. as SaveData で型を絞って処理.
    .AddTo(Disposable);
```

### 削除（デバッグメニューのユーザーデータ削除）

```csharp
// 引用元: Client/Assets/Scripts/Client/Devkit/DeveloperMenu/Command/User/UserDelete.cs (抜粋)
LocalDataManager.DeleteAll();
```

## API(主要公開メンバー)

### LocalDataManager（static メソッド）

| メンバー | 説明 |
|---|---|
| `static T Get<T>()` | 取得。未ロードなら Load してキャッシュ。ファイルが無ければ `new T()` |
| `static void Load<T>()` | 明示ロード（通常は Get が内部で呼ぶ）。失敗時は例外 throw |
| `static void Save<T>(T data, bool immediate = false)` | 保存。デフォルトはフレーム末尾集約 + スレッドプール書き込み。`immediate: true` で即時開始 |
| `static bool FileExist<T>()` | 保存ファイルの存在確認 |
| `static void Delete<T>()` | ファイル削除 + メモリキャッシュ削除 |
| `static void DeleteAll()` | 保存ディレクトリごと削除 + 全キャッシュ削除 |

### LocalDataManager（インスタンスメンバー: `LocalDataManager.Instance` 経由）

| メンバー | 説明 |
|---|---|
| `void SetCryptoKey(AesCryptoKey cryptoKey)` | 暗号鍵の設定。起動シーケンスで最初に呼ぶ（Client 側は `InitializeObject.InitializeLocalDataManager`） |
| `void SetFileDirectory(string directory)` | 保存先ディレクトリの変更（無ければ作成） |
| `string GetFilePath<T>()` | 実ファイルパス取得（型→パスのキャッシュあり）。`FileNameAttribute` 未設定・`ILocalData` 未実装なら例外 |
| `void CacheClear()` | 型→パス / 型→インスタンスの両キャッシュをクリア |
| `Observable<ILocalData> OnLoadAsObservable()` | Load 完了時に発火（データキャッシュ登録前） |
| `Observable<ILocalData> OnSaveAsObservable()` | ファイル書き込み直前に発火 |
| `string DefaultFileDirectory` / `string FileDirectory` | 既定 / 現在の保存先ディレクトリ |

### LocalDataExtension / FileNameAttribute / ILocalData

| メンバー | 説明 |
|---|---|
| `data.Save()`（`LocalDataExtension.Save<T>`） | `LocalDataManager.Save(data)` の糖衣。通常はこちらを使う |
| `FileNameAttribute(string fileName, bool encrypt = true)` | 保存ファイル名。encrypt はファイル名の暗号化ハッシュ化のみに作用（本文暗号化は鍵の有無で決まる） |
| `ILocalData` | マーカー interface（メンバーなし） |

## 注意点・罠

- **`[FileName]` 必須**。付け忘れると `GetFilePath` で `FileNameAttribute is not set for this class.` 例外
- **`SetCryptoKey` 前に `Get` / `Load` しない**。ファイル名ハッシュも鍵に依存するため、鍵設定前後でファイルパスが変わり「別ファイル」扱いになる（パスは `filePathCache` にも残る）。初期化順は必ず `InitializeLocalDataManager` → 各種 Get
- **デフォルトの Save は遅延書き込み**（フレーム末尾）。クラッシュ・強制終了ではフレーム内の変更が失われうる。失うと致命的なデータは `Save(data, immediate: true)`
- 同一インスタンスへの同フレーム多重 `Save()` は 1 回の書き込みに集約される（`saveRequests` で抑止）。「連続で呼んでも書き込みは1回」は仕様
- `Get<T>()` が返すのは共有キャッシュインスタンス。書き換えた時点でメモリ上は全参照箇所に反映されるが、**`Save()` を呼ぶまでディスクには書かれない**
- `[FileName]` の名前をクラス間で重複させるとファイルが衝突する（コンパイルエラーにならない）。`Dominion-` プレフィックス + 一意名を厳守
- プロパティのリネーム・型変更は既存端末のデータ喪失につながる（map モードでも名前が変わると読めない）
- **IL2CPP 実機では MessagePack の生成コードが必要**。生成自体は Source Generator で自動だが、属性付け忘れ等はエディタ（DynamicResolver）では動いてしまい、実機に入れて初めて落ちる
- Load 失敗（破損ファイル等）は例外 throw（`LocalData load failed.`）。握り潰されない点に注意
- FileCache モジュール（`Modules.FileCache` の `CacheData`）も内部で本モジュールを利用している（`[FileName("__FileCache_")]`）

## 関連

- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `AesCryptoKey` / `MessagePackFileUtility`
- [MessagePack](MessagePack.md) — `UnityCustomResolver` と Source Generator によるコード生成
- [FileCache](FileCache.md) — 有効期限付きのダウンロードファイルキャッシュ（本モジュール利用側）
- [Cache](Cache.md) — メモリ上のみの一時キャッシュ（永続化しない場合はこちら）
- [Prefs](Prefs.md) — PlayerPrefs ベースの軽量な永続キー値

# Master

> **namespace**: `Modules.Master`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Master/`
> **Client側使用**: 約125ファイル（2026-07時点）
> **依存**: UniTask / R3 / MessagePack(+LZ4) / Extensions（`Singleton<T>`, `AesCryptoKey`）/ Modules.Performance / Modules.Devkit.Console

## 概要

マスターデータ（Excel由来の静的ゲームデータ）の配信・キャッシュ・ロード・参照基盤。1マスター = 1クラス = 1暗号化MessagePackファイル（拡張子 `.master`）で管理する。
Client側は `Dominion.Client.Master.Master<>`（`Client/Assets/Scripts/Client/Master/Core/Master.cs`）が本基盤の `Modules.Master.Master<>` を継承し、ダウンロード実装と static アクセサを追加している。マスター定義クラス本体は `Client/Assets/Scripts/Client/Master/` 配下。

### データの流れ（全体像）

```
[Excel編集]  Master/Masters/<カテゴリ>/<名前>Master/<名前>Master.xlsx
    ↕ MasterConverter (Master/Tools/win/_Export.bat / _Import.bat)
[git管理データ]  同フォルダ Records/*.record (YAML, 1レコード=1ファイル) + <名前>Master.index (レコード順)
    ↓ MasterGenerator.Generate（Unityメニュー Master > Generate / Jenkins: JenkinsMaster）
[配信ファイル]  <名前>.master (MessagePack + LZ4 + AES) + version.txt (rootHash / ファイル別hash・size)
    ↓ MasterS3Uploader → S3 / rootHash を PlayFab TitleData へ (MasterRootHash)
[実行時]  version.txt DL (MasterUpdateManager) → RequireUpdateMasters (差分判定)
          → UpdateMaster (差分DL) → LoadMaster (復号+デシリアライズ+Setup)
```

- 変換ツール `MasterConverter.exe` は Export = xlsx → `.record`(YAML)、Import = `.record` → xlsx再構築。
- ビルド側 `MasterGenerator` / `RecordDataLoader` はエディタ専用（`UniModules/Scripts/Modules/Devkit/MasterGenerator/Editor/`、namespace は同じ `Modules.Master`）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ID指定で1レコード取得 | `ItemMaster.GetRecordByItemId(id)` 等（各マスターの public static アクセサ。**型名で呼ぶ**） |
| 全レコード取得 | `ItemMaster.GetAllRecords()`（`new static`。LINQ と併用） |
| 外部キーでグループ取得 | 各マスターが `OnSetup()` で構築したキャッシュ経由（例: `CharacterSkillMaster.GetRecordsByCharacterId(id)`） |
| 新しいマスターを追加 | 「使い方(実例) > マスター新規追加手順」参照 |
| 全マスター一括ロード | `MasterManager.Instance.LoadMaster()` |
| 更新が必要なマスターの判定 | `MasterManager.Instance.RequireUpdateMasters(versionTable)` |
| マスターの差分ダウンロード | `MasterManager.Instance.UpdateMaster(updateMasters)` |
| ロード完了/更新/エラーの購読 | `OnLoadFinishAsObservable()` / `OnUpdateMasterAsObservable()` / `OnErrorAsObservable()` |
| 配信URL・保存先・暗号化キー設定 | `SetDownloadUrl()` / `SetInstallDirectory()` / `SetCryptoKey()` |
| マスターファイル名の取得 | `MasterManager.Instance.GetMasterFileName<T>()`（暗号化後の実ファイル名） |
| ローカルキャッシュ全削除 | `MasterManager.Instance.ClearMasterCache()` |
| レコードから他マスターのレコード参照 | `Reference.Get(this, nameof(Prop), keySelector, valueSelector)`（キャッシュ付き） |
| ファイル名をクラス名と変えたい | `[FileName("xxx")]` をマスタークラスに付与 |
| MasterViewer 等の表示列を拡張 | `CustomDataAttribute` 派生（Client実装: `ConvertTextDataAttribute` / `ContentNameAttribute`） |
| エディタでDLせずローカル .master を使う | メニュー Master > Use CachedMasterFile（`MasterManager.Prefs.checkVersion` をOFF） |
| ローカルで .master を生成して動作確認 | メニュー Master > Generate (CloneToInstallDirectory) |

## 主要クラス

### 基盤側（`Modules.Master`）

| クラス | 種別 | 役割 |
|---|---|---|
| `IMaster` | interface | マスター共通操作（`Update` / `Prepare` / `Load` / `Setup` / `Delete`）。`MasterManager` が一括制御する単位 |
| `Master<TKey, TMaster, TMasterContainer, TMasterRecord>` | abstract generic（static `Instance` 保持） | 1マスターの基底。`Dictionary<TKey, TMasterRecord>` を保持し、ファイル読込→復号→MessagePackデシリアライズ→登録を担う |
| `MasterContainer<TMasterRecord>` | abstract | MessagePackシリアライズの入れ物。`TMasterRecord[] records` のみ |
| `MasterManager` | Singleton（sealed partial） | 全マスターの登録・一括更新/ロード・ファイルパス/暗号化/LZ4設定・バージョン管理（`.version.cs`）・R3イベント通知 |
| `Reference` | Singleton | レコード→他マスターレコード参照の型別キャッシュ。マスター更新/ロード時に `Clear()` される |
| `FileNameAttribute` | Attribute（class対象） | マスターファイル名をクラス名（Masterサフィックス除去）から上書き |
| `CustomDataAttribute` | abstract Attribute（property対象） | 表示用データ変換の拡張ポイント（MasterViewer等のエディタツールが利用） |
| `IVersionFileHandler` / `DefaultVersionFileHandler` | interface / sealed class | ローカル `version` ファイルの難読化（デフォルト実装は全バイトのビット反転） |
| `MasterManager.Prefs` | エディタ専用（`MasterManager.editor.cs`） | `checkVersion`（ProjectPrefs）。`EnableVersionCheck` と合わせてバージョンチェックの有効/無効を制御 |

### Client側の対応クラス（参考: `Dominion.Client.Master` 等）

| クラス | 場所 | 役割 |
|---|---|---|
| `Master<TKey, TMaster, TMasterContainer, TMasterRecord>` | `Client/Assets/Scripts/Client/Master/Core/Master.cs` | `Download` 実装（`MasterFileDownLoader` + rootHash URL）、Android向け `PrepareLoad`/`FileLoad`、**`new static` の `GetAllRecords` / `GetRecord`** |
| `MasterDefinition` | `Client/Assets/Scripts/Client/Master/MasterDefinition.cs` | 全マスターを `Create()` して登録する一覧（新規マスターはここに追記必須） |
| `MasterUpdateManager` | `Client/Assets/Scripts/Client/Module/Download/MasterUpdateManager.cs` | サーバーの `version.txt` をDLし `VersionTable`（IMaster→hash）を構築 |
| `ContentsUpdateManager` | `Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs` | 起動時の更新フロー（`LoadVersion` → `RequireUpdateMasters` → `UpdateMaster`） |
| `ConvertTextDataAttribute` / `ContentNameAttribute` | `Client/Assets/Scripts/Client/Master/Core/Attribute/` | `CustomDataAttribute` 実装。TextDataキー列の実テキスト表示 / ContentId の名称表示 |

## 使い方(実例)

### 1. ID指定で1レコード取得（最頻出）

```csharp
// 引用: Client/Assets/Scripts/Client/Data/User/UserItemData.cs
var itemRecord = ItemMaster.GetRecordByItemId(ItemId);

if (itemRecord == null){ return; }

Vendor = itemRecord.SellPrice.HasValue && vendor;
```

### 2. 全レコード取得 + LINQ

```csharp
// 引用: Client/Assets/Scripts/Client/Core/Purchase/PurchaseManager.cs
var purchaseRecords = PurchaseMaster.GetAllRecords()
    .Where(x => x.Platform == platform)
    .ToArray();
```

### 3. マスター定義の典型形（単一キー）

```csharp
// 引用: Client/Assets/Scripts/Client/Master/Item/ItemMaster.cs（抜粋）
/// <summary> アイテム定義マスター </summary>
public sealed partial class ItemMaster : Master<uint, ItemMaster, ItemMaster.Container, ItemMaster.Record>
{
    [MessagePackObject(true)]
    public sealed partial class Container : MasterContainer<Record> { }

    [MessagePackObject(true)]
    public sealed partial class Record
    {
        /// <summary> アイテムID </summary>
        public uint ItemId { get; private set; }
        /// <summary> 名称 (TextData) </summary>
        [ConvertTextData]
        public string Name { get; private set; }
        // ... 中略 ...

        public Record() { }

        [SerializationConstructor]
        public Record(uint itemId, string name, /* ... */ string spriteName)
        {
            ItemId = itemId;
            Name = name;
            // ...
        }
    }

    protected override uint GetRecordKey(Record record)
    {
        return record.ItemId;
    }

    public static Record GetRecordByItemId(uint itemId)
    {
        return GetRecord(itemId);    // 基底の protected new static GetRecord (未ヒット時エラーログ+null)
    }
}
```

### 4. 複合キー + OnSetupキャッシュ + Reference参照

```csharp
// 引用: Client/Assets/Scripts/Client/Master/Character/CharacterSkillMaster.cs（抜粋）
public sealed partial class CharacterSkillMaster : Master<Tuple<uint, uint>, CharacterSkillMaster, CharacterSkillMaster.Container, CharacterSkillMaster.Record>
{
    [MessagePackObject(true)]
    public sealed partial class Record
    {
        public uint CharacterId { get; private set; }
        public uint SkillNo { get; private set; }
        public uint SkillId { get; private set; }

        // 他マスターのレコードをキャッシュ付きで参照 (シリアライズ対象外にする).
        [IgnoreMember, JsonIgnore]
        public SkillMaster.Record Skill
        {
            get { return Reference.Get(this, nameof(Skill), x => x.SkillId, x => SkillMaster.GetRecordBySkillId(x)); }
        }
        // ...
    }

    private Dictionary<uint, Record[]> recordByCharacterIdCache = null;

    protected override void OnSetup()
    {
        // 外部キー検索用のキャッシュを構築 (※スレッドプール上で実行される).
        recordByCharacterIdCache = GetAllRecords()
            .GroupBy(x => x.CharacterId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SkillNo).ToArray());
    }

    protected override Tuple<uint, uint> GetRecordKey(Record record)
    {
        return Tuple.Create(record.CharacterId, record.SkillNo);
    }

    public static Record[] GetRecordsByCharacterId(uint characterId)
    {
        return Instance.recordByCharacterIdCache.GetValueOrDefault(characterId, new Record[0]);
    }
}
```

他の実例: `EnemyLootMaster.cs`（DEVELOPMENTビルド限定の整合性チェックを `OnSetup` 内で実施）、`CharacterLevelMaster.cs`（累計経験値テーブルを `OnSetup` で事前計算し `GetLevelTotalExp` 等で公開）、`OrbStatLevelMaster.cs`（`Tuple<StatType, int>` キー + StatType別 Min/Max レベルキャッシュ）。

### 5. 初期化・起動時ロードフロー

```csharp
// 引用: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs（抜粋）
public static void InitializeMasterManager()
{
    // マスターのインスタンスを生成.
    MasterDefinition.GetAllMasters();

    var masterManager = Modules.Master.MasterManager.Instance;

    var keyData = keyFileManager.Get(KeyFileManager.KeyType.MasterData);

    if (keyData != null)
    {
        var cryptoKey = new AesCryptoKey(keyData.Key, keyData.Iv);

        masterManager.SetCryptoKey(cryptoKey);
    }

    masterManager.Lz4Compression = true;
    masterManager.SetDownloadUrl(Urls.MasterUrl);
    masterManager.SetInstallDirectory(UnityPathUtility.PersistentDataPath);
}
```

```csharp
// 引用: Client/Assets/Scripts/Client/Manager/GameStartupManager.cs（抜粋）
systemModel.IsMasterLoaded = await masterManager.LoadMaster();

if (!systemModel.IsMasterLoaded)
{
    SystemModel.ErrorTransitionToTitle(ErrorCode.MasterLoadError);

    return;
}
```

更新判定〜差分DLは `ContentsUpdateManager.BuildRequireUpdateContents()`（`LoadVersion` → `RequireUpdateMasters`）→ `StartUpdate()`（`UpdateMaster`）を参照。

### マスター新規追加手順（Claude向けチェックリスト）

1. **Excelデータ**: `Master/Masters/<カテゴリ>/<名前>Master/` フォルダを作成し、`ClassSchema.xlsx`（列定義。テンプレ: `Master/Template/ClassSchema.xlsx`）・`<名前>Master.xlsx`（データ本体）・`Records/*.record`（YAML、1レコード=1ファイル）・`<名前>Master.index`（レコード順リスト）を用意する。xlsx ⇔ .record の相互変換は `Master/Tools/win/_Export.bat`（xlsx→record）/ `_Import.bat`（record→xlsx）。
   - **罠**: ClassSchema のフィールド名に前後スペースがあると、その列だけ Import で空になる（MasterConverter の Trim 漏れ）。
2. **C#定義**: `Client/Assets/Scripts/Client/Master/<カテゴリ>/<名前>Master.cs` を作成。上記「使い方(実例) 3」の形式を厳守:
   - `Dominion.Client.Master.Master<TKey, TMaster, TMaster.Container, TMaster.Record>` を継承した `sealed partial` クラス
   - ネストクラス名は **必ず `Container` / `Record`**（`MasterGenerator` が `型名+Container` / `型名+Record` の名前でリフレクション解決するため改名不可）
   - `Container` / `Record` に `[MessagePackObject(true)]`、`Record` に引数なしコンストラクタ + `[SerializationConstructor]` 付きコンストラクタ
   - `GetRecordKey` をオーバーライド（複合キーは `Tuple.Create`）
   - 外部公開は `GetRecordByXxxId` 等の public static メソッドを定義（基底の `GetRecord` は protected）
   - 列名の慣習: 閾値・要求条件は `Require*`（例: `RequireExp` = AlchemyCraftLevelMaster、`RequireLevel` = EnemyLootMaster、`RequireNum` = FacilityLevelupMaterialMaster）、範囲上下限は `Min*` / `Max*`（例: `MinAmount`/`MaxAmount` = ItemLootMaster、`MinDrop` = EnemyMaster、`MaxHp`/`MaxLevel` = CharacterStatsMaster）
   - TextDataキーを格納する文字列列には `[ConvertTextData]` を付与
3. **登録**: `Client/Assets/Scripts/Client/Master/MasterDefinition.cs` の `GetAllMasters()` に `<名前>Master.Create(),` をカテゴリ順で追記（**忘れるとロードもInstance生成もされない**）。
4. **ローカル確認**: Unityメニュー `Master > Generate (CloneToInstallDirectory)` で `.record` から `.master` を生成して保存先へ複製 → `Master > Use CachedMasterFile` をONにして起動すればDLなしで確認可能。

## API(主要公開メンバー)

### `Master<TKey, TMaster, TMasterContainer, TMasterRecord>`（基盤側基底）

| メンバー | 説明 |
|---|---|
| `static TMaster Instance` | 唯一のインスタンス。`Create()` 前のアクセスは `InvalidOperationException` |
| `static IMaster Create()` | インスタンス生成 + `MasterManager` へ登録（`MasterDefinition` から呼ぶ） |
| `static bool IsExist()` | インスタンス生成済みか |
| `void Delete()` | `MasterManager` から登録解除しインスタンス破棄 |
| `IEnumerable<TMasterRecord> GetAllRecords()` | 全レコード（Dictionary.Values。順序保証が要るなら OrderBy する） |
| `TMasterRecord GetRecord(TKey key)` | キー指定取得（未ヒットは null。基盤側はログなし） |
| `void SetRecords(IEnumerable<TMasterRecord>)` | レコード一括登録（同一キー重複で例外） |
| `protected IReadOnlyDictionary<TKey, TMasterRecord> Records` | 内部Dictionary への読み取りアクセス |
| `UniTask<Tuple<bool,double>> Prepare / Load / Update`, `Tuple<bool,double> Setup` | ロードパイプライン各段（通常は `MasterManager` 経由で呼ばれ、直接は呼ばない） |
| `protected virtual void OnSetup()` | ロード完了後のキャッシュ構築フック（**スレッドプール上で実行**） |
| `protected virtual void Refresh()` | 内部キャッシュのクリア（ロード直前・エラー時に呼ばれる） |
| `protected virtual void OnError()` | 失敗時フック（デフォルトは `Refresh()`） |
| `protected virtual UniTask PrepareLoad / UniTask<byte[]> FileLoad / byte[] Decrypt / TMasterRecord[] Deserialize` | 読込各段のカスタマイズポイント |
| `protected abstract TKey GetRecordKey(TMasterRecord)` | レコード→キー変換（必須実装） |
| `protected abstract UniTask<bool> Download(string version, CancellationToken)` | ファイル取得（Client側基底が実装済み） |

### Client側基底 `Dominion.Client.Master.Master<>` の追加メンバー

| メンバー | 説明 |
|---|---|
| `public new static IEnumerable<TMasterRecord> GetAllRecords()` | `Instance.GetAllRecords()` の static 版 |
| `protected new static TMasterRecord GetRecord(TKey key)` | static 版 + 未ヒット時に `Master record not found.` のエラーログを出して null を返す |

### `MasterManager`（Singleton）

| メンバー | 説明 |
|---|---|
| `IReadOnlyCollection<IMaster> Masters` | 登録済みマスター一覧 |
| `void Register(IMaster)` / `void Remove(IMaster)` | 登録/解除（通常 `Create()`/`Delete()` 経由） |
| `UniTask<bool> LoadMaster(CancellationToken)` | 全登録マスターを Prepare → Load → Setup の3段で並列一括ロード |
| `UniTask<bool> UpdateMaster(Dictionary<IMaster,string>, IProgress<IMaster>, CancellationToken)` | 指定マスターをDL更新しローカルバージョン記録（25件ずつチャンク実行） |
| `UniTask<IMaster[]> RequireUpdateMasters(Dictionary<IMaster,string> versionTable)` | ローカルバージョン/ファイル有無と突き合わせ、更新が必要なマスターを列挙 |
| `void CancelAll()` | 実行中の更新/ロードをキャンセル |
| `void ClearMasterCache()` | versionファイル + InstallDirectory 内ファイル + Reference キャッシュを削除 |
| `void Clear()` | 全マスターの登録解除（インスタンス破棄） |
| `void SetDownloadUrl(string)` / `void SetInstallDirectory(string)` / `void SetCryptoKey(AesCryptoKey)` | 配信URL / 保存先（`<dir>/Master`、iOSはNoBackupFlag付与）/ AES暗号化キー設定 |
| `bool Lz4Compression` | MessagePackのLZ4圧縮（`Lz4BlockArray`）使用フラグ。デフォルト true |
| `string DownloadUrl` / `string InstallDirectory` / `AesCryptoKey CryptoKey` | 設定値の参照 |
| `string GetFilePath(IMaster)` / `string GetMasterFileName<T>()` / `string GetMasterFileName(Type)` | 保存先フルパス / マスターファイル名（CryptoKey設定時はファイル名自体も暗号化） |
| `static string DeleteMasterSuffix(string)` | 末尾の `Master` を除去（ItemMaster → Item.master のファイル名規則） |
| `MessagePackSerializerOptions GetSerializerOptions()` | `StandardResolverAllowPrivate` + `UnityCustomResolver`（+LZ4）のオプション |
| `Observable<Unit> OnLoadFinishAsObservable()` / `OnUpdateMasterAsObservable()` / `Observable<Exception> OnErrorAsObservable()` | R3イベント（ロード完了 / 1マスター更新毎 / 失敗） |
| `UniTask LoadVersion()` / `UniTask SaveVersion()` / `UniTask ClearVersion(IMaster)` | ローカル `version` ファイル（`ファイル名|hash` 行形式、難読化あり）の読み書き |
| `void SetVersionFileHandler(IVersionFileHandler)` | versionファイル難読化方式の差し替え |
| `bool EnableVersionCheck` | **エディタ専用**。false でバージョンチェックをスキップしローカルファイルを使用 |

### `Reference`（Singleton）

| メンバー | 説明 |
|---|---|
| `static TValue Get<TRecord,TKey,TValue>(record, keyName, keySelector, valueSelector)` | (レコード型, プロパティ名) 単位のキャッシュ付き参照解決 |
| `static void Remove<TRecord,TKey,TValue>(record, keyName, keySelector)` | キャッシュ1件削除 |
| `static void Clear()` | 全キャッシュ削除（`UpdateMaster` / `LoadMaster` / `ClearMasterCache` 冒頭で自動実行） |

## 注意点・罠

1. **`Instance.GetAllRecords()` は CS0176 エラー**。Client側基底が `new static` で隠蔽しているため、`ItemMaster.GetAllRecords()` / `ItemMaster.GetRecordByItemId(id)` のように**必ず型名で呼ぶ**（`Client/Assets/Scripts/Client/Master/Core/Master.cs` L89-109 参照）。
2. **`MasterDefinition.GetAllMasters()` への登録漏れ**に注意。`Create()` されないマスターは `Instance` アクセスで `InvalidOperationException`（"not created."）になり、ロード対象にもならない。
3. **ネストクラス名は `Container` / `Record` 固定**。`MasterGenerator` が `masterType.FullName + "+Container"` / `"+Record"` の文字列でリフレクション解決するため、別名にするとビルド不能。
4. **`GetRecord` は例外を投げない**。Client側 static 版は未ヒットでエラーログ + null 返し。呼び出し側で `if (record == null){ return; }` の null チェックが必須。
5. **同一キーのレコード重複は `SetRecords` で例外**（"Records same key already exists."）。複合キーの粒度不足に注意。
6. **`OnSetup()` / `Load` はスレッドプール上で実行される**。`OnSetup` 内で Unity API（GameObject, Debug.Log以外のUnityEngine機能等）を呼ばない。DEVELOPMENTビルド限定の整合性チェック＋キャッシュ構築に留める（実例: `EnemyLootMaster.OnSetup`）。
7. **`GetAllRecords()` の列挙順は登録順（Dictionary.Values）**。順序に意味がある場合は `OrderBy` する（実例は `OnSetup` 内で `OrderBy` してキャッシュ）。
8. **暗号化**: `.master` の中身（AES）＋ファイル名（AES, `SetCryptoKey` 設定時）＋ローカル `version` ファイル（`IVersionFileHandler`、デフォルトはビット反転）の3層。実ファイル名の確認はエディタメニュー Master > Open MasterFileNameViewer。
9. **バージョン管理は2系統**。サーバー側 `version.txt`（rootHash + マスター別hash、`MasterUpdateManager` が構築）と、ローカル `version` ファイル（DL成功時に `UpdateVersion` → 1秒デバウンスで `SaveVersion`）。差分判定は `RequireUpdateMasters`。ロード失敗時は該当マスターのバージョン情報を消して次回強制更新。
10. **エディタで最新マスターが反映されない場合**: Master > Use CachedMasterFile がONだとバージョンチェックが無効化されローカルキャッシュを使い続ける（`MasterManager.Prefs.checkVersion`、トグルは `UniModules/Scripts/Editor/EditorMenu.cs`）。逆にDLを避けてローカル生成分を使いたい場合はONにする。
11. **列名の慣習**: 閾値・要求条件は `Require*`、範囲上下限は `Min*` / `Max*`（実例は「マスター新規追加手順」2 参照）。新規列追加時はこの使い分けに従う。
12. **テキストはキー文字列で持つ**: マスターにはTextDataのキー（例: `Item-Name_708001`）を格納し、実行時に `TextData.Get` で解決する。日本語直書き禁止（`[ConvertTextData]` はエディタのMasterViewer表示用変換）。
13. `AesCryptoKey` はスレッドセーフではない（基盤側は `Load` 時にスレッド専用キーを複製して対応済み。独自に `Decrypt` を触る場合は同様の配慮が必要）。

## 関連

- [MessagePack](MessagePack.md) — シリアライズ基盤（`UnityCustomResolver`）。Editorでの動的コード生成/実機の事前生成コードに関わる
- [TextData](TextData.md) — マスターに格納した文字列キーの実行時解決（`TextData.Get`）
- [ExternalAsset](ExternalAsset.md) — マスターと並んで起動時に更新される外部アセット配信基盤（`ContentsUpdateManager` が両方を統括）
- [PlayFab](PlayFab.md) — `MasterRootHash` を PlayFab TitleData から取得して配信バージョンを決定
- [Devkit](Devkit.md) — `MasterGenerator` / `MasterConfig` / MasterViewer 等のエディタ専用ツール群（`Modules/Devkit/MasterGenerator/Editor/`）
- [Performance](Performance.md) — 更新時のフレーム分散（`FunctionFrameLimiter`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `AesCryptoKey`

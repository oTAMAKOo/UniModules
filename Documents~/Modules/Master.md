# Master

> **namespace**: `Modules.Master`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Master/`
> **Client側使用**: 約125ファイル（2026-07時点）
> **依存**: UniTask / R3 / MessagePack(+LZ4) / Extensions（`Singleton<T>`, `AesCryptoKey`）/ Modules.Performance / Modules.Devkit.Console

## 概要

マスターデータ（Excel由来の静的ゲームデータ）の配信・キャッシュ・ロード・参照基盤。1マスター = 1クラス = 1暗号化MessagePackファイル（拡張子 `.master`）で管理する。
Client側は `Dominion.Client.Master.Master<>`（`Client/Assets/Scripts/Client/Master/Core/Master.cs`）が本基盤の `Modules.Master.Master<>` を継承し、ダウンロード実装と static アクセサ（**`new static` の `GetAllRecords` / `GetRecord`**。後者は未ヒット時エラーログ + null）を追加している。マスター定義クラス本体は `Client/Assets/Scripts/Client/Master/` 配下。
主要クラス（基盤側）: `IMaster`（マスター共通操作。`MasterManager` が一括制御する単位）/ `Master<TKey, TMaster, TMasterContainer, TMasterRecord>`（1マスターの基底。`Dictionary<TKey, TMasterRecord>` を保持し、ファイル読込→復号→デシリアライズ→登録を担う）/ `MasterContainer<TMasterRecord>`（MessagePackシリアライズの入れ物）/ `MasterManager`（Singleton。全マスターの登録・一括更新/ロード・パス/暗号化/LZ4設定・バージョン管理・R3イベント通知）/ `Reference`（レコード→他マスターレコード参照の型別キャッシュ。マスター更新/ロード時に自動 `Clear()`）/ `FileNameAttribute`（マスターファイル名の上書き）/ `CustomDataAttribute`（MasterViewer 等の表示用データ変換の拡張ポイント）。

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

### Client側の対応クラス

| クラス | 場所 | 役割 |
|---|---|---|
| `Master<TKey, TMaster, TMasterContainer, TMasterRecord>` | `Client/Assets/Scripts/Client/Master/Core/Master.cs` | `Download` 実装（`MasterFileDownLoader` + rootHash URL）、Android向け `PrepareLoad`/`FileLoad`、**`new static` の `GetAllRecords` / `GetRecord`** |
| `MasterDefinition` | `Client/Assets/Scripts/Client/Master/MasterDefinition.cs` | 全マスターを `Create()` して登録する一覧（新規マスターはここに追記必須） |
| `MasterUpdateManager` | `Client/Assets/Scripts/Client/Module/Download/MasterUpdateManager.cs` | サーバーの `version.txt` をDLし `VersionTable`（IMaster→hash）を構築 |
| `ContentsUpdateManager` | `Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs` | 起動時の更新フロー（`LoadVersion` → `RequireUpdateMasters` → `UpdateMaster`） |
| `ConvertTextDataAttribute` / `ContentNameAttribute` | `Client/Assets/Scripts/Client/Master/Core/Attribute/` | `CustomDataAttribute` 実装。TextDataキー列の実テキスト表示 / ContentId の名称表示 |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ID指定で1レコード取得 | `ItemMaster.GetRecordByItemId(id)` 等（各マスターの public static アクセサ。**型名で呼ぶ**） |
| 全レコード取得 | `ItemMaster.GetAllRecords()`（`new static`。LINQ と併用） |
| 外部キーでグループ取得 | 各マスターが `OnSetup()` で構築したキャッシュ経由（例: `CharacterSkillMaster.GetRecordsByCharacterId(id)`） |
| 新しいマスターを追加 | 「使い方 > マスター新規追加手順」参照 |
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

## 使い方

### 定型パターン（引用元）

- ID指定で1レコード取得（最頻出。取得後の null チェック必須）: `Client/Assets/Scripts/Client/Data/User/UserItemData.cs`
- 全レコード取得 + LINQ: `Client/Assets/Scripts/Client/Core/Purchase/PurchaseManager.cs`
- マスター定義の典型形（単一キー）: `Client/Assets/Scripts/Client/Master/Item/ItemMaster.cs`
- 複合キー（`Tuple.Create`）+ OnSetupキャッシュ + `Reference` 参照（`[IgnoreMember, JsonIgnore]` でシリアライズ対象外にする）: `Client/Assets/Scripts/Client/Master/Character/CharacterSkillMaster.cs`
- その他の実例: `EnemyLootMaster.cs`（DEVELOPMENTビルド限定の整合性チェックを `OnSetup` 内で実施）、`CharacterLevelMaster.cs`（累計経験値テーブルを `OnSetup` で事前計算し `GetLevelTotalExp` 等で公開）、`OrbStatLevelMaster.cs`（`Tuple<StatType, int>` キー + StatType別 Min/Max レベルキャッシュ）
- 初期化（暗号化キー・LZ4・配信URL・保存先の設定）: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs` の `InitializeMasterManager`
- 起動時ロード: `Client/Assets/Scripts/Client/Manager/GameStartupManager.cs`（`LoadMaster` 失敗時は `ErrorCode.MasterLoadError` でタイトルへ）
- 更新判定〜差分DL: `ContentsUpdateManager.BuildRequireUpdateContents()`（`LoadVersion` → `RequireUpdateMasters`）→ `StartUpdate()`（`UpdateMaster`）

### マスター新規追加手順（Claude向けチェックリスト）

1. **Excelデータ**: `Master/Masters/<カテゴリ>/<名前>Master/` フォルダを作成し、`ClassSchema.xlsx`（列定義。テンプレ: `Master/Template/ClassSchema.xlsx`）・`<名前>Master.xlsx`（データ本体）・`Records/*.record`（YAML、1レコード=1ファイル）・`<名前>Master.index`（レコード順リスト）を用意する。xlsx ⇔ .record の相互変換は `Master/Tools/win/_Export.bat`（xlsx→record）/ `_Import.bat`（record→xlsx）。
   - **罠**: ClassSchema のフィールド名に前後スペースがあると、その列だけ Import で空になる（MasterConverter の Trim 漏れ）。
2. **C#定義**: `Client/Assets/Scripts/Client/Master/<カテゴリ>/<名前>Master.cs` を作成。`Client/Assets/Scripts/Client/Master/Item/ItemMaster.cs` の形式を厳守:
   - `Dominion.Client.Master.Master<TKey, TMaster, TMaster.Container, TMaster.Record>` を継承した `sealed partial` クラス
   - ネストクラス名は **必ず `Container` / `Record`**（`MasterGenerator` が `型名+Container` / `型名+Record` の名前でリフレクション解決するため改名不可）
   - `Container` / `Record` に `[MessagePackObject(true)]`、`Record` に引数なしコンストラクタ + `[SerializationConstructor]` 付きコンストラクタ
   - `GetRecordKey` をオーバーライド（複合キーは `Tuple.Create`）
   - 外部公開は `GetRecordByXxxId` 等の public static メソッドを定義（基底の `GetRecord` は protected）
   - 列名の慣習: 閾値・要求条件は `Require*`（例: `RequireExp` = AlchemyCraftLevelMaster、`RequireLevel` = EnemyLootMaster、`RequireNum` = FacilityLevelupMaterialMaster）、範囲上下限は `Min*` / `Max*`（例: `MinAmount`/`MaxAmount` = ItemLootMaster、`MinDrop` = EnemyMaster、`MaxHp`/`MaxLevel` = CharacterStatsMaster）
   - TextDataキーを格納する文字列列には `[ConvertTextData]` を付与
3. **登録**: `Client/Assets/Scripts/Client/Master/MasterDefinition.cs` の `GetAllMasters()` に `<名前>Master.Create(),` をカテゴリ順で追記（**忘れるとロードもInstance生成もされない**）。
4. **ローカル確認**: Unityメニュー `Master > Generate (CloneToInstallDirectory)` で `.record` から `.master` を生成して保存先へ複製 → `Master > Use CachedMasterFile` をONにして起動すればDLなしで確認可能。

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

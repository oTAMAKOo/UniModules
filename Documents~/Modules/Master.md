# Master

> **namespace**: `Modules.Master`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Master/`
> **依存**: UniTask / R3 / MessagePack(+LZ4) / Extensions（`Singleton<T>`, `AesCryptoKey`）/ Modules.Performance / Modules.Devkit.Console

## 概要

マスターデータ（Excel由来の静的ゲームデータ）の配信・キャッシュ・ロード・参照基盤。1マスター = 1クラス = 1暗号化MessagePackファイル（拡張子 `.master`）で管理する。
主要クラス: `IMaster`（マスター共通操作。`MasterManager` が一括制御する単位）/ `Master<TKey, TMaster, TMasterContainer, TMasterRecord>`（1マスターの基底。`Dictionary<TKey, TMasterRecord>` を保持し、ファイル読込→復号→デシリアライズ→登録を担う）/ `MasterContainer<TMasterRecord>`（MessagePackシリアライズの入れ物）/ `MasterManager`（Singleton。全マスターの登録・一括更新/ロード・パス/暗号化/LZ4設定・バージョン管理・R3イベント通知）/ `Reference`（レコード→他マスターレコード参照の型別キャッシュ。マスター更新/ロード時に自動 `Clear()`）/ `FileNameAttribute`（マスターファイル名の上書き）/ `CustomDataAttribute`（MasterViewer 等の表示用データ変換の拡張ポイント）。

### データの流れ（全体像）

```
[Excel編集]  Master/Masters/<カテゴリ>/<名前>Master/<名前>Master.xlsx
    ↕ MasterConverter (Master/Tools/win/_Export.bat / _Import.bat)
[git管理データ]  同フォルダ Records/*.record (YAML, 1レコード=1ファイル) + <名前>Master.index (レコード順)
    ↓ MasterGenerator.Generate（Unityメニュー Master > Generate / CI: JenkinsMaster）
[配信ファイル]  <名前>.master (MessagePack + LZ4 + AES) + version.txt (rootHash / ファイル別hash・size)
    ↓ MasterS3Uploader → 配信ストレージ / rootHash を PlayFab TitleData 等へ
[実行時]  version.txt DL → RequireUpdateMasters (差分判定)
          → UpdateMaster (差分DL) → LoadMaster (復号+デシリアライズ+Setup)
```

- 変換ツール `MasterConverter.exe` は Export = xlsx → `.record`(YAML)、Import = `.record` → xlsx再構築。
- ビルド側 `MasterGenerator` / `RecordDataLoader` はエディタ専用（`UniModules/Scripts/Modules/Devkit/MasterGenerator/Editor/`、namespace は同じ `Modules.Master`）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ID指定で1レコード取得 | 各マスター派生クラスの public static アクセサ（利用側で追加、`GetRecord` は public） |
| 全レコード取得 | `masterInstance.GetAllRecords()`（`IEnumerable<TRecord>`） |
| 外部キーでグループ取得 | 各マスターが `OnSetup()` で構築したキャッシュ経由 |
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
| MasterViewer 等の表示列を拡張 | `CustomDataAttribute` 派生 |
| エディタでDLせずローカル .master を使う | メニュー Master > Use CachedMasterFile（`MasterManager.Prefs.checkVersion` をOFF） |
| ローカルで .master を生成して動作確認 | メニュー Master > Generate (CloneToInstallDirectory) |

## 使い方

### マスター新規追加手順

1. **Excelデータ**: `Master/Masters/<カテゴリ>/<名前>Master/` フォルダを作成し、`ClassSchema.xlsx`（列定義。テンプレ: `Master/Template/ClassSchema.xlsx`）・`<名前>Master.xlsx`（データ本体）・`Records/*.record`（YAML、1レコード=1ファイル）・`<名前>Master.index`（レコード順リスト）を用意する。xlsx ⇔ .record の相互変換は `Master/Tools/win/_Export.bat`（xlsx→record）/ `_Import.bat`（record→xlsx）。
   - **罠**: ClassSchema のフィールド名に前後スペースがあると、その列だけ Import で空になる（MasterConverter の Trim 漏れ）。
2. **C#定義**: `Modules.Master.Master<TKey, TMaster, TMaster.Container, TMaster.Record>` を継承した `sealed partial` クラスを作成する。
   - ネストクラス名は **必ず `Container` / `Record`**（`MasterGenerator` が `型名+Container` / `型名+Record` の名前でリフレクション解決するため改名不可）
   - `Container` / `Record` に `[MessagePackObject(true)]`、`Record` に引数なしコンストラクタ + `[SerializationConstructor]` 付きコンストラクタ
   - `GetRecordKey` をオーバーライド（複合キーは `Tuple.Create`）
   - 基底の `GetRecord` は public。利用側で用途に応じたラッパー public static を提供する慣例
   - 列名の慣習: 閾値・要求条件は `Require*`（例: `RequireExp`, `RequireLevel`, `RequireNum`）、範囲上下限は `Min*` / `Max*`（例: `MinAmount`/`MaxAmount`, `MinDrop`, `MaxHp`/`MaxLevel`）
   - TextDataキーを格納する文字列列には `[ConvertTextData]` 等のカスタム属性を付与すると MasterViewer で実テキストを表示できる
3. **登録**: `MasterManager` に対して各マスターの `Create()` を呼び登録する（`GetAllMasters()` 相当の一覧を利用側で保持する。**忘れるとロードもInstance生成もされない**）。
4. **ローカル確認**: Unityメニュー `Master > Generate (CloneToInstallDirectory)` で `.record` から `.master` を生成して保存先へ複製 → `Master > Use CachedMasterFile` をONにして起動すればDLなしで確認可能。

## 注意点・罠

1. **`MasterManager` への登録漏れ**に注意。`Create()` されないマスターは `Instance` アクセスで `InvalidOperationException`（"not created."）になり、ロード対象にもならない。
2. **ネストクラス名は `Container` / `Record` 固定**。`MasterGenerator` が `masterType.FullName + "+Container"` / `"+Record"` の文字列でリフレクション解決するため、別名にするとビルド不能。
3. **`GetRecord` は例外を投げない**（public。未ヒットで null）。利用側の static アクセサでラップする場合も未ヒット時の挙動（null 返し + エラーログ等）を統一しておくとよい。呼び出し側で `if (record == null){ return; }` の null チェックは必須。
4. **同一キーのレコード重複は `SetRecords` で例外**（"Records same key already exists."）。複合キーの粒度不足に注意。
5. **`OnSetup()` / `Load` はスレッドプール上で実行される**。`OnSetup` 内で Unity API（GameObject, Debug.Log以外のUnityEngine機能等）を呼ばない。整合性チェック＋キャッシュ構築に留める。
6. **`GetAllRecords()` の列挙順は登録順（Dictionary.Values）**。順序に意味がある場合は `OrderBy` する（`OnSetup` 内で `OrderBy` してキャッシュする形が定石）。
7. **暗号化**: `.master` の中身（AES）＋ファイル名（AES, `SetCryptoKey` 設定時）＋ローカル `version` ファイル（`IVersionFileHandler`、デフォルトはビット反転）の3層。実ファイル名の確認はエディタメニュー Master > Open MasterFileNameViewer。
8. **バージョン管理は2系統**。サーバー側 `version.txt`（rootHash + マスター別hash）と、ローカル `version` ファイル（DL成功時に `UpdateVersion` → 1秒デバウンスで `SaveVersion`）。差分判定は `RequireUpdateMasters`。ロード失敗時は該当マスターのバージョン情報を消して次回強制更新。
9. **エディタで最新マスターが反映されない場合**: Master > Use CachedMasterFile がONだとバージョンチェックが無効化されローカルキャッシュを使い続ける（`MasterManager.Prefs.checkVersion`、トグルは `UniModules/Scripts/Editor/EditorMenu.cs`）。逆にDLを避けてローカル生成分を使いたい場合はONにする。
10. **列名の慣習**: 閾値・要求条件は `Require*`、範囲上下限は `Min*` / `Max*`（実例は「マスター新規追加手順」2 参照）。新規列追加時はこの使い分けに従う。
11. **テキストはキー文字列で持つ**: マスターにはTextDataのキー（例: `Item-Name_708001`）を格納し、実行時に `TextData.Get` で解決する。日本語直書き禁止（`[ConvertTextData]` はエディタのMasterViewer表示用変換）。
12. `AesCryptoKey` はスレッドセーフではない（基盤側は `Load` 時にスレッド専用キーを複製して対応済み。独自に `Decrypt` を触る場合は同様の配慮が必要）。

## 関連

- [MessagePack](MessagePack.md) — シリアライズ基盤（`UnityCustomResolver`）。Editorでの動的コード生成/実機の事前生成コードに関わる
- [TextData](TextData.md) — マスターに格納した文字列キーの実行時解決（`TextData.Get`）
- [ExternalAsset](ExternalAsset.md) — マスターと並んで起動時に更新される外部アセット配信基盤
- [PlayFab](PlayFab.md) — 配信バージョン（rootHash）を TitleData 等から取得する連携先の例
- [Devkit](Devkit.md) — `MasterGenerator` / `MasterConfig` / MasterViewer 等のエディタ専用ツール群（`Modules/Devkit/MasterGenerator/Editor/`）
- [Performance](Performance.md) — 更新時のフレーム分散（`FunctionFrameLimiter`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `AesCryptoKey`

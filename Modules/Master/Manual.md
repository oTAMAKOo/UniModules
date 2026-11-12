# Master

マスターデータを扱う機能。
マスターファイルの更新判定、ロード、キーでのレコード取得、ほかのマスターへの参照のキャッシュができる。
ファイルのダウンロード処理は、マスターの基底クラスを継承する側で実装する。

## 基本情報

> **namespace**: `Modules.Master`
> **場所**: `Modules/Master/`
> **依存**: UniTask / R3 / MessagePack(+LZ4) / Extensions（`Singleton<T>`, `AesCryptoKey`）/ Modules.MessagePack（`UnityCustomResolver`）/ Modules.Performance / Modules.Devkit.Console / エディタのみ: Modules.Devkit.Prefs

## 概要

マスターデータ（Excel由来の静的ゲームデータ）の配信・キャッシュ・ロード・参照基盤。1マスター = 1クラス = 1暗号化MessagePackファイル（拡張子 `.master`）で管理する。
主要クラス: `IMaster`（マスター共通操作。`MasterManager` が一括制御する単位）/ `Master<TKey, TMaster, TMasterContainer, TMasterRecord>`（1マスターの基底。`Dictionary<TKey, TMasterRecord>` を保持し、ファイル読込→復号→デシリアライズ→登録を担う）/ `MasterContainer<TMasterRecord>`（MessagePackシリアライズの入れ物）/ `MasterManager`（Singleton。全マスターの登録・一括更新/ロード・パス/暗号化/LZ4設定・バージョン管理・R3イベント通知）/ `Reference`（レコード→他マスターレコード参照の型別キャッシュ。マスター更新/ロード時に自動 `Clear()`）/ `FileNameAttribute`（マスターファイル名の上書き）/ `CustomDataAttribute`（MasterViewer 等の表示用データ変換の拡張ポイント）。

### データの流れ（全体像）

```
[git管理データ]  <名前>Master/Records/*.record (YAML, 1レコード=1ファイル) + <名前>Master.index (レコード順)
    ↓ MasterGenerator.Generate（メニュー・CI からの呼び出し登録は利用側で行う）
[配信ファイル]  <名前>.master (MessagePack + LZ4 + AES) + version.txt (rootHash / ファイル別hash・size)
    ↓ MasterS3Uploader → 配信ストレージ / rootHash を PlayFab TitleData 等へ
[実行時]  LoadVersion (ローカルの version ファイル読込)
          → 配信側のバージョン取得（version.txt の取得・解析は利用側で行い、マスター別のバージョン表を作る）
          → RequireUpdateMasters (差分判定)
          → UpdateMaster (差分DL。実DLは各マスターの Download 実装) → LoadMaster (Prepare → 復号+デシリアライズ → Setup)
```

- 保存先は `{SetInstallDirectory で渡したパス}/Master`（既定は persistentDataPath）。ファイル名はクラス名から末尾の `Master` を除いたもの + `.master`（`[FileName]` で上書き可。`SetCryptoKey` 設定時は暗号化した名前）。

- `.record`(YAML) の原本管理（Excel 等との相互変換運用）は利用プロジェクト側で行う。
- ビルド側 `MasterGenerator` / `RecordDataLoader` はエディタ専用（`UniModules/Modules/Devkit/MasterGenerator/Editor/`、namespace は同じ `Modules.Master`）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ID指定で1レコード取得 | 各マスター派生クラスの public static アクセサ（利用側で追加、`GetRecord` は public） |
| 全レコード取得 | `masterInstance.GetAllRecords()`（`IEnumerable<TRecord>`） |
| 外部キーでグループ取得 | 各マスターが `OnSetup()` で構築したキャッシュ経由 |
| 新しいマスターを追加 | 「使い方 > マスター新規追加手順」参照 |
| 全マスター一括ロード | `await MasterManager.Instance.LoadMaster(cancelToken)`（成否を bool で返す） |
| ローカルのバージョン情報を読む（起動時） | `await MasterManager.Instance.LoadVersion()` |
| 更新が必要なマスターの判定 | `await MasterManager.Instance.RequireUpdateMasters(versionTable)`（`Dictionary<IMaster, string>` を渡し `IMaster[]` を得る） |
| マスターの差分ダウンロード | `await MasterManager.Instance.UpdateMaster(updateMasters, progress, cancelToken)`（`Dictionary<IMaster, string>` = マスターと新バージョン。`IProgress<IMaster>` で1件ごとに通知。1件でも更新に失敗すると false を返し、`OnErrorAsObservable` に例外を流す） |
| 更新・ロードを中断 | `MasterManager.Instance.CancelAll()` |
| 登録済みマスターの一覧 / 全登録解除 | `MasterManager.Instance.Masters` / `Clear()` |
| ロード完了/更新/エラーの購読 | `OnLoadFinishAsObservable()` / `OnUpdateMasterAsObservable()` / `OnErrorAsObservable()` |
| 配信URL・保存先・暗号化キー設定 | `SetDownloadUrl()` / `SetInstallDirectory()` / `SetCryptoKey()` |
| LZ4 圧縮の有無を切り替える | `MasterManager.Instance.Lz4Compression`（既定 true。`GetSerializerOptions()` に反映） |
| ローカル version ファイルの難読化を差し替える | `SetVersionFileHandler(IVersionFileHandler)` |
| マスターファイル名の取得 | `MasterManager.Instance.GetMasterFileName<T>()`（暗号化後の実ファイル名） |
| ローカルキャッシュ全削除 | `MasterManager.Instance.ClearMasterCache()` |
| レコードから他マスターのレコード参照 | `Reference.Get(this, nameof(Prop), keySelector, valueSelector)`（キャッシュ付き） |
| ファイル名をクラス名と変えたい | `[FileName("xxx")]` をマスタークラスに付与 |
| MasterViewer 等の表示列を拡張 | `CustomDataAttribute` 派生 |
| エディタでDLせずローカル .master を使う | メニュー `Extension/Master/Use CachedMasterFile`（`MasterManager.Prefs.checkVersion` をOFF） |
| ローカルで .master を生成して動作確認 | `MasterGenerator.Generate`（メニュー登録は利用側） |

## 使い方

### マスター新規追加手順

1. **レコードデータ**: `<名前>Master/` フォルダに `Records/*.record`（YAML、1レコード=1ファイル）と `<名前>Master.index`（レコード順リスト）を用意する（原本 Excel と .record の変換運用は利用プロジェクト側のドキュメントを参照）。
2. **C#定義**: `Modules.Master.Master<TKey, TMaster, TMaster.Container, TMaster.Record>` を継承した `sealed partial` クラスを作成する。
   - ネストクラス名は **必ず `Container` / `Record`**（`MasterGenerator` が `型名+Container` / `型名+Record` の名前でリフレクション解決するため改名不可）
   - `Container` / `Record` に `[MessagePackObject(true)]`、`Record` に引数なしコンストラクタ + `[SerializationConstructor]` 付きコンストラクタ
   - abstract の `GetRecordKey`（複合キーは `Tuple.Create`）と `Download(version, cancelToken)`（保存先 `MasterManager.GetFilePath(this)` へファイルを取得し成否を返す）を実装する。`Download` は利用側で共通の中間基底クラスにまとめるとよい
   - 必要に応じて `OnSetup`（キャッシュ構築）/ `Refresh`（保持データのクリア。ロード前とエラー時に呼ばれる）/ `PrepareLoad`（ロード前の準備）をオーバーライド
   - 基底の `GetRecord` は public。利用側で用途に応じたラッパー public static を提供する慣例
   - 文字列列の表示を変換するカスタム属性（`CustomDataAttribute` 派生、利用側実装）を付与すると MasterViewer で変換後テキストを表示できる
3. **登録**: `MasterManager` に対して各マスターの `Create()` を呼び登録する（`GetAllMasters()` 相当の一覧を利用側で保持する。**忘れるとロードもInstance生成もされない**）。
4. **ローカル確認**: `MasterGenerator.Generate`（メニュー登録は利用側）で `.record` から `.master` を生成して保存先へ複製 → `Extension/Master/Use CachedMasterFile` をONにして起動すればDLなしで確認可能。

## 注意点・罠

1. **`MasterManager` への登録漏れ**に注意。`Create()` されないマスターは `Instance` アクセスで `InvalidOperationException`（"not created."）になり、ロード対象にもならない。
2. **ネストクラス名は `Container` / `Record` 固定**。`MasterGenerator` が `masterType.FullName + "+Container"` / `"+Record"` の文字列でリフレクション解決するため、別名にするとビルド不能。
3. **`GetRecord` は例外を投げない**（public。未ヒットで null）。利用側の static アクセサでラップする場合も未ヒット時の挙動（null 返し + エラーログ等）を統一しておくとよい。呼び出し側で `if (record == null){ return; }` の null チェックは必須。
4. **同一キーのレコード重複は `SetRecords` で例外**（"Records same key already exists."）。複合キーの粒度不足に注意。
5. **`OnSetup()` / `Load` はスレッドプール上で実行される**。`OnSetup` 内で Unity API（GameObject, Debug.Log以外のUnityEngine機能等）を呼ばない。整合性チェック＋キャッシュ構築に留める。
6. **`GetAllRecords()` の列挙順は登録順（Dictionary.Values）**。順序に意味がある場合は `OrderBy` する（`OnSetup` 内で `OrderBy` してキャッシュする形が定石）。
7. **暗号化**: `.master` の中身（AES）＋ファイル名（AES, `SetCryptoKey` 設定時）＋ローカル `version` ファイル（`IVersionFileHandler`、デフォルトはビット反転）の3層。実ファイル名の確認は `MasterFileNameWindow<T>` 派生ウィンドウで行う（メニュー登録は利用側）。
8. **バージョン管理は2系統**。サーバー側 `version.txt`（rootHash + マスター別hash）と、ローカル `version` ファイル（DL成功時に `UpdateVersion` → 1秒後に `SaveVersion`。`UpdateMaster` の最後にも保存する）。ローカル側は起動時に `LoadVersion()` で読む（呼ばないとバージョン情報が空のままで全マスターが更新対象になる）。差分判定は `RequireUpdateMasters`（ファイルが無い、またはローカルのバージョンと不一致なら更新対象）。ロード失敗時は該当マスターのファイルとバージョン情報を消して次回強制更新。保存先が StreamingAssets 配下のときは version ファイルを保存せず、`ClearMasterCache` もファイルを消さない。
9. **`OnErrorAsObservable` に流れる例外は null のことがある**。`LoadMaster` で Prepare / Load / Setup が失敗（例外はマスター内でログ出力済み）した場合、例外オブジェクトを持たないまま通知される。購読側で null を考慮する。購読が無い場合は `Debug.LogException` に出す。
10. **エディタで最新マスターが反映されない場合**: `Extension/Master/Use CachedMasterFile` がONだとバージョンチェックが無効化されローカルキャッシュを使い続ける（`MasterManager.Prefs.checkVersion`、トグルは `UniModules/Editor/EditorMenu.cs`）。逆にDLを避けてローカル生成分を使いたい場合はONにする。
11. **テキストはキー文字列で持つ**: マスターにはTextDataのキー（「シート名-Enum名」形式）を格納し、実行時に `TextData.Get` で解決する。テキスト直書き禁止（MasterViewer 上の表示変換はカスタム属性で行う。「マスター新規追加手順」2 参照）。
12. `AesCryptoKey` はスレッドセーフではない（基盤側は `Load` 時にスレッド専用キーを複製して対応済み。独自に `Decrypt` を触る場合は同様の配慮が必要）。

## 関連

- [MessagePack](../MessagePack/Manual.md) — シリアライズ基盤（`UnityCustomResolver`）。Editorでの動的コード生成/実機の事前生成コードに関わる
- [TextData](../TextData/Manual.md) — マスターに格納した文字列キーの実行時解決（`TextData.Get`）
- [ExternalAssets](../ExternalAssets/Manual.md) — マスターと並んで起動時に更新される外部アセット配信基盤
- [PlayFab](../PlayFab/Manual.md) — 配信バージョン（rootHash）を TitleData 等から取得する連携先の例
- [Devkit](../Devkit/Manual.md) — `MasterGenerator` / `MasterConfig` / MasterViewer 等のエディタ専用ツール群（`Modules/Devkit/MasterGenerator/Editor/`）
- [Performance](../Performance/Manual.md) — 更新時のフレーム分散（`FunctionFrameLimiter`）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Singleton<T>` / `AesCryptoKey`

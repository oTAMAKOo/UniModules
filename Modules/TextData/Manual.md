# TextData

多言語のテキストを管理する機能。
Excel で書いたテキストを暗号化したアセットにし、enum や文字列キーで取り出せる。
テキストの生成にはエディタと Excel 変換ツールが必要。

## 基本情報

> **namespace**: `Modules.TextData`（`TextData` 本体と生成コード） / `Modules.TextData.Components`（`TextDataBase<T>`・`TextDataAsset`・`TextType`・`TextSetter`・セレクタ・検証） / `Modules.TextData.Editor`（生成ツール・設定・Excel 連携）
> **場所**: `Modules/TextData/`（実行時コードは直下と `TextSetter/`、エディタ専用は `Editor/` と `TextSetter/Editor/`。メニュー定義は基盤の `Editor/EditorMenu.cs`）
> **依存**: R3（Observable/Subject）/ UniTask（Editor）/ TMPro・uGUI Text / Extensions（Singleton, AesCryptoKey, PathUtility）/ Modules.Devkit（Editor）/ Modules.Localize（Editor: EditorLanguage）/ Newtonsoft.Json・YamlDotNet（Editor: 中間データの読み込み）

## 概要

ローカライズ対応テキスト管理基盤。Excel を原本とするテキストを AES 暗号化済み `TextDataAsset`（ScriptableObject）としてロードし、生成された enum または文字列キーで実行時に取得する。
内蔵（Internal: アプリ同梱、enum アクセス）と配信（External: DL配信、マスターの文字列キーでアクセス）の2系統がある。
主要クラス（実行時）: `TextData`（Singleton・partial。`Get`/`Format`/`LoadEmbedded`/`AddContents`。生成コードと partial で合成）/ `TextDataBase<T>`（辞書管理・暗号キー・検索の基底）/ `TextDataAsset`（テキスト実体）/ `TextType`（Internal / External）/ `TextSetter`（`[ExecuteAlways]`。同一 GameObject の `Text` / `TextMeshProUGUI` に自動適用）。
エディタ専用: `TextDataGenerator`（yaml→アセット+コード生成）/ `GenerateWindow` / `TextDataExcel`（変換ツールの起動。Import / Export / Open）/ `TextDataAssetUpdater`（Excel 監視・自動生成）/ `TextDataLoader`（起動/コンパイル後の再読込）/ `TextDataConfig`（設定アセット）/ `LanguageManager`・`LanguageInfo`（言語の登録と選択中の言語）/ `SelectorWindow` / `TextDataValidationWindow`・`TextDataValidator` ほか。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| アプリ同梱テキストを取得したい | `TextData.Get(TextData.General.Close)`（カテゴリ enum 指定） |
| プレースホルダ入りテキストを書式化したい | `TextData.Format(TextData.General.Time_Days, days)` |
| マスターデータのテキスト（スキル名・アイテム名等）を取得したい | `TextData.Get(record.Name)`（文字列キー版。カラム値は「シート名-Enum名」形式） |
| uGUI Text / TextMeshProUGUI に静的テキストを表示したい | `TextSetter` コンポーネント（インスペクタでテキスト選択、コード不要） |
| `TextSetter` 設定済みテキストを実行時に書式化したい | `textSetter.Format(args)` |
| テキスト更新（言語切替・配信取込）に反応したい | `TextData.Instance.OnUpdateContentsAsObservable()` |
| 配信テキストを実行時に追加ロードしたい | `TextData.Instance.AddContents(asset)` |
| Guid からテキストを取得したい | `TextData.Instance.FindText(textGuid)` / `FindTextInfo(textGuid)`。enum から Guid は `FindTextGuid(enum)` |
| 言語識別子からアセットのファイル名を得たい | `TextData.GetAssetFileName(identifier)`（`TextData-{identifier}.asset`。空なら `TextData.asset`） |
| 新しいテキストを追加したい | Excel（内蔵/配信）を編集 → 自動/手動 Generate（後述） |
| エディタから手動で Import / Export / Generate したい | メニュー `Extension/TextData/Open Generate Window`（Excel の Open / Import / Export と Generate のボタン） |
| エディタの言語と生成対象を登録したい | `LanguageManager.Instance.Initialize(LanguageInfo[])`（言語 enum・識別子・テキスト列の番号・スクリプト生成の有無） |
| エディタでテキストを読み込み直したい | `TextDataLoader.Reload()`（選択中の言語のアセットを読み、開いている階層の `TextSetter` へ再適用） |
| エディタでテキスト一覧を検索したい | メニュー `Extension/TextData/Open Selector Window` |
| 空テキストを検出したい | メニュー `Extension/TextData/Open Validation Window`（判定は `TextDataValidator.IsInValid`。継承して `SetValidator` で差し替えられる） |

## 使い方

定型パターン:

- **enum 指定で取得・書式化（最頻出）**: `TextData.Get(TextData.General.Close)` / `TextData.Format(TextData.General.Time_Days, days)`
- **マスターデータの文字列キーで取得（配信テキスト）**: `TextData.Get(skillRecord.Name)`。マスターのカラムには `シート名-Enum名` 形式のキーが入っている
- **起動時初期化**: `SetCryptoKey(key, iv)` → 言語識別子（利用側で定義）を決定 → `LoadEmbedded("TextData/TextData-{identifier}.asset")`（`Resources` からの相対パス。拡張子は内部で除く。`TextDataAsset` を直接渡すオーバーロードもある）
- **配信テキストの追加取込**: `ExternalAsset.LoadAsset<TextDataAsset>` → `TextData.Instance.AddContents(asset)` で内蔵テキストに配信分を追加合成
- **TextSetter（静的テキストはコード不要）**: Text / TextMeshProUGUI と同じ GameObject に `TextSetter` を付け、インスペクタの「select」から選択するだけ。Text 系コンポーネントへの自動付与は基盤の `AdditionalComponent` 機構で行う（登録内容は利用側で定義）

## テキスト追加フロー

原本 Excel と中間データ yaml（`Contents/*.yaml`）は Unity プロジェクト外のワークスペースに配置して運用する。ワークスペースは内蔵（Embedded）と配信（Distribution）の2系統。具体的な配置場所と Excel↔yaml 変換ツールは利用プロジェクト側で定義する。

設定は `TextDataConfig`（`SingletonScriptableObject`）に持つ: 中間データの形式（`FileLoader.Format`。Yaml / Json）、内蔵（`InternalSetting`: アセット出力先・スクリプト出力先・ソース）と配信（`ExternalSetting`: アセット出力先・ソース）、暗号化キー（32文字）と IV（16文字）、変換ツールのパス（Windows / Mac 別）。ソース（`TextDataSource`）は Excel ファイル名・ワークスペースフォルダ（Unity プロジェクトフォルダからの相対パス）・表示名を持ち、中間データはワークスペース直下の `Contents` フォルダから読む。配信は `ExternalSetting` のソースが1つ以上あるときだけ有効（`EnableExternal`）。

変換ツールは `TextDataExcel` が `--workspace <ワークスペースのパス> --mode import` または `--mode export` の引数で起動する（Export = Excel→中間データ、Import = 中間データ→Excel）。

1. **Excel 編集**: シート=カテゴリ（enum 型名）、行=1テキスト（enumName / description / 言語別テキスト列）
2. **Export（Excel→yaml）→ Generate（yaml→アセット+コード）**: Excel 保存すると `TextDataAssetUpdater` が自動検出して実行（約1秒ごとに Excel の更新時刻を前回の更新時刻と比べる。生成するのはエディタで選択中の言語だけ）。手動は `Extension/TextData/Open Generate Window` から
3. **生成物**:
   - 内蔵: `TextDataConfig` で指定した出力先の `TextData/`（`TextData.Instance.AssetFolderLocalPath`）に `TextData-{lang}.asset` + enum スクリプト（スクリプト生成は `LanguageInfo.ScriptGenerate` が有効な言語のみ。どの言語で生成するかは利用側の言語登録で設定）。C# の内訳は `{カテゴリ名}.cs`（nested enum）/ `TextData.category.cs`（`CategoryType` enum + Guid テーブル）/ `TextData.definition.cs`（enum→Guid 解決テーブルと `Get`/`Format` オーバーロード群）
   - 配信: 配信用出力先の `TextData/` に `TextData-{lang}.asset`（**enum は生成されない**。ExternalAsset として配信）
4. テキストは生成時に AES 暗号化されアセットへ格納。実行時は参照時に遅延復号

Generate は中間データのハッシュがアセットに記録済みのものと同じなら何もしない（Generate Window の Generate ボタンは強制的に作り直す）。

カテゴリ（シート）を追加/削除すると enum ファイルも自動生成/自動削除される（スクリプト出力先の直下にある、生成対象でない `.cs` は削除される。出力先に手書きのスクリプトを置かない）。

Guid と ID名 の重複は Export 時に処理される:

- **Guid 重複（行コピー等）**: 後発レコードへ新しい Guid を自動発行する。出力済み yaml でその Guid を保有していたレコードが元の Guid を保持するため、既存の `TextSetter` 参照は壊れない。再発行内容は警告として出力される
- **同一シート内の ID名 重複**: Export がエラー終了し、yaml は更新されない（enum が同名で2つ生成されるのを防ぐ）。Excel 側の ID名 を修正する
- Generate 側にも同じ2種の検証があり、検出時はエラーログを出して中断する（yaml を直接編集した場合の防御）

## 注意点・罠

- **失敗時挙動の非対称に注意**: enum 版 `Format` はテキスト未定義・未ロード時に空文字を返して `Debug.LogError`、文字列キー版 `Format(string, ...)` は null を返す。`Get` は両版とも null を返す（`""` ではない）
- **文字列キーの形式は `シート名-Enum名`**。配信テキストはマスターのカラムにこのキーを設定して運用する（コード直書きしない）
- **初期化順**: `SetCryptoKey` → `LoadEmbedded` → （マスターロード後）`AddContents`。エディタでは `TextDataLoader` が自動処理
- **`LoadEmbedded` は既存辞書を `Clear()` してから取り込む**: `AddContents` は追加合成（同一 Guid は上書き、完了後 `OnUpdateContents` 発火）
- **TextSetter は実行時 Awake でテキストを設定する**（エディタの OnEnable 処理は再生中は動かない。実行時の再適用は `OnUpdateContentsAsObservable` 購読経由のみ）: 非アクティブ状態でコードから `text` を設定すると、初回アクティブ化時の Awake で TextSetter 側の値に上書きされる
- **TextSetter はバッチモード（`Application.isBatchMode`）ではテキストを適用しない**
- **TextSetter 設定済みの Text を直接書き換えない**: テキスト更新イベント（言語切替・配信取込）で TextSetter が再適用し戻される。動的テキストは TextSetter の textGuid を空にしてコードで設定するか、`textSetter.Format()` を使う
- **ダミーテキスト（`#` 付き）はエディタ専用**: textGuid 未設定時のみレイアウト確認用に表示。`OnDisable`/ビルド時に除去されるので実機には出ない
- **`TextData.Instance` のコンストラクタは private**: `Singleton<T>` 経由（`Instance` 初回アクセスで生成）。`CreateInstance()` 呼び出しは不要
- **配信（External）テキストに enum はない**: `ExternalSetting` に scriptFolder 自体がなく生成対象外。必ず文字列キーでアクセス
- **Editor の自動更新**: Excel 保存だけで yaml/アセット/enum まで自動更新される（`TextDataAssetUpdater`、ProjectPrefs `autoUpdate` 既定 true）。Excel が開かれていても更新は走るが、Import ボタンはファイルロック中無効。再生中・バッチモード・コンパイル中・アセット更新中・手動の Import / Export 実行中は自動更新しない。選択中の言語のアセットが無いときは `autoUpdate` の値に関係なく1回生成する。生成してもアセットができなかった場合は、スクリプトの再読み込みまで生成し直さない。配信テキストは `EnableExternal` が true で、かつ `ExternalSetting` のアセット出力先が設定されているときだけ自動更新の対象にする
- **エディタで言語が未選択だと何も動かない**: `LanguageManager.Current` が null のあいだ、`TextDataLoader.Reload()`・自動更新は何もせず、Generate Window は「No language selected.」を表示する。`LanguageManager.Initialize` での言語登録と言語の選択が前提
- **`LoadEmbedded` に渡したアセットが null（パスの誤り等）だと、何もせず戻る**（既存の辞書も消さず、エラーも出ない）
- **Export は Excel の ID 列（Guid）へ書き戻さない**: Guid の正本は yaml 側。Excel の ID 列が空でも、出力済み yaml と ID名 が一致すれば Guid が復元される（未 Import でも Guid が変わらないようにする仕様）。Excel の ID 列へ反映するには Import を実行する
- **変換ツールの出力の扱い**: エラー・警告は標準エラー出力に出る。Unity 側は ExitCode != 0 なら `Debug.LogError`、正常終了時に警告があれば `Debug.LogWarning` を出す
- モジュールの Rx は **R3**（`Observable<Unit>`）。UniRx の `IObservable` ではない点に注意

## 関連

- [Master](../Master/Manual.md) — 配信テキストの文字列キー（`Name`/`Description` カラム）の供給元
- [ExternalAssets](../ExternalAssets/Manual.md) — 配信 TextDataAsset のダウンロード・ロード
- [Localize](../Localize/Manual.md) — エディタの言語選択（`EditorLanguage`）
- [UI](../UI/Manual.md) — テキスト表示コンポーネント全般

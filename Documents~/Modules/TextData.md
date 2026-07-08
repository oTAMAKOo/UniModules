# TextData

> **namespace**: `Modules.TextData`（本体） / `Modules.TextData.Components`（TextSetter・アセット・検証） / `Modules.TextData.Editor`（生成ツール）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TextData/`
> **Client側使用**: 約292ファイル（2026-07時点、全モジュール中最多）
> **依存**: R3（Observable/Subject）/ UniTask（Editor）/ TMPro・uGUI Text / Extensions（Singleton, AesCryptoKey, PathUtility）/ Modules.Devkit（Editor）/ Modules.Localize（Editor: EditorLanguage）

## 概要

ローカライズ対応テキスト管理基盤。Excel を原本とするテキストを AES 暗号化済み `TextDataAsset`（ScriptableObject）としてロードし、生成された enum または文字列キーで実行時に取得する。
**プロジェクト規約: UIテキスト・メッセージをコードに直書きせず、必ず `TextData.Get` / `TextData.Format` を使うこと。**
内蔵（Internal: アプリ同梱、enum アクセス）と配信（External: DL配信、マスターの文字列キーでアクセス）の2系統がある。
主要クラス（実行時）: `TextData`（Singleton・partial。`Get`/`Format`/`LoadEmbedded`/`AddContents`。生成コードと partial で合成）/ `TextDataBase<T>`（辞書管理・暗号キー・検索の基底）/ `TextDataAsset`（テキスト実体）/ `TextType`（Internal / External）/ `TextSetter`（`[ExecuteAlways]`。同一 GameObject の `Text` / `TextMeshProUGUI` に自動適用）。
エディタ専用: `TextDataGenerator`（yaml→アセット+コード生成）/ `GenerateWindow` / `TextDataAssetUpdater`（Excel 監視・自動生成）/ `TextDataLoader`（起動/コンパイル後の再読込）/ `TextDataConfig`（設定アセット: `Client/Assets/Resource (Editor)/Configs/TextDataConfig.asset`）/ `SelectorWindow` / `TextDataValidator` ほか。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| アプリ同梱テキストを取得したい | `TextData.Get(TextData.General.Close)`（カテゴリ enum 指定） |
| プレースホルダ入りテキストを書式化したい | `TextData.Format(TextData.General.Time_Days, days)` |
| マスターデータのテキスト（スキル名・アイテム名等）を取得したい | `TextData.Get(record.Name)`（文字列キー版。カラム値は `"Item-Name_100001"` 形式） |
| uGUI Text / TextMeshProUGUI に静的テキストを表示したい | `TextSetter` コンポーネント（インスペクタでテキスト選択、コード不要） |
| `TextSetter` 設定済みテキストを実行時に書式化したい | `textSetter.Format(args)` |
| テキスト更新（言語切替・配信取込）に反応したい | `TextData.Instance.OnUpdateContentsAsObservable()` |
| 配信テキストを実行時に追加ロードしたい | `TextData.Instance.AddContents(asset)` |
| 新しいテキストを追加したい | `Dominion/TextData/Embedded/Embedded.xlsx`（内蔵）または `Distribution/Distribution.xlsx`（配信）を編集 → 自動/手動 Generate（後述） |
| テキストの enum 定義を確認したい | `Client/Assets/Scripts/Constants/TextData/`（生成物。手編集禁止） |
| エディタでテキスト一覧を検索したい | メニュー `Extension/TextData/Open Selector Window` |
| 空テキストを検出したい | メニュー `Extension/TextData/Open Validation Window` |

## 使い方

定型パターンと参照先:

- **enum 指定で取得・書式化（最頻出）**: `TextData.Get(TextData.General.Close)` / `TextData.Format(TextData.General.Time_Days, days)`（実例: `Client/Assets/Scripts/Client/Core/AdsManager.cs`、`Client/Assets/Scripts/Client/Utility/TimeUtility.cs`）
- **マスターデータの文字列キーで取得（配信テキスト）**: `TextData.Get(skillRecord.Name)`（実例: `Client/Assets/Scripts/Client/Mechanics/Skill/SkillBase.cs`）。マスターのカラムには `シート名-Enum名` 形式のキーが入っている（例: `Master/Masters/Item/ItemMaster/Records/100001.record` の `Name: Item-Name_100001`）
- **起動時初期化（実施済み。新規実装で呼ぶ必要はない）**: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs`（`SetCryptoKey(key, iv)` → `LangageManager.GetLanguageIdentifier()`（"jp"/"en"/"ko"/"zh-TW"/"zh-CN"）→ `LoadEmbedded("TextData/TextData-{identifier}.asset")`）
- **配信テキストの追加取込**: `Client/Assets/Scripts/Client/Module/Download/ContentsUpdateManager.cs`（`ExternalAsset.LoadAsset<TextDataAsset>` → `TextData.Instance.AddContents(asset)` で内蔵テキストに配信分を追加合成）
- **TextSetter（静的テキストはコード不要）**: Text / TextMeshProUGUI と同じ GameObject に `TextSetter` を付け、インスペクタの「select」から選択するだけ。静的な UI ラベルはこの方式が標準（`Editor/TuneComponent/AdditionalComponent.cs` により Text 系コンポーネントへ自動付与設定あり）

## テキスト追加フロー

原本はリポジトリ直下 `TextData/`（Unity プロジェクト外）にある。

```
TextData/
├── Embedded/Embedded.xlsx        … 内蔵テキスト原本（1シート=1カテゴリ）
│   └── Contents/*.yaml           … Excel から Export された中間データ（git 管理対象）
├── Distribution/Distribution.xlsx … 配信テキスト原本（Item/Equipment/ActiveSkill 等マスター用）
│   └── Contents/*.yaml
└── Tools/win/Converter/TextDataConverter.exe … Excel↔yaml 変換ツール
```

1. **Excel 編集**: シート=カテゴリ（enum 型名）、行=1テキスト（enumName / description / 言語別テキスト列 jp, en, ko, zh-TW, zh-CN）
2. **Export（Excel→yaml）→ Generate（yaml→アセット+コード）**: Excel 保存すると `TextDataAssetUpdater` が自動検出して実行。手動は `Extension/TextData/Open Generate Window` から
3. **生成物**:
   - 内蔵: `Client/Assets/Resources/TextData/TextData-{lang}.asset` + `Constants/TextData/*.cs`（enum。スクリプト生成は Japanese 設定時のみ）。C# の内訳は `{カテゴリ名}.cs`（nested enum、19カテゴリ）/ `TextData.category.cs`（`CategoryType` enum + Guid テーブル）/ `TextData.definition.cs`（enum→Guid 解決テーブルと `Get`/`Format` オーバーロード群）
   - 配信: `Client/Assets/Resource (External)/TextData/TextData-{lang}.asset`（**enum は生成されない**。ExternalAsset として S3 配信）
4. **全言語一括生成**: メニュー `Dominion/TextData/Generate All Language`（`Editor/TextData/GenerateAllLanguage.cs`）
5. テキストは生成時に AES 暗号化されアセットへ格納。実行時は参照時に遅延復号

カテゴリ（シート）を追加/削除すると enum ファイルも自動生成/自動削除される。テキスト Guid が重複していると Generate がエラーログを出して中断する。

## 注意点・罠

- **直書き禁止（規約）**: 表示テキストは必ず `TextData.Get` / `TextData.Format`。使う enum が見当たらない場合は勝手に足さずユーザーに確認（enum は生成物のため手編集禁止）
- **失敗時挙動の非対称に注意**: enum 版 `Format` はテキスト未定義・未ロード時に空文字を返して `Debug.LogError`、文字列キー版 `Format(string, ...)` は null を返す。`Get` は両版とも null を返す（`""` ではない）
- **文字列キーの形式は `シート名-Enum名`**: 例 `Item-Name_100001`。配信テキストはマスターのカラムにこのキーを設定して運用する（コード直書きしない）
- **初期化順**: `SetCryptoKey` → `LoadEmbedded` → （マスターロード後）`AddContents`。実行時は `InitializeObject` / `LangageManager.LoadTextData` / `ContentsUpdateManager.LoadExternalTextData` が実施済みなので通常触らない。エディタでは `TextDataLoader` が自動処理
- **`LoadEmbedded` は既存辞書を `Clear()` してから取り込む**: `AddContents` は追加合成（同一 Guid は上書き、完了後 `OnUpdateContents` 発火）
- **TextSetter は Awake / OnEnable でテキストを上書きする**: `SetActive(true)` 直後にコードで `text` を設定する場合は**有効化後に設定**しないと TextSetter に上書きされる（実例コメント: `Client/Assets/Scripts/Client/Scene/Battle/View/BattleUnit/Parts/BattleUnitActionNameView.cs`、`CutInController.cs`）
- **TextSetter 設定済みの Text を直接書き換えない**: テキスト更新イベント（言語切替・配信取込）で TextSetter が再適用し戻される。動的テキストは TextSetter の textGuid を空にしてコードで設定するか、`textSetter.Format()` を使う
- **ダミーテキスト（`#` 付き）はエディタ専用**: textGuid 未設定時のみレイアウト確認用に表示。`OnDisable`/ビルド時に除去されるので実機には出ない
- **`TextData.Instance` のコンストラクタは private**: `Singleton<T>` 経由（`Instance` 初回アクセスで生成）。`CreateInstance()` 呼び出しは不要
- **配信（External）テキストに enum はない**: `ExternalSetting` に scriptFolder 自体がなく生成対象外。必ず文字列キーでアクセス
- **Editor の自動更新**: Excel 保存だけで yaml/アセット/enum まで自動更新される（`TextDataAssetUpdater`、ProjectPrefs `autoUpdate` 既定 true）。Excel が開かれていても更新は走るが、Import ボタンはファイルロック中無効
- モジュールの Rx は **R3**（`Observable<Unit>`）。UniRx の `IObservable` ではない点に注意

## 関連

- [Master](Master.md) — 配信テキストの文字列キー（`Name`/`Description` カラム）の供給元
- [ExternalAsset](ExternalAsset.md) — 配信 TextDataAsset のダウンロード・ロード
- [Localize](Localize.md) — エディタの言語選択（`EditorLanguage`）
- [UI](UI.md) — テキスト表示コンポーネント全般

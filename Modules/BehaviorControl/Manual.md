# BehaviorControl

表のデータで行動を選ぶ AI の機能。
確率・対象・条件・行動を並べた表を上から評価し、最初に成立した行動を実行できる。

## 基本情報

> **namespace**: `Modules.BehaviorControl`（`Editor/` 配下も同じ namespace）
> **場所**: `Modules/BehaviorControl/`（ランタイム7ファイル + `Editor/` 6ファイル）
> **依存**: R3 / Extensions（`Singleton<T>`, `FixedQueue`, `RandomUtility`） / Editor側のみ Newtonsoft.Json・YamlDotNet・Extensions.Devkit・Modules.Devkit

## 概要

データ駆動の行動選択AI（ルールテーブル型）基盤。「確率 → 対象選択 → 条件判定（And/Or） → 行動実行」の行リストを上から評価し、最初に成立した行動を実行する。行動/対象/条件は enum＋コールバック登録で拡張し、データは Excel 由来の Yaml/Json をエディタで `BehaviorControlAsset`（ScriptableObject）に変換して供給する。実行ログのビューワ（`BehaviorControlMonitor`）付き。

主要クラス: `BehaviorController<TArgument, TAction, TTarget, TCondition>`（実行機。enum別コールバック登録 + `Execute`）/ `BehaviorData<TAction, TTarget, TCondition>`（実行用データの abstract 基底。利用側で継承する）/ `ConditionConnecter` enum（None / And / Or）/ `BehaviorControlAsset`（配布データ。タイプ類は文字列のまま保持）/ `BehaviorDataBuilder<TBehaviorData, TAction, TTarget, TCondition>`（アセット→実行データ変換）/ `Parameter`（カンマ区切りパラメータの型変換アクセサ）/ `BehaviorControlLogger`（Singleton。実行ログ `LogData` の蓄積、上限50件。`Logs` / `Clear()` / `OnLogUpdateAsObservable()`）。Editor側に Yaml/Json→アセット変換の `AssetGenerateWindow<TInstance, TBehaviorData, TAction, TTarget, TCondition>`（abstract。読み込みは `FileLoader`、変換は `ImportDataConverter`、入力の形は `ImportData`）/ `BehaviorControlSetting`（インポート元フォルダ・出力先フォルダ・形式（Yaml / Json）の設定）/ `BehaviorControlMonitor`（ログビューワ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ルールテーブル型AIを組みたい | enum3種（行動/対象/条件）定義 → `new BehaviorController<TArg, TAction, TTarget, TCondition>(controllerName, onErrorCallback)` に `RegisterCallback(type, callback)` → `Execute(behaviorName, behaviorData, argument)`（行動を実行したら true） |
| コールバックを書きたい | `bool (ref TArgument argument, Parameter parameter)` の形（`ExecuteAction` / `SelectTarget` / `CheckCondition`）。引数は `ref` で渡り、対象選択の結果を条件・行動へ引き継げる |
| AIデータをアセット化したい | `AssetGenerateWindow` 派生（Editor）の `Open()` → インポート元フォルダを選んで Generate → Yaml/Json から `BehaviorControlAsset` 生成 |
| アセットを実行用データに変換 | `new BehaviorDataBuilder<...>(onErrorCallback).Build(asset)`（enum名の文字列を解決。エラー時 null） |
| 実行ログを見たい | `BehaviorControlMonitor.Open()`（Editorウィンドウ。直近50件） |

## 注意点・罠

- 確率判定は `RandomUtility.RandomInRange(1f, 100f)` の**内部直呼び**で、コントローラごとに乱数源を差し替える口が無い（共有の静的な乱数を使う）。リプレイ整合性が求められる用途では別途ラップまたは代替実装が必要。
- `SuccessRate` は 0〜1 で指定する（範囲外は丸め、内部で100倍して乱数と比べる）。乱数が1以上のため、0.01 未満は当たらない。
- 条件式は「左から逐次評価・false になった時点で打ち切り」。And/Or の優先順位制御は無く、`A And B Or C` のような式は数学的な優先順位通りには評価されない。**途中結果が false になるとその先を評価しないため、Or は左側が true のときにしか評価されず、false を true に戻すことはない**。2個目以降の条件に `Connecter` 未設定（None）だと評価がそこで打ち切られる。
- コールバックが未登録のタイプは false 扱い（`onErrorCallback` に通知）。
- `Execute` のログに残るのは「確率判定と対象選択を通過した行」のみ。確率外れ・対象選択失敗の行は Monitor に出ない（デバッグ時に空ログでも異常とは限らない）。
- 行動コールバックが false を返すと「不成立」として次の行の評価に進む（確率に当たっても行動しないことがある仕様）。
- `BehaviorControlAsset` はタイプ名を文字列で持つため、enum リネームで実行時に Build エラーになる（コンパイルエラーにならない）。
- `BehaviorDataBuilder.Build` は1件でも不明タイプがあると**全体が null**（エラーメッセージ通知）。
- `Parameter.Get<T>(index)` はカンマ区切りの**空/欠落は `ArgumentException` を throw**、型変換失敗は onError 通知して default。
- Editor 変換は Excel 直読みではない。Excel → Yaml/Json 化は別途外部ツール前提（`BehaviorControlSetting` のインポートフォルダに置く運用）。
- インポートするファイルの条件の接続詞は `&`（And）/ `|`（Or）で書く。2個目以降の条件でそれ以外だとインポートエラーになる。
- Generate は、元ファイルの更新日時がアセットの `LastUpdate` より新しいものだけを読み込んで変換する。

## 関連

- [Master](../Master/Manual.md) — マスターデータ基盤
- [Devkit](../Devkit/Manual.md) — `SingletonScriptableObject` / `ProjectPrefs` / `ScriptableObjectGenerator` / `UnityConsole`（Editor機能の基盤）
- [Extensions/Devkit.md](../../Documents~/Extensions/Devkit.md) — `SingletonEditorWindow`（ウィンドウの基底）

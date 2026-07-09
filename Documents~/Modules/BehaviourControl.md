# BehaviourControl

> **namespace**: `Modules.BehaviorControl`（**フォルダ名は `BehaviourControl/`。綴りが異なるので grep 注意**）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/BehaviourControl/`
> **依存**: R3 / Extensions（`Singleton<T>`, `FixedQueue`, `RandomUtility`） / Editor側のみ Newtonsoft.Json・YamlDotNet・Modules.Devkit

## 概要

データ駆動の行動選択AI（ルールテーブル型）基盤。「確率 → 対象選択 → 条件判定（And/Or） → 行動実行」の行リストを上から評価し、最初に成立した行動を実行する。行動/対象/条件は enum＋コールバック登録で拡張し、データは Excel 由来の Yaml/Json をエディタで `BehaviorControlAsset`（ScriptableObject）に変換して供給する。実行ログのビューワ（`BehaviorControlMonitor`）付き。

主要クラス: `BehaviorController<TArgument, TAction, TTarget, TCondition>`（実行機。enum別コールバック登録 + `Execute`）/ `BehaviorData`（実行用データ）/ `BehaviorControlAsset`（配布データ。タイプ類は文字列のまま保持）/ `BehaviorDataBuilder`（アセット→実行データ変換）/ `Parameter`（カンマ区切りパラメータの型変換アクセサ）/ `BehaviorControlLogger`（実行ログ蓄積、上限50件）。Editor側に Yaml/Json→アセット変換の `AssetGenerateWindow` / `BehaviorControlSetting` / `BehaviorControlMonitor`（ログビューワ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ルールテーブル型AIを組みたい | enum3種（行動/対象/条件）定義 → `BehaviorController<TArg, TAction, TTarget, TCondition>` に `RegisterCallback` → `Execute` |
| AIデータをアセット化したい | `AssetGenerateWindow` 派生（Editor）で Yaml/Json → `BehaviorControlAsset` 生成 |
| アセットを実行用データに変換 | `BehaviorDataBuilder<...>.Build(asset)`（enum名の文字列を解決。エラー時 null） |
| 実行ログを見たい | `BehaviorControlMonitor.Open()`（Editorウィンドウ。直近50件） |

## 注意点・罠

- **フォルダ名（BehaviourControl）と namespace（`Modules.BehaviorControl`）の綴りが違う**。using 検索は `BehaviorControl`（米綴り）で行う。
- 確率判定は `RandomUtility.RandomInRange(1f, 100f)` の**内部直呼び**で、シード注入口が無い。リプレイ整合性が求められる用途では別途ラップまたは代替実装が必要。
- 条件式は「左から逐次評価・false になった時点で打ち切り」。And/Or の優先順位制御は無く、`A And B Or C` のような式は数学的な優先順位通りには評価されない。2個目以降の条件に `Connecter` 未設定（None）だと評価がそこで打ち切られる。
- `Execute` のログに残るのは「確率判定と対象選択を通過した行」のみ。確率外れ・対象選択失敗の行は Monitor に出ない（デバッグ時に空ログでも異常とは限らない）。
- 行動コールバックが false を返すと「不成立」として次の行の評価に進む（確率に当たっても行動しないことがある仕様）。
- `BehaviorControlAsset` はタイプ名を文字列で持つため、enum リネームで実行時に Build エラーになる（コンパイルエラーにならない）。
- `BehaviorDataBuilder.Build` は1件でも不明タイプがあると**全体が null**（エラーメッセージ通知）。
- `Parameter.Get<T>(index)` はカンマ区切りの**空/欠落は `ArgumentException` を throw**、型変換失敗は onError 通知して default。
- Editor 変換は Excel 直読みではない。Excel → Yaml/Json 化は別途外部ツール前提（`BehaviorControlSetting` のインポートフォルダに置く運用）。

## 関連

- [Master](Master.md) — マスターデータ基盤
- [Devkit](Devkit.md) — `SingletonEditorWindow` / `SingletonScriptableObject` / `ProjectPrefs`（Editor機能の基盤）

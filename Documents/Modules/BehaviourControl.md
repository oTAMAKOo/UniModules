# BehaviourControl

> **namespace**: `Modules.BehaviorControl`（**フォルダ名は `BehaviourControl/`。綴りが異なるので grep 注意**）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/BehaviourControl/`
> **Client側使用**: 0ファイル（基盤内使用も0、2026-07時点）
> **依存**: R3 / Extensions（`Singleton<T>`, `FixedQueue`, `RandomUtility`） / Editor側のみ Newtonsoft.Json・YamlDotNet・Modules.Devkit

## 概要

データ駆動の行動選択AI（ルールテーブル型）基盤。「確率 → 対象選択 → 条件判定（And/Or） → 行動実行」の行リストを上から評価し、最初に成立した行動を実行する。行動/対象/条件は enum＋コールバック登録で拡張し、データは Excel 由来の Yaml/Json をエディタで `BehaviorControlAsset`（ScriptableObject）に変換して供給する。実行ログのビューワ（`BehaviorControlMonitor`）付き。

**本プロジェクトでは未使用**。戦闘の行動選択AI（Brainシステム、`Client/Assets/Scripts/Client/Battle/Brain/` の `IBrain` / `BrainBase` / 各戦略Brain）は本モジュールを**参照しない独自実装**であり、敵AIを触る際は Brain 側を見ること。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **戦闘の敵・味方AIを実装/修正したい（本プロジェクト）** | 本モジュールではなく `Client/Assets/Scripts/Client/Battle/Brain/` 配下（`BrainBase` 派生 + Brainマスター） |
| （採用時）ルールテーブル型AIを組みたい | enum3種（行動/対象/条件）定義 → `BehaviorController<TArg, TAction, TTarget, TCondition>` に `RegisterCallback` → `Execute` |
| （採用時）AIデータをアセット化したい | `AssetGenerateWindow` 派生（Editor）で Yaml/Json → `BehaviorControlAsset` 生成 |
| （採用時）アセットを実行用データに変換 | `BehaviorDataBuilder<...>.Build(asset)`（enum名の文字列を解決。エラー時 null） |
| （採用時）実行ログを見たい | `BehaviorControlMonitor.Open()`（Editorウィンドウ。直近50件） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `BehaviorController<TArgument, TAction, TTarget, TCondition>` | sealed class | 実行機。`ExecuteAction` / `SelectTarget` / `CheckCondition` デリゲート（いずれも `(ref TArgument, Parameter) → bool`）を enum 別に登録し、`Execute` で行リストを評価 |
| `BehaviorData<TAction, TTarget, TCondition>` | abstract class | 実行用データ。`Behavior`（SuccessRate・行動/対象タイプ＋パラメータ文字列・`Condition[]`）と `ConditionConnecter`（And/Or） |
| `BehaviorControlAsset` | ScriptableObject | 配布データ。タイプ類は**文字列**のまま保持（enum非依存）。`LastUpdate` ハッシュ的更新日時付き |
| `BehaviorDataBuilder<TBehaviorData, ...>` | sealed class | `BehaviorControlAsset` → `BehaviorData` 変換（enum名解決。不明名はエラー通知して null 返却） |
| `Parameter` | sealed class | カンマ区切りパラメータの型変換アクセサ（`Get<T>(index)`。`Convert.ChangeType` ベース） |
| `BehaviorControlLogger` | Singleton（`Extensions.Singleton<T>`） | 実行ログ蓄積（`FixedQueue` 上限50件）＋更新通知 |
| `LogData` | sealed class | 1回の `Execute` のログ（行ごとの確率値・各ノードの成否） |
| `AssetGenerateWindow<TInstance, TBehaviorData, ...>` | abstract SingletonEditorWindow（`Editor/`） | Yaml/Json フォルダを選択して `BehaviorControlAsset` 群を生成 |
| `BehaviorControlSetting` | SingletonScriptableObject（`Editor/`） | インポート元パス・出力先フォルダ・形式（Yaml/Json）設定 |
| `FileLoader` | static class（`Editor/`） | Yaml（YamlDotNet）/ Json（Newtonsoft）のデシリアライズ |
| `ImportData` / `RecordData` / `ImportDataConverter<...>` | class（`Editor/`） | 中間データと enum 解決コンバータ（Builder のエディタ版） |
| `BehaviorControlMonitor` | SingletonEditorWindow（`Editor/`） | 実行ログビューワ（成否アイコン付きで行評価の内訳を表示） |

## 使い方(実例)

Client側・基盤内とも使用例なし。実コードのシグネチャに基づく最小の想定例。

```csharp
// 想定例（実在コードではない）. シグネチャは
// Client/Assets/UniModules/Scripts/Modules/BehaviourControl/BehaviorController.cs 参照.
public enum AiAction { Attack, Heal }
public enum AiTarget { LowestHpEnemy, LowestHpAlly }
public enum AiCondition { HpRateBelow, TurnCount }

public sealed class EnemyBehaviorData : BehaviorData<AiAction, AiTarget, AiCondition> { }

// 構築.
var controller = new BehaviorController<AiContext, AiAction, AiTarget, AiCondition>("EnemyAI", x => Debug.LogError(x));

controller.RegisterCallback(AiAction.Attack, (ref AiContext ctx, Parameter param) => ExecuteAttack(ref ctx, param.Get<int>(0)));
controller.RegisterCallback(AiTarget.LowestHpEnemy, (ref AiContext ctx, Parameter param) => SelectLowestHp(ref ctx));
controller.RegisterCallback(AiCondition.HpRateBelow, (ref AiContext ctx, Parameter param) => ctx.HpRate < param.Get<float>(0));

// アセット→実行データ変換.
var builder = new BehaviorDataBuilder<EnemyBehaviorData, AiAction, AiTarget, AiCondition>(x => Debug.LogError(x));
var behaviorData = builder.Build(behaviorControlAsset);

// 実行（成立した行動があれば true）.
var executed = controller.Execute("Enemy001", behaviorData, context);
```

## API(主要公開メンバー)

### BehaviorController&lt;TArgument, TAction, TTarget, TCondition&gt;

| メンバー | 説明 |
|---|---|
| `BehaviorController(string controllerName, Action<string> onErrorCallback = null)` | 名前はログ・モニタ表示用。エラーはコールバック通知（例外にしない） |
| `RegisterCallback(TAction, ExecuteAction)` / `(TTarget, SelectTarget)` / `(TCondition, CheckCondition)` | enum別コールバック登録（同一キーは上書き） |
| `Execute(string behaviorName, BehaviorData<...> data, TArgument argument) : bool` | 行リストを上から評価し、行動が成立したら true で打ち切り。実行ログを `BehaviorControlLogger` に自動追加 |

### BehaviorDataBuilder / Parameter / BehaviorControlLogger

| メンバー | 説明 |
|---|---|
| `BehaviorDataBuilder.Build(BehaviorControlAsset) : TBehaviorData` | 文字列タイプ→enum 解決。1件でも不明タイプがあると**全体が null**（エラーメッセージ通知） |
| `Parameter.Get<T>(int index)` | カンマ区切りの index 番目を `T` に変換。**空/欠落は `ArgumentException` を throw**、型変換失敗は onError 通知して default |
| `BehaviorControlLogger.Add(LogData)` / `Clear()` / `Logs` | ログ追加（上限50でFIFO）/ 全消去 / 取得 |
| `BehaviorControlLogger.OnLogUpdateAsObservable() : Observable<Unit>` | ログ更新通知（Monitor が Repaint に使用） |

## 注意点・罠

- **フォルダ名（BehaviourControl）と namespace（`Modules.BehaviorControl`）の綴りが違う**。using 検索は `BehaviorControl`（米綴り）で行う。
- **Brainシステムとは別物**。敵AI関連のタスクでこのモジュールを起点にしないこと（Brain は `Dominion.Client.Battle` namespace の独自実装で本モジュール非依存）。
- 確率判定は `RandomUtility.RandomInRange(1f, 100f)` の**内部直呼び**で、シード注入口が無い。戦闘エンジンのリプレイ整合性（`Battle/` 配下の規約）とは相容れないため、現行の戦闘AIには採用不可。
- 条件式は「左から逐次評価・false になった時点で打ち切り」。And/Or の優先順位制御は無く、`A And B Or C` のような式は数学的な優先順位通りには評価されない。2個目以降の条件に `Connecter` 未設定（None）だと評価がそこで打ち切られる。
- `Execute` のログに残るのは「確率判定と対象選択を通過した行」のみ。確率外れ・対象選択失敗の行は Monitor に出ない（デバッグ時に空ログでも異常とは限らない）。
- 行動コールバックが false を返すと「不成立」として次の行の評価に進む（確率に当たっても行動しないことがある仕様）。
- `BehaviorControlAsset` はタイプ名を文字列で持つため、enum リネームで実行時に Build エラーになる（コンパイルエラーにならない）。
- Editor 変換は Excel 直読みではない。Excel → Yaml/Json 化は別途外部ツール前提（`BehaviorControlSetting` のインポートフォルダに置く運用）。

## 関連

- Brain システム（Client側・`Client/Assets/Scripts/Client/Battle/Brain/`）— 本プロジェクトの実際の行動選択AI（ドキュメントは `.claude/notes/character_enemy_master_overview.md` §6 参照）
- [Master](Master.md) — Brain のパラメータ供給元（Brainマスター）
- [Devkit](Devkit.md) — `SingletonEditorWindow` / `SingletonScriptableObject` / `ProjectPrefs`（Editor機能の基盤）

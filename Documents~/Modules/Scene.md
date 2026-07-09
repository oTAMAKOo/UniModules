# Scene

> **namespace**: `Modules.Scene`（計測補助のみ `Modules.Scene.Diagnostics`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Scene/`（SceneManagerBase は partial 6ファイル構成）
> **依存**: UniTask / R3 / Extensions（`Singleton`, `FixedQueue`, `UnityUtility`） / Modules.View（VM連携） / Modules.R3Extension（`ObservableEx.FromUniTask`） / Modules.Devkit.Console

## 概要

Unity シーンの遷移・ロード管理基盤。シーンを enum で識別し、引数オブジェクト（`ISceneArgument`）を渡して遷移する。
ライフサイクル `Initialize（ロード時1回）→ Prepare（通信・読込）→ Enter（表示開始）→ Leave（離脱）` を自動駆動し、ローディング表示・シーンキャッシュ・履歴（戻る遷移）・加算シーン（画面の上に別シーンを重ねる）・事前ロードを提供する。
主要クラス: `SceneManagerBase<TInstance, TScenes>`（遷移・ロード・キャッシュ・履歴・待機・UniqueComponents の本体。非MonoBehaviour の `Extensions.Singleton<T>`）/ `SceneBase<TScenes>`（シーンルートに置く基底）/ `ISceneArgument<TScenes>`（遷移引数。`Identifier` / `PreLoadScenes` / `Cache`）/ `SceneInstance<TScenes>`（ロード済みシーン1個分。`Enable/Disable` でルート GameObject 一括アクティブ制御）/ 補助 interface（`ISceneEvent` / `ITransitionHandler` / `IgnoreControl` / `WaitHandler`）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しい画面（シーン）を追加したい | 下記「新しいシーンを追加する手順」参照 |
| 別シーンへ遷移したい | `sceneManager.Transition(new XxxSceneArgument())` |
| 遷移先にデータを渡したい | `XxxSceneArgument` にフィールドを追加 → シーン側で `Argument.Xxx` 参照 |
| 全シーンを破棄して遷移したい（タイトル戻り等） | `Transition(arg, false, LoadSceneMode.Single)` |
| 現在のシーンを再読み込みしたい | `sceneManager.Reload()` |
| 現在のシーンの上に別シーンを重ねたい | `AppendTransition(sceneArgument)` |
| 重ねたシーンから元のシーンへ戻りたい | `UnloadTransition(戻り先Scenes, sceneInstance または gameObject)` |
| ライフサイクル無しでシーンだけ加算ロードしたい | `Append(identifier or argument, activeOnLoad)` |
| 履歴で1つ前のシーンへ戻りたい | `TransitionBack()`（`Transition(arg, registerHistory: true, mode)` で履歴登録した遷移が対象。履歴登録可否を SceneArgument 側の bool で切り替える慣例は利用側で用意する） |
| 遷移中かを判定したい（多重遷移防止） | `if (sceneManager.IsTransition){ return; }` |
| 遷移の完了・各フェーズをフックしたい | `OnEnterCompleteAsObservable()` / `OnPrepareAsObservable()` 等 |
| 遷移中に非同期処理を待たせたい（サーバー同期等） | `BeginWait()` / `FinishWait(handler)`（`using` 可） |
| シーン側から遷移を拒否したい | シーンに `ITransitionHandler` を実装し `HandleTransition()` で false |
| シーンのロード/アンロード時に処理したい | SceneBase と同一 GameObject に `ISceneEvent` 実装コンポーネント |
| ロード済み他シーンの ViewModel を取得したい | `GetViewModel<TViewModel>(scene)`（[View](View.md) 連携） |
| シーンがロード済みか調べたい / 実体を取りたい | `IsSceneLoaded(id)` / `GetSceneInstance(id)` |
| 次に行くシーンを裏で先読みしたい | `SceneArgument.PreLoadScenes` を override |
| 遷移時のルート一括非アクティブ制御から除外したい | ルート GameObject に `IgnoreControl`（`IgnoreType.ActiveControl`） |
| ローディング演出を出さずに遷移したい | 基盤側にローディング表示制御は無い。利用側で SceneArgument に bool を追加し、`OnPrepareAsObservable()` 等をフックして表示/非表示を切り替える |
| シーン跨ぎで1個だけ存在すべきコンポーネントを管理したい | `SceneManagerBase` 派生で `UniqueComponents`（`protected abstract Dictionary<Type, DuplicatedSettings>`）を override して定義 / 実行時登録は `RegisterUniqueComponent<T>()` |
| 起動シーンを登録したい（Boot 処理） | `RegisterBootScene()` |

## 使い方

### 遷移フロー（`Transition` 呼び出し時の実行順）

```
Transition(argument)  ※ void・fire-and-forget（await 不可）
  → ITransitionHandler.HandleTransition()（ロード済みシーンによる拒否チェック）
  → TransitionStart …… 派生 SceneManager 側で覆いのフェードアウト等を実装
  → 旧シーン: Leave() → Disable()（ルート非アクティブ化）→ 不要シーンのアンロード（キャッシュ・PreLoad 対象は残す）
  → 新シーンロード【未ロード時のみ。ロード直後にルート一括非アクティブ化 → Initialize()。キャッシュ済ならスキップ】
  → SetActiveScene → SetArgument(argument) → 履歴登録 → Prepare()
  → 旧シーンアンロード（Cache=false の場合）→ Resources.UnloadUnusedAssets + GC.Collect
  → TransitionWait（BeginWait ハンドラが全解除されるまで待機）
  → 新シーン Enable()（ルート再アクティブ化）→ OnTransition()
  → TransitionFinish …… 派生 SceneManager 側で覆いのフェードイン等を実装
  → Enter()【同期メソッド】→ PreLoadScenes の事前ロード開始
  ※ 各フェーズ前後で onLeave / onPrepare / onEnter 等の Observable（OnXxxAsObservable）が発火する
```

- `Initialize` はシーンロード時に1回だけ。`Prepare` / `Enter` / `Leave` は遷移の度に毎回呼ばれる。
- 既定は `LoadSceneMode.Additive`（マネージャが自前でアンロード管理）。`Single` は全ロード済み・キャッシュ・加算シーンを破棄。

### 新しいシーンを追加する手順（基盤側 API のみ）

| 手順 | 内容 |
|---|---|
| 1 | `.unity` ファイルを作成し Build Settings 登録。利用側で用意する `Scenes` enum（識別子）とパス辞書を更新（自動生成の仕組みは利用側で用意する） |
| 2 | `XxxSceneArgument : ISceneArgument<TScenes>` を定義（`Identifier` 必須。受け渡しデータはフィールド/プロパティで追加） |
| 3 | `XxxScene : SceneBase<TScenes>` を作り、シーンの**ルート GameObject** にアタッチ（ルート配下に見つからないと `SceneBase class does not exist.` エラーで遷移失敗） |
| 4 | `Initialize` / `Prepare` / `Enter` / `Leave` を必要に応じて override |
| 5 | 呼び出し側で `sceneManager.Transition(new XxxSceneArgument { ... })` |

## 注意点・罠

- **`Transition` 系はすべて void（await 不可）**。内部で fire-and-forget 実行される。完了検知は `OnEnterCompleteAsObservable()` 等。呼び出し前に `if (sceneManager.IsTransition){ return; }` ガードを入れるのが定型（遷移中の呼び出しは例外なく黙って無視されるため、押下連打等で「何も起きない」だけになる）
- **`Initialize` はロード時1回のみ**。`Cache = true` のシーンはキャッシュから再表示された時 `Initialize` が呼ばれない。遷移毎に必要な処理は `Prepare` / `Enter` に書く
- **`Enter` は同期メソッド**。async にできない。非同期の開始処理は `.Forget()` で投げる
- **`SetArgument` は `Prepare` より前**に呼ばれる。`Prepare` 内から `Argument` は参照可能。逆に `Initialize` 時点では未設定の場合がある（キャッシュヒット時を除き Load 直後の Initialize が先行）
- **AppendTransition 中は `Current` が更新されない**（Append 元を指し続ける）。仕様であり、戻り先の決定に利用できる。加算シーンか否かは `SceneInstance.Append` で判定
- **加算シーンからの戻りは `UnloadTransition`**
- **`Append` / `UnloadAppendScene` はライフサイクルを呼ばない**（加算ロード/アンロードのみで `Prepare` / `Enter` / `Leave` は呼ばれない。`Append` でも `SetArgument` は実行される）。ライフサイクル込みで重ねる場合は `AppendTransition` / `UnloadTransition` を使う
- **履歴保持の判定**: bool 引数を省略した派生 `Transition(arg, mode)` は「遷移元シーンの `RegisterHistory`」に従って、遷移元を履歴に残すか自動判定する（実装側の慣例）。bool を明示する基底の3引数版はプロパティを無視する
- **シーンルートに `SceneBase` 派生が必須**。ルート GameObject 配下から `ISceneBase` を検索するため、見つからないと `SceneBase class does not exist.` エラーで遷移が中断する
- **シーンロード直後、アクティブだったルート GameObject は一括非アクティブ化され、Prepare 完了 + TransitionWait 解除後に再アクティブ化される**。ルートオブジェクトの Awake/Start のタイミングに依存する実装をしない。除外したいルート（常駐演出等）には `IgnoreControl` を付ける
- **`BeginWait` の解除漏れで遷移が永久に完了しなくなる**。`FinishWait` を必ず呼ぶか `using (BeginWait())` を使う
- **`Cache = false` にすべきシーン**: 遷移毎に引数・状態が変わるシーン。キャッシュ済みシーンは `FixedQueue`（サイズは利用側で設定）から溢れると自動アンロード
- **`LoadSceneMode.Single` 遷移はロード済み・キャッシュ・加算シーンを全部破棄する**。通常遷移は Additive を使い、Single は起動フローや強制タイトル遷移のみ
- **遷移毎に `Resources.UnloadUnusedAssets()` + `GC.Collect()` が走る**（CleanUp）。遷移を高頻度に連発する設計は避ける
- `ISceneEvent` の `OnLoadScene` 発火対象は「ロード完了時点の currentScene」の SceneBase と**同一 GameObject 上**のコンポーネントのみ（子は対象外。ロードされたシーン自身ではない点にも注意）。`OnUnloadScene` はアンロードされるシーン側に発火する（非対称）
- `SceneManagerBase` は非 MonoBehaviour の `Extensions.Singleton<T>`。`Instance` 初回アクセスで自動生成され、`CreateInstance()` の明示呼び出しは不要
- 現在のシーン・ActiveScene はアンロード不可（`UnloadScene(identifier)` は ArgumentException / 警告ログ）
- 遷移時間は `TimeDiagnostics` により計測され、UnityConsole の "Scene" イベント（緑色）に `prev → next (ms)` 形式で出力される

## 関連

- [View](View.md) — `SceneBase<TScenes>` 派生に `IViewRoot` を実装すると VM を保持できる（VM 型引数付きの拡張基底は利用側で用意する慣例）。`SceneManagerBase.GetViewModel<T>`（SceneManagerBase.View.cs）で他シーンの VM 取得
- [Window](Window.md) — シーン内のポップアップ・ウィンドウ（Open/Close ライフサイクル）。シーンを跨ぐ画面は本モジュール、シーン内に重ねる画面は Window を使う
- [UI](UI.md) — シーン内の View 実装で使う UI 部品
- [ExternalAsset](ExternalAsset.md) — `Prepare` 内でのアセット読込に使用

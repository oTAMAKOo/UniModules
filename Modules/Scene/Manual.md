# Scene

シーンの遷移とロードを管理する機能。
引数を渡した遷移、シーンのキャッシュ、戻る遷移、シーンの重ね表示、先読みができる。

## 基本情報

> **namespace**: `Modules.Scene`（計測補助のみ `Modules.Scene.Diagnostics`）
> **場所**: `Modules/Scene/`（SceneManagerBase は partial 5ファイル構成。`SceneManagerBase.Diagnostics.cs` は計測用の `TimeDiagnostics` クラスのみ）
> **依存**: UniTask / R3（R3.Triggers） / Unity.Linq / Extensions（`Singleton`, `FixedQueue`, `UnityUtility`） / Modules.View（VM連携） / Modules.R3Extension（`ObservableEx.FromUniTask`） / Modules.Devkit.Console

## 概要

Unity シーンの遷移・ロード管理基盤。シーンを enum で識別し、引数オブジェクト（`ISceneArgument`）を渡して遷移する。
ライフサイクル `Initialize（ロード時1回）→ Prepare（通信・読込）→ Enter（表示開始）→ Leave（離脱）` を自動駆動し、ローディング表示・シーンキャッシュ・履歴（戻る遷移）・加算シーン（画面の上に別シーンを重ねる）・事前ロードを提供する。
主要クラス: `SceneManagerBase<TInstance, TScenes>`（遷移・ロード・キャッシュ・履歴・待機・UniqueComponents の本体。非MonoBehaviour の `Extensions.Singleton<T>`）/ `SceneBase<TScenes>`（シーンルートに置く基底）/ `ISceneArgument<TScenes>`（遷移引数。`Identifier` / `PreLoadScenes` / `Cache`）/ `SceneInstance<TScenes>`（ロード済みシーン1個分。`Enable/Disable` でルート GameObject 一括アクティブ制御）/ 補助型（interface: `ISceneEvent` / `ITransitionHandler`、class: `IgnoreControl`（MonoBehaviour）/ `WaitHandler`（IDisposable））。

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
| ライフサイクル無しでシーンだけ加算ロードしたい | `Append(identifier or argument, activeOnLoad).Subscribe()`（戻り値は `Observable<SceneInstance<TScenes>>`。購読した時点でロードが始まる） |
| `Append` で重ねたシーンをアンロードしたい | `UnloadAppendScene(sceneInstance または ISceneBase, deactivateSceneObjects)` |
| GameObject が属する加算シーンを調べたい | `FindAppendSceneInstance(gameObject)` / 一覧は `AppendSceneInstances` |
| 履歴で1つ前のシーンへ戻りたい | `TransitionBack()`（`Transition(arg, registerHistory: true, mode)` で遷移元を履歴に残した遷移が対象。履歴が現在のシーンだけなら何もしない） |
| 履歴を消したい / 履歴を見たい | `ClearTransitionHistory()`（現在のシーンだけ残す） / `GetArgumentHistory()` |
| 遷移中かを判定したい（多重遷移防止） | `if (sceneManager.IsTransition){ return; }`（遷移先は `TransitionTarget`） |
| 進行中の遷移を中止したい | `TransitionCancel()`（`OnCancelAsObservable()` が発火する） |
| 遷移の完了・各フェーズをフックしたい | `OnLeaveAsObservable()` / `OnLeaveCompleteAsObservable()` / `OnPrepareAsObservable()` / `OnPrepareCompleteAsObservable()` / `OnEnterAsObservable()` / `OnEnterCompleteAsObservable()`（引数は対象の `SceneInstance`） |
| シーンのロード・アンロードをフックしたい | `OnLoadSceneAsObservable()` / `OnLoadSceneCompleteAsObservable()` / `OnLoadErrorAsObservable()` / `OnUnloadSceneAsObservable()` / `OnUnloadSceneCompleteAsObservable()` / `OnUnloadErrorAsObservable()` |
| 加算シーンから戻ったことをフックしたい | `OnUnloadTransitionAsObservable()`（引数は戻り先のシーン） |
| 加算遷移の成立をフックしたい | `OnBeforeAppendTransitionAsObservable()` → `OnAppendTransitionAsObservable()` の順で発火（引数は加算されたシーン。前者は「加算遷移通知の購読処理より先に実行したい処理」用で、シーンロード完了後＝遷移成立が確定した時点で発火する） |
| 遷移中に非同期処理を待たせたい（サーバー同期等） | `BeginWait()` / `FinishWait(handler)`（`using` 可）。全待機の強制解除は `CancelAllTransitionWait()` |
| シーン側から遷移を拒否したい | `SceneBase` 派生に `ITransitionHandler` を実装し `HandleTransition()` で false（対象はロード済みで有効状態のシーン。チェックが走るのは Additive の `Transition` 系と `AppendTransition` / `UnloadTransition`。Single 遷移では呼ばれない） |
| シーンのロード/アンロード時に処理したい | SceneBase と同一 GameObject に `ISceneEvent` 実装コンポーネント |
| ロード済み他シーンの ViewModel を取得したい | `GetViewModel<TViewModel>(scene)` / シーンを指定しない `GetViewModel<TViewModel>()`（[View](../View/Manual.md) 連携） |
| シーンがロード済みか調べたい / 実体を取りたい | `IsSceneLoaded(id)` / `GetSceneInstance(id)` / 一覧は `LoadedScenesInstances` |
| シーンを指定してアンロードしたい | `UnloadScene(id)` / キャッシュ済みを全部なら `await UnloadAllCacheScene()` / キャッシュの有無は `HasCache(id)` |
| 起動シーンか・戻る遷移で来たかを知りたい | `SceneBase.IsLaunchScene` / `SceneBase.IsSceneBack` |
| 次に行くシーンを裏で先読みしたい | `ISceneArgument.PreLoadScenes` で先読みするシーンを返す |
| 遷移時のルート一括非アクティブ制御から除外したい | ルート GameObject に `IgnoreControl`（`IgnoreType.ActiveControl`） |
| ローディング演出を出さずに遷移したい | 基盤側にローディング表示制御は無い。利用側で SceneArgument に bool を追加し、`OnPrepareAsObservable()` 等をフックして表示/非表示を切り替える |
| シーン跨ぎで1個だけ存在すべきコンポーネントを管理したい | `SceneManagerBase` 派生で `UniqueComponents`（`protected abstract Dictionary<Type, DuplicatedSettings>`）を override して定義（重複時の扱いは `DuplicatedAction`: `DisableComponent` / `DisableGameObject` / `DestroyGameObject`。既定は `DestroyGameObject`） / 実行時登録は `RegisterUniqueComponent<T>(target)` |
| 起動シーンを登録したい（Boot 処理） | `await RegisterBootScene()` |

## 使い方

### 遷移フロー（`Transition` 呼び出し時の実行順）

```
Transition(argument)  ※ void・fire-and-forget（await 不可）
  → ITransitionHandler.HandleTransition()（ロード済みシーンによる拒否チェック。Additive 遷移時のみ）
  → 進行中の事前ロードをキャンセル → registerHistory が false なら遷移元を履歴から外す
  → TransitionStart …… 派生 SceneManager 側で覆いのフェードアウト等を実装
  → 旧シーン: Leave() → Disable()（ルート非アクティブ化）→ 不要シーンのアンロード（遷移元・遷移先・キャッシュ・PreLoad 対象は残す）
  → 新シーンロード【未ロード時のみ。ロード直後にルート一括非アクティブ化 → ISceneEvent.OnLoadScene() → Initialize()。ロード済みならスキップ】
  → SetActiveScene → SetArgument(argument) → Current を新シーンに更新 → 履歴登録 → 1フレーム待機 → Prepare()
  → 旧シーンアンロード（Cache=false の場合）→ Resources.UnloadUnusedAssets + GC.Collect
  → TransitionWait（BeginWait ハンドラが全解除されるまで待機）
  → 新シーン Enable()（ルート再アクティブ化）→ 1フレーム待機 → OnTransition()
  → TransitionFinish …… 派生 SceneManager 側で覆いのフェードイン等を実装
  → Enter()【同期メソッド】→ PreLoadScenes の事前ロード開始
  ※ Leave / Prepare / Enter の直前に OnXxxAsObservable、直後に OnXxxCompleteAsObservable が発火する
```

- `Initialize` はシーンロード時に1回だけ。`Prepare` / `Enter` / `Leave` は遷移の度に毎回呼ばれる。
- 既定は `LoadSceneMode.Additive`（マネージャが自前でアンロード管理）。`Single` は全ロード済み・キャッシュ・加算シーンを破棄（加算シーンにも `Leave` が呼ばれる）。

### 加算遷移フロー（`AppendTransition` / `UnloadTransition`）

```
AppendTransition(argument)
  → ITransitionHandler.HandleTransition() → TransitionStart
  → 有効状態のロード済みシーンをすべて Disable()（遷移元の Leave は呼ばれない）
  → 加算シーンをロード（Initialize()）→ SetArgument(argument) → Enable() → SetActiveScene
  → OnBeforeAppendTransitionAsObservable → OnAppendTransitionAsObservable
  → Prepare() → TransitionFinish → Enter()

UnloadTransition(戻り先, 加算シーン)
  → ITransitionHandler.HandleTransition() → TransitionStart（引数は戻り先シーンの引数）
  → 加算シーン: Leave() → Disable()
  → 戻り先: Enable() → SetActiveScene → 加算シーンのアンロード開始（完了は待たない）
  → OnUnloadTransitionAsObservable → 戻り先 OnTransition() → TransitionFinish（引数は null）→ 戻り先 Enter()
```

- 加算遷移では `TransitionWait`（`BeginWait` の待機）と、遷移元の `Leave` / 戻り先の `Prepare` は実行されない。
- `UnloadTransition` で戻り先に `Enter()` は呼ばれるが、`OnEnterAsObservable()` / `OnEnterCompleteAsObservable()` は発火しない。

### 派生クラスで実装するメンバー

| クラス | メンバー |
|---|---|
| `SceneManagerBase` 派生 | `ScenePaths`（識別子 → シーンパス） / `UniqueComponents` / `TransitionStart(argument, isSceneBack)` / `TransitionFinish(argument, isSceneBack)`。任意で `CacheSize`（既定 3） / `OnRegisterCurrentScene(sceneInstance)` |
| `SceneBase` 派生 | `GetArgumentType()` / `SetArgument(argument)` / `GetArgument()`。任意で `Initialize` / `Prepare` / `Enter` / `Leave` / `OnTransition` |

### 新しいシーンを追加する手順（基盤側 API のみ）

| 手順 | 内容 |
|---|---|
| 1 | `.unity` ファイルを作成し Build Settings 登録。利用側で用意する `Scenes` enum（識別子）とパス辞書を更新（自動生成の仕組みは利用側で用意する） |
| 2 | `XxxSceneArgument : ISceneArgument<TScenes>` を定義（`Identifier` 必須。受け渡しデータはフィールド/プロパティで追加） |
| 3 | `XxxScene : SceneBase<TScenes>` を作り、シーンの**ルート GameObject** にアタッチ（ルート配下に見つからないと `SceneBase class does not exist.` エラーで遷移失敗）。`GetArgumentType` / `SetArgument` / `GetArgument` は abstract のため実装が必要 |
| 4 | `Initialize` / `Prepare` / `Enter` / `Leave` / `OnTransition` を必要に応じて override |
| 5 | 呼び出し側で `sceneManager.Transition(new XxxSceneArgument { ... })` |

## 注意点・罠

- **`Transition` 系はすべて void（await 不可）**。内部で fire-and-forget 実行される。完了検知は `OnEnterCompleteAsObservable()` 等。呼び出し前に `if (sceneManager.IsTransition){ return; }` ガードを入れるのが定型（`Transition` / `Reload` / `AppendTransition` / `UnloadTransition` は遷移中の呼び出しが無視され、UnityConsole の "Scene" イベントに Warning ログが出る。押下連打等では「何も起きない」だけになる。`ForceAppendTransition` / `ForceUnloadTransition` は進行中の遷移をキャンセルしてから実行する）
- **`TransitionBack` は遷移中（`TransitionTarget` が設定されている間）の呼び出しをログ無しで無視する**。戻り先が履歴に無い場合も何もしない
- **`Initialize` はロード時1回のみ**。`Cache = true` のシーンはキャッシュから再表示された時 `Initialize` が呼ばれない。遷移毎に必要な処理は `Prepare` / `Enter` に書く
- **`Enter` は同期メソッド**。async にできない。非同期の開始処理は `.Forget()` で投げる
- **`SetArgument` は `Prepare` より前**に呼ばれる。`Prepare` 内から引数は参照可能。逆に遷移でロードされたシーンの `Initialize` 時点では未設定（ロード直後の `Initialize` が `SetArgument` より先行する）。`RegisterBootScene` で登録する起動シーンだけは `SetArgument` → `Initialize` の順になる
- **`RegisterBootScene` は引数型（`GetArgumentType()`）を引数なしコンストラクタで生成する**。起動シーンになり得るシーンの引数クラスには引数なしコンストラクタが必要
- **`ISceneArgument.PreLoadScenes` は、先読みが無ければ null か空配列を返す**（どちらも「先読み無し」として扱う）
- **AppendTransition 中は `Current` が更新されない**（Append 元を指し続ける）。仕様であり、戻り先の決定に利用できる。加算シーンか否かは `SceneInstance.Append` で判定
- **加算シーンからの戻りは `UnloadTransition`**
- **`Append` / `UnloadAppendScene` はライフサイクルを呼ばない**（加算ロード/アンロードのみで `Prepare` / `Enter` / `Leave` は呼ばれない。未ロードなら `Initialize` は呼ばれる。`SetArgument` が実行されるのは引数オブジェクトを渡すオーバーロードだけで、識別子だけを渡す `Append(identifier)` では実行されない）。ライフサイクル込みで重ねる場合は `AppendTransition` / `UnloadTransition` を使う
- **`Append` は購読しないと実行されない**。戻り値の Observable を `Subscribe()` するか `ToUniTask` で待つ
- **`UnloadTransition` 中の `TransitionFinish` には引数 null が渡される**。派生 SceneManager の `TransitionFinish` は null を許容する実装にする（戻り先がロード済みでない場合は `TransitionStart` にも null が渡る）
- **履歴保持の判定**: `Transition(arg, registerHistory, mode)` の `registerHistory` は「遷移元のシーンを履歴に残すか」を表す。既定の false では遷移元が履歴から外れ、`TransitionBack` で戻れない。`Reload` / `TransitionBack` による遷移も遷移元を履歴に残さない
- **シーンルートに `SceneBase` 派生が必須**。ルート GameObject 配下から `ISceneBase` を検索するため、見つからないと `SceneBase class does not exist.` エラーで遷移が中断する
- **シーンロード直後、アクティブだったルート GameObject は一括非アクティブ化され、Prepare 完了 + TransitionWait 解除後に再アクティブ化される**。ルートオブジェクトの Awake/Start のタイミングに依存する実装をしない。除外したいルート（常駐演出等）には `IgnoreControl` を付け、コードから `Type` に `IgnoreType.ActiveControl` を設定する（`Type` は SerializeField ではないためアタッチのみでは除外されない）
- **`BeginWait` の解除漏れで遷移が永久に完了しなくなる**。`FinishWait` を必ず呼ぶか `using (BeginWait())` を使う
- **`Cache = false` にすべきシーン**: 遷移毎に引数・状態が変わるシーン。キャッシュ済みシーンは `FixedQueue`（サイズは `CacheSize`。既定 3、派生で override 可）から溢れると自動アンロード（溢れたのが現在のシーンなら、アンロードせずキューへ入れ直す）
- **`LoadSceneMode.Single` 遷移はロード済み・キャッシュ・加算シーンを全部破棄する**。通常遷移は Additive を使い、Single は起動フローや強制タイトル遷移のみ
- **遷移毎に `Resources.UnloadUnusedAssets()` + `GC.Collect()` が走る**（CleanUp。シーンを1つアンロードする度にも走る）。遷移を高頻度に連発する設計は避ける
- `ISceneEvent` の `OnLoadScene` 発火対象は「ロード完了時点の currentScene」の SceneBase と**同一 GameObject 上**のコンポーネントのみ（子は対象外。ロードされたシーン自身ではない点にも注意。`RegisterBootScene` では起動シーン自身に発火する）。`OnUnloadScene` はアンロードされるシーン側に発火する（非対称）
- `SceneManagerBase` は非 MonoBehaviour の `Extensions.Singleton<T>`。`Instance` 初回アクセスで自動生成され、`CreateInstance()` の明示呼び出しは不要
- 現在のシーン・ActiveScene はアンロード不可（`UnloadScene(identifier)` は ArgumentException / 警告ログ）
- 遷移時間は `TimeDiagnostics` により計測され、UnityConsole の "Scene" イベント（緑色）に `prev → next (ms)` 形式で出力される

## 関連

- [View](../View/Manual.md) — `SceneBase<TScenes>` 派生に `IViewRoot` を実装すると VM を保持できる（VM 型引数付きの拡張基底は利用側で用意する慣例）。`SceneManagerBase.GetViewModel<T>`（SceneManagerBase.View.cs）で他シーンの VM 取得
- [Window](../Window/Manual.md) — シーン内のポップアップ・ウィンドウ（Open/Close ライフサイクル）。シーンを跨ぐ画面は本モジュール、シーン内に重ねる画面は Window を使う
- [UI](../UI/Manual.md) — シーン内の View 実装で使う UI 部品
- [ExternalAssets](../ExternalAssets/Manual.md) — `Prepare` 内でのアセット読込に使用

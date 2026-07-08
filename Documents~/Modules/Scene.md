# Scene

> **namespace**: `Modules.Scene`（計測補助のみ `Modules.Scene.Diagnostics`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Scene/`（SceneManagerBase は partial 6ファイル構成）
> **Client側使用**: 5ファイル（2026-07時点）※ 実際の入口は Client 側ラッパー `SceneManager` / `SceneBase`（`Dominion.Client` namespace のため using 不要。全画面遷移がこのモジュール経由で、使用箇所は実質多数）
> **依存**: UniTask / R3（UniRxではない） / Extensions（`Singleton`, `FixedQueue`, `UnityUtility`） / Modules.View（VM連携） / Modules.R3Extension（`ObservableEx.FromUniTask`） / Modules.Devkit.Console

## 概要

Unity シーンの遷移・ロード管理基盤。シーンを enum（`Constants.Scenes`）で識別し、引数オブジェクト（`ISceneArgument`）を渡して遷移する。
ライフサイクル `Initialize（ロード時1回）→ Prepare（通信・読込）→ Enter（表示開始）→ Leave（離脱）` を自動駆動し、ローディング表示・シーンキャッシュ・履歴（戻る遷移）・加算シーン（画面の上に別シーンを重ねる）・事前ロードを提供する。
Client 側の入口は `Dominion.Client.SceneManager`（Singleton）と `Dominion.Client.SceneBase<TArgument, TViewModel>`。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しい画面（シーン）を追加したい | 下記「新しいシーンを追加する手順」参照 |
| 別シーンへ遷移したい | `SceneManager.Instance.Transition(new XxxSceneArgument())` |
| 遷移先にデータを渡したい | `XxxSceneArgument` にフィールドを追加 → シーン側で `Argument.Xxx` 参照 |
| 全シーンを破棄して遷移したい（タイトル戻り等） | `Transition(arg, false, LoadSceneMode.Single)` |
| 現在のシーンを再読み込みしたい | `SceneManager.Instance.Reload()` |
| 現在のシーンの上に別シーンを重ねたい（編成・戦闘等） | `AppendTransition(sceneArgument)` |
| 重ねたシーンから元のシーンへ戻りたい | `UnloadTransition(戻り先Scenes, sceneInstance または gameObject)` |
| ライフサイクル無しでシーンだけ加算ロードしたい | `Append(identifier or argument, activeOnLoad)` |
| 履歴で1つ前のシーンへ戻りたい | `TransitionBack()`（要 `Transition(arg, registerHistory: true)`。Client 未使用） |
| 遷移中かを判定したい（多重遷移防止） | `if (sceneManager.IsTransition){ return; }` |
| 遷移の完了・各フェーズをフックしたい | `OnEnterCompleteAsObservable()` / `OnPrepareAsObservable()` 等 |
| 遷移中に非同期処理を待たせたい（サーバー同期等） | `BeginWait()` / `FinishWait(handler)`（`using` 可） |
| シーン側から遷移を拒否したい | シーンに `ITransitionHandler` を実装し `HandleTransition()` で false |
| シーンのロード/アンロード時に処理したい | SceneBase と同一 GameObject に `ISceneEvent` 実装コンポーネント |
| ロード済み他シーンの ViewModel を取得したい | `GetViewModel<TViewModel>(scene)`（[View](View.md) 連携） |
| シーンがロード済みか調べたい / 実体を取りたい | `IsSceneLoaded(id)` / `GetSceneInstance(id)` |
| 次に行くシーンを裏で先読みしたい | `SceneArgument.PreLoadScenes` を override |
| 遷移時のルート一括非アクティブ制御から除外したい | ルート GameObject に `IgnoreControl`（`IgnoreType.ActiveControl`） |
| ローディング演出を出さずに遷移したい | `SceneArgument.loadingAnimation = false` |
| シーン跨ぎで1個だけ存在すべきコンポーネントを管理したい | Client `SceneManager.UniqueComponentsSettings` に登録 / `RegisterUniqueComponent()` |
| 起動シーンを登録したい（Boot 処理） | `RegisterBootScene()`（`ApplicationInitializer` が呼出済み） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `SceneManagerBase<TInstance, TScenes>` | abstract class（`Extensions.Singleton<T>` 継承・非MonoBehaviour・partial） | 遷移・ロード・キャッシュ・履歴・待機・UniqueComponents の本体。`ScenePaths` / `UniqueComponents` / `TransitionStart` / `TransitionFinish` が abstract |
| `SceneBase<TScenes>`（基盤） | abstract MonoBehaviour（`ISceneBase<TScenes>` 実装） | シーンルートに置く基底。`Initialize/Prepare/Enter/Leave/OnTransition` virtual |
| `ISceneArgument<TScenes>` | interface | 遷移引数。`Identifier` / `PreLoadScenes` / `Cache` |
| `SceneInstance<TScenes>` | sealed class | ロード済みシーン1個分の情報。`Enable/Disable` でルート GameObject 一括アクティブ制御 |
| `ISceneEvent` | interface | `OnLoadScene()` / `OnUnloadScene()` 通知を受ける |
| `ITransitionHandler` | interface | ロード済みシーンが遷移可否を返す（false で遷移中止） |
| `IgnoreControl` | sealed MonoBehaviour | SceneInstance の Enable/Disable 制御から除外するマーカー |
| `WaitHandler` | sealed class（IDisposable） | `BeginWait()` の戻り値。Dispose で待機解除 |
| `TimeDiagnostics` | sealed class（`Modules.Scene.Diagnostics`） | Load/Prepare/Leave/Total の時間計測（UnityConsole "Scene" イベントに出力） |

### Client側ラッパー（実際に触るのはこちら）

| クラス | 場所 | 役割 |
|---|---|---|
| `SceneManager` | `Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs` | `SceneManagerBase<SceneManager, Scenes>` 実装。CacheSize=5。TransitionStart/Finish で `LoadingScreen` のフェード表示。UniqueComponents 定義（EventSystem, PopupManager 等） |
| `SceneArgument` | `Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs` | 引数基底。`Cache`（既定 true）/ `PreLoadScenes` / `loadingAnimation` / `bgm` / `bgmId` |
| `SceneBase<TArgument, TViewModel>` | 同上 | シーン基底。`IViewRoot` 実装・`viewModel` 保持（[View](View.md) 参照）。`Argument` プロパティ、SetArgument 時に BGM 自動再生 |
| `Scenes` enum / `SceneDefinition.ScenePaths` | `Client/Assets/Scripts/Constants/Scenes.cs` | **自動生成ファイル**（`ScenesScriptGenerator`）。手動編集禁止 |

## 使い方(実例)

### 遷移フロー全体（`Transition` 呼び出し時の実行順）

```
Transition(argument)  ※ void・fire-and-forget（await 不可）
  → ITransitionHandler.HandleTransition()（ロード済みシーンによる拒否チェック）
  → TransitionStart …… Client実装: LoadingScreen.FadeOut()＝画面を覆う（loadingAnimation でアイコン有無）
  → 旧シーン: onLeave → Leave() → PlayerPrefs.Save() → onLeaveComplete → Disable()（ルート非アクティブ化）
  → 不要シーンのアンロード（キャッシュ・PreLoad 対象は残す）
  → 新シーンロード【未ロード時のみ。ロード直後にルート一括非アクティブ化 → Initialize()。キャッシュ済ならスキップ】
  → SetActiveScene → SetArgument(argument)【Client実装: Argument 設定 + BGM 再生】→ 履歴登録 → 1フレーム待ち
  → onPrepare → Prepare() → onPrepareComplete
  → 旧シーンアンロード（Cache=false の場合）→ Resources.UnloadUnusedAssets + GC.Collect
  → TransitionWait（BeginWait ハンドラが全解除されるまで待機）
  → 新シーン Enable()（ルート再アクティブ化）→ OnTransition()
  → TransitionFinish …… Client実装: LoadingScreen.FadeIn()＝ローディング明け
  → onEnter → Enter()【同期メソッド】→ onEnterComplete
  → PreLoadScenes の事前ロード開始
```

- `Initialize` はシーンロード時に1回だけ。`Prepare` / `Enter` / `Leave` は遷移の度に毎回呼ばれる。
- 既定は `LoadSceneMode.Additive`（マネージャが自前でアンロード管理）。`Single` は全ロード済み・キャッシュ・加算シーンを破棄（Boot→Title/Home、強制タイトル遷移で使用）。

### 新しいシーンを追加する手順

| 手順 | 内容 |
|---|---|
| 1 | `.unity` ファイルを `Assets/Scenes/` に作成。Build Settings 登録で `Constants/Scenes.cs`（enum + パス辞書）が自動再生成される（`SceneAssetPostprocessor`）。手動再生成は Unity メニュー `Extension/Generators/Scripts/ - Scenes.cs`。**Scenes.cs を手で編集しない** |
| 2 | `XxxSceneArgument : SceneArgument` を定義（`Identifier` 必須 override。受け渡しデータはフィールド/プロパティで追加） |
| 3 | `XxxViewModel : ViewModel` を単独ファイルで定義（[View](View.md) の実例4参照） |
| 4 | `XxxScene : SceneBase<XxxSceneArgument, XxxViewModel>` を作り、シーンの**ルート GameObject**にアタッチ（ルート配下に見つからないと `SceneBase class does not exist.` エラーで遷移失敗） |
| 5 | `Initialize` / `Prepare` / `Enter` / `Leave` を必要に応じて override |
| 6 | 呼び出し側で `SceneManager.Instance.Transition(new XxxSceneArgument { ... })` |

最小のシーン実装（引数 + シーン本体が1ファイルに同居するのが慣例）:

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Menu/MenuScene.cs（抜粋）
public sealed class MenuSceneArgument : SceneArgument
{
    public override Scenes? Identifier { get { return Scenes.Menu; } }

    public override bool Cache { get { return true; } }

    public override Scenes[] PreLoadScenes
    {
        get { return new Scenes[] { }; }
    }
}

public sealed class MenuScene : SceneBase<MenuSceneArgument, MenuViewModel>
{
    [SerializeField]
    private MenuView menuView = null;

    public override UniTask Initialize()
    {
        return UniTask.CompletedTask;
    }

    public override async UniTask Prepare()
    {
        var tasks = new List<UniTask>()
        {
            menuView.Prepare(),
        };

        await UniTask.WhenAll(tasks);
    }

    public override void Enter()
    {
        menuView.Enter();
    }

    public override UniTask Leave()
    {
        return UniTask.CompletedTask;
    }
}
```

### 実例1: 基本の遷移（引数フィールドでデータを渡す）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Title/TitleViewModel.cs（抜粋）
var sceneArgument = new HomeSceneArgument
{
    TransitionFromTitle = true,
};

SceneManager.Instance.Transition(sceneArgument);
```

呼び出し前の多重遷移ガード + Single モード指定（起動フロー）:

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Boot/View/BootView.cs（抜粋）
var sceneManager = SceneManager.Instance;

if (sceneManager.IsTransition) { return; }

var titleSceneArgument = new TitleSceneArgument()
{
    initial = true,
};

sceneManager.Transition(titleSceneArgument, mode:LoadSceneMode.Single);
```

### 実例2: 加算シーン遷移（AppendTransition）と戻り（UnloadTransition）

現在のシーンをロードしたまま非アクティブ化し、上に別シーンを重ねるパターン（WorldMap → 編成、WorldMap → 戦闘等）。戻りは `TransitionBack` ではなく `UnloadTransition`。

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs（抜粋）
// 編成シーンへ遷移.
var sceneArgument = new PartySceneArgument()
{
    UserCitadelId = Content.UserCitadelId,
    PartySlot = viewModel.CurrentPartyTabIndex,
    ReturnScene = Constants.Scenes.WorldMap,    // 戻り先は引数に自前で持たせる慣例.
};

sceneManager.AppendTransition(sceneArgument);
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Party/PartyView.cs（抜粋）
private void ExitPartyScene()
{
    var returnScene = ViewModel.ReturnScene;

    if (!returnScene.HasValue){ return; }

    var sceneManager = SceneManager.Instance;

    // 自シーンの gameObject から SceneInstance を逆引きしてアンロード遷移.
    sceneManager.UnloadTransition(returnScene.Value, gameObject);
}
```

### 実例3: Append 起動か通常起動かで戻り方を分岐

`SceneInstance.Append` フラグと「AppendTransition 中は `Current` が更新されない」性質を利用する。

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/Window/BattleMenuWindow/BattleMenuWindow.cs（抜粋）
var sceneInstance = sceneManager.GetSceneInstance(Scenes.Battle);

// AppendTransition で開かれていた場合は加算シーンアンロード遷移で戻る.
if (sceneInstance != null && sceneInstance.Append)
{
    sceneManager.UnloadTransition(GetReturnScene(sceneInstance), sceneInstance);
}
// 通常遷移で開かれていた場合はホームへ遷移.
else
{
    sceneManager.Transition(new HomeSceneArgument());
}
```

### 実例4: 遷移中に外部処理を待たせる（BeginWait）

Prepare 完了〜シーン表示の間に非同期処理（サーバー同期等）を割り込ませる。解除しないと遷移が完了しないので必ず `FinishWait` か `using`。

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/SystemModel.cs（抜粋）
async UniTask OnScenePrepare()
{
    var requireServerSync = RequireServerSync();

    if (!requireServerSync){ return; }

    var waitHandler = sceneManager.BeginWait();

    await GetServerData();

    sceneManager.FinishWait(waitHandler);
}

sceneManager.OnPrepareAsObservable()
    .Subscribe(_ => OnScenePrepare().Forget())
    .AddTo(Disposable);
```

`using (BeginWait()) { await ...; }` 形式も可（`Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs` の PlayBgm 参照）。

### 実例5: シーンイベント購読（他システムからのフック）

```csharp
// 引用元: Client/Assets/Scripts/Client/Tutorial/TutorialControllerBase.cs（抜粋）
void SceneEnter(SceneInstance<Scenes> sceneInstance)
{
    if (sceneInstance.GetScene() != gameObject.scene){ return; }

    sceneEnter = true;
}

sceneManager.OnEnterCompleteAsObservable()
    .Where(_ => UnityUtility.IsActiveInHierarchy(gameObject))
    .Subscribe(x => SceneEnter(x))
    .AddTo(this);
```

## API(主要公開メンバー)

### SceneManagerBase&lt;TInstance, TScenes&gt;（Client では `SceneManager.Instance`）

遷移系（すべて void・fire-and-forget。完了は Observable で検知）:

| メンバー | 説明 |
|---|---|
| `Transition<TArgument>(argument, bool registerHistory = false, LoadSceneMode mode = Additive)` | シーン遷移。遷移中の呼び出しは無視される（静かに return） |
| `Reload()` | 現在のシーンを現在の引数のまま再遷移 |
| `TransitionBack()` | 履歴の1つ前のシーンへ遷移（`registerHistory: true` で積まれた履歴を使用。Client 未使用） |
| `TransitionCancel()` | 進行中の遷移を CancellationTokenSource ごと中止（onCancel 発火） |
| `AppendTransition<TArgument>(argument)` | 加算シーン遷移: 現ロード済みシーンを Disable したまま残し、対象を Additive ロード + Prepare/Enter 実行。**Current は更新されない** |
| `ForceAppendTransition<TArgument>(argument)` | TransitionCancel してから AppendTransition |
| `UnloadTransition(TScenes transitionScene, SceneInstance<TScenes>)` | 加算シーンを Leave → アンロードし、戻り先シーンを Enable → OnTransition → Enter |
| `UnloadTransition(TScenes transitionScene, GameObject)` | gameObject の所属シーンから加算シーンを逆引きして同上 |
| `ForceUnloadTransition(TScenes, SceneInstance<TScenes>)` | TransitionCancel してから UnloadTransition |

ロード・アンロード系:

| メンバー | 説明 |
|---|---|
| `RegisterBootScene() : UniTask` | 起動中のシーンを現在シーンとして登録し Initialize→Prepare→Enter を駆動（`ApplicationInitializer` から呼出済み） |
| `Append<TArgument>(argument, bool activeOnLoad = true) : Observable<SceneInstance<TScenes>>` | 加算ロードのみ（**Prepare/Enter/Leave は呼ばれない**。SetArgument は実行）。戻り値購読で完了検知 |
| `Append(TScenes identifier, bool activeOnLoad = true)` | 同上の引数なし版 |
| `UnloadAppendScene(ISceneBase<TScenes> or SceneInstance<TScenes>, bool deactivateSceneObjects = true)` | 加算シーンのアンロード（ライフサイクル呼び出しなし） |
| `UnloadScene(TScenes identifier)` | 指定シーンをアンロード。**現在のシーン指定は ArgumentException** |
| `UnloadAllCacheScene() : UniTask` | キャッシュ済み全シーンをアンロード（タイトル強制遷移前の掃除等） |
| `FindAppendSceneInstance(GameObject) : SceneInstance<TScenes>` | GameObject の所属シーンから加算シーンを検索 |

状態・情報:

| メンバー | 説明 |
|---|---|
| `Current : SceneInstance<TScenes>` | 現在のシーン。**AppendTransition では変わらない** |
| `IsTransition : bool` | 遷移中か。呼び出し前ガードの慣例（実例1） |
| `TransitionTarget : TScenes?` | 遷移先（遷移中のみ非null） |
| `LoadedScenesInstances` / `AppendSceneInstances` | ロード済み / 加算ロード済みシーン一覧 |
| `IsSceneLoaded(id)` / `GetSceneInstance(id)` / `HasCache(id)` | ロード済み判定 / 取得 / キャッシュ有無 |
| `GetArgumentHistory()` / `ClearTransitionHistory()` | 遷移引数履歴の取得 / クリア（現在シーン分は残る） |
| `GetViewModel<TViewModel>(TScenes scene)` / `GetViewModel<TViewModel>()` | ロード済みシーン（IViewRoot 実装）の VM 取得。未ロードなら null（[View](View.md) 連携） |

待機・イベント:

| メンバー | 説明 |
|---|---|
| `BeginWait() : WaitHandler` | 遷移を「シーン表示直前」で待機させるハンドラ発行（IDisposable、`using` 可） |
| `FinishWait(WaitHandler)` / `CancelAllTransitionWait()` | 待機解除 / 全解除 |
| `OnPrepare/OnPrepareComplete/OnEnter/OnEnterComplete/OnLeave/OnLeaveComplete + AsObservable()` | ライフサイクル各フェーズ通知（`Observable<SceneInstance<TScenes>>`） |
| `OnLoadScene/OnLoadSceneComplete/OnUnloadScene/OnUnloadSceneComplete + AsObservable()` | ロード/アンロード通知 |
| `OnLoadError/OnUnloadError/OnCancel + AsObservable()` | エラー・キャンセル通知（`Observable<Unit>`） |
| `OnAppendTransitionAsObservable()` / `OnUnloadTransitionAsObservable()` | 加算遷移（加算シーン通知）/ 加算アンロード遷移（戻り先シーン通知） |

UniqueComponents（シーン跨ぎ単一コンポーネント管理）:

| メンバー | 説明 |
|---|---|
| `RegisterUniqueComponent<TComponent>(target)` / `RegisterUniqueComponent(Type, Behaviour)` | DontDestroyOnLoad の "UniqueComponents" ルートへ回収・管理 |
| `CollectUniqueComponents()` | 全ロード済みシーンから回収（重複は `DuplicatedSettings.DuplicateAction` に従い Destroy/Disable） |
| `SuspendCapturedComponents()` / `ResumeCapturedComponents()` | `RequireSuspend = true` のコンポーネントをロード中一時無効化 / 復元（ロード処理内で自動呼出） |

派生クラスが実装するもの（新規プロジェクト時のみ。Dominion では実装済み）:

| メンバー | 説明 |
|---|---|
| `ScenePaths : Dictionary<TScenes, string>`（abstract） | Client 実装は自動生成の `SceneDefinition.ScenePaths` |
| `UniqueComponents : Dictionary<Type, DuplicatedSettings>`（abstract） | Client 実装は EventSystem / PopupManager / LoadingScreen 等を登録 |
| `TransitionStart / TransitionFinish`（abstract） | Client 実装は `LoadingScreen.Instance.FadeOut()` / `FadeIn()`（`loadingAnimation` → `ShowLoading`） |
| `CacheSize`（virtual、既定3） | Client 実装は 5 |
| `OnRegisterCurrentScene(SceneInstance)`（virtual） | 起動シーン登録時フック |

### SceneBase&lt;TScenes&gt;（基盤）/ ISceneBase&lt;TScenes&gt;

| メンバー | 説明 |
|---|---|
| `Initialize() : UniTask`（virtual） | ロード時1回だけの初期化（View の Initialize 呼び出し等） |
| `Prepare() : UniTask`（virtual） | 遷移毎の準備（通信・アセット読込）。この間ローディング表示中 |
| `Enter() : void`（virtual） | 表示開始。**同期メソッド**（非同期開始は `.Forget()`） |
| `Leave() : UniTask`（virtual） | 離脱時の後始末 |
| `OnTransition() : UniTask`（virtual） | シーン Enable 直後・ローディング明け前のフック |
| `SetArgument / GetArgument / GetArgumentType`（abstract） | Client の `SceneBase<TArgument>` が実装済み |
| `IsLaunchScene` / `IsSceneBack` | このシーンから起動したか / TransitionBack で戻ってきたか |

### Client側 SceneArgument / SceneBase（`Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs`）

| メンバー | 説明 |
|---|---|
| `SceneArgument.Identifier : Scenes?`（abstract） | 遷移先シーン。必ず override |
| `SceneArgument.Cache`（virtual、既定 true） | 遷移後もシーンをキャッシュに残すか。状態を持ち回りたくないシーンは false（Title / Battle が該当） |
| `SceneArgument.PreLoadScenes`（virtual、既定空） | Enter 後に裏で事前ロードするシーン |
| `SceneArgument.loadingAnimation = true` | ローディングアイコン表示の有無 |
| `SceneArgument.bgm : Sounds.Bgm?` / `bgmId : uint?` | 指定すると SetArgument 時に自動で BGM 再生（`SetBgm`） |
| `SceneArgument.RegisterHistory`（virtual、既定 true） | このシーンを「戻る」遷移（`TransitionBack`）の対象として履歴に残すか。Boot / Title は false を宣言 |
| `SceneBase<TArgument>.Argument : TArgument` | 遷移引数（SetArgument 以降=Prepare から参照可） |
| `SceneBase<TArgument>.IsActive` | ルート GameObject のアクティブ制御 |
| `SceneBase<TArgument, TViewModel>.viewModel` / `GetViewModel()` | VM 保持と IViewRoot 実装（フィールド初期化子で生成済み） |

### SceneInstance&lt;TScenes&gt;

| メンバー | 説明 |
|---|---|
| `Identifier : TScenes?` / `Instance : ISceneBase<TScenes>` | シーン識別子 / SceneBase 実体 |
| `IsEnable` / `Enable()` / `Disable()` | ロード時にアクティブだったルート GameObject 群の一括アクティブ制御（`IgnoreControl` 付きは除外） |
| `Append` / `MarkAsAppend()` | AppendTransition で開かれたか（実例3の分岐に使用） |
| `GetScene() : UnityEngine.SceneManagement.Scene?` | Unity の Scene 構造体取得（`gameObject.scene` との比較に使用） |

### ISceneEvent / ITransitionHandler / IgnoreControl / WaitHandler

| メンバー | 説明 |
|---|---|
| `ISceneEvent.OnLoadScene() / OnUnloadScene() : UniTask` | シーンロード/アンロード時に発火。対象は SceneBase と**同一 GameObject 上**のコンポーネントのみ（子は対象外） |
| `ITransitionHandler.HandleTransition() : UniTask<bool>` | ロード済みシーンの SceneBase が実装すると遷移前に問い合わせが来る。false で遷移中止（Client 実装例なし） |
| `IgnoreControl.Type : IgnoreType`（Flags: `ActiveControl`） | 付けたルート GameObject を Enable/Disable 一括制御から除外 |
| `WaitHandler.Dispose()` / `OnDisposeAsObservable()` | Dispose で FinishWait 相当（BeginWait 内で購読済み） |

## 注意点・罠

- **`Transition` 系はすべて void（await 不可）**。内部で fire-and-forget 実行される。完了検知は `OnEnterCompleteAsObservable()` 等。呼び出し前に `if (sceneManager.IsTransition){ return; }` ガードを入れるのが実コードの慣例（遷移中の呼び出しは例外なく黙って無視されるため、押下連打等で「何も起きない」だけになる）
- **`Initialize` はロード時1回のみ**。`Cache = true`（Client 既定）のシーンはキャッシュから再表示された時 `Initialize` が呼ばれない。遷移毎に必要な処理は `Prepare` / `Enter` に書く
- **`Enter` は同期メソッド**。async にできない。非同期の開始処理は `ExecuteBattle().Forget()` のように投げる（`BattleScene.Enter` 参照）
- **`SetArgument` は `Prepare` より前**に呼ばれる。`Prepare` 内から `Argument` は参照可能。逆に `Initialize` 時点では未設定の場合がある（キャッシュヒット時を除き Load 直後の Initialize が先行）
- **AppendTransition 中は `Current` が更新されない**（Append 元を指し続ける）。仕様であり、戻り先の決定に利用されている（実例3）。加算シーンか否かは `SceneInstance.Append` で判定
- **加算シーンからの戻りは `UnloadTransition`**。`TransitionBack`（履歴戻り）は Client では未使用
- **履歴保持の判定**: bool 引数を省略した Client側 `SceneManager.Transition(arg, mode)` は「遷移元シーンの `RegisterHistory`」（既定 true。Boot / Title は false）に従って、遷移元を履歴に残すか自動判定する。bool を明示する基底の3引数版はプロパティを無視する（ErrorDialog / SystemModel の強制遷移が使用）
- **シーンルートに `SceneBase` 派生が必須**。ルート GameObject 配下から `ISceneBase` を検索するため、見つからないと `SceneBase class does not exist.` エラーで遷移が中断する
- **シーンロード直後、アクティブだったルート GameObject は一括非アクティブ化され、Prepare 完了 + TransitionWait 解除後に再アクティブ化される**。ルートオブジェクトの Awake/Start のタイミングに依存する実装をしない（プロジェクトの Unity ライフサイクル禁止ルールとも整合）。除外したいルート（常駐演出等）には `IgnoreControl` を付ける
- **`BeginWait` の解除漏れで遷移が永久に完了しなくなる**。`FinishWait` を必ず呼ぶか `using (BeginWait())` を使う
- **`Cache = false` にすべきシーン**: 遷移毎に引数・状態が変わるシーン（Battle は「戦闘ごとに BattleData が変わるためキャッシュしない」、Title も false）。キャッシュ済みシーンは `FixedQueue`（Client は5件）から溢れると自動アンロード
- **`LoadSceneMode.Single` 遷移はロード済み・キャッシュ・加算シーンを全部破棄する**。通常遷移は Additive を使い、Single は起動フロー（Boot→Title/Home）と強制タイトル遷移（`SystemModel.ForceTransitionToTitle`）のみ
- **遷移毎に `Resources.UnloadUnusedAssets()` + `GC.Collect()` が走る**（CleanUp）。遷移を高頻度に連発する設計は避ける
- `ISceneEvent` の `OnLoadScene` 発火対象は「ロード完了時点の currentScene」の SceneBase と同一 GameObject 上のコンポーネント（ロードされたシーン自身ではない点に注意）。`OnUnloadScene` はアンロードされるシーン側に発火する（非対称）
- `SceneManager`（Client）は非 MonoBehaviour の `Extensions.Singleton<T>`。`Instance` 初回アクセスで自動生成され、`CreateInstance()` の明示呼び出しは不要
- 現在のシーン・ActiveScene はアンロード不可（`UnloadScene(identifier)` は ArgumentException / 警告ログ）
- Client `SceneManager.cs` 内の `#if ENABLE_CRIWARE_ADX` ブロック（OnPrepareComplete での BGM 再生）は現状の定義では**コンパイル対象外**。BGM 自動再生の実体は `SceneBase.SetArgument → SetBgm`（`SceneArgument.bgm` / `bgmId`）
- 遷移時間は `TimeDiagnostics` により計測され、UnityConsole の "Scene" イベント（緑色）に `prev → next (ms)` 形式で出力される（性能調査時はここを見る）

## 関連

- [View](View.md) — `SceneBase<TArgument, TViewModel>` が `IViewRoot` を実装し VM を保持。`SceneManagerBase.GetViewModel<T>`（SceneManagerBase.View.cs）で他シーンの VM 取得
- [Window](Window.md) — シーン内のポップアップ・ウィンドウ（Open/Close ライフサイクル）。シーンを跨ぐ画面は本モジュール、シーン内に重ねる画面は Window を使う
- [UI](UI.md) — シーン内の View 実装で使う UI 部品
- [ExternalAsset](ExternalAsset.md) — `Prepare` 内でのアセット読込に使用

# Window

> **namespace**: `Modules.Window`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Window/`（Window.cs / PopupManager.cs / PopupStashManager.cs / TouchBlock.cs / PopupParent.cs の5ファイル。`Editor/` サブフォルダなし）
> **Client側使用**: `using Modules.Window` 直接は2ファイル（2026-07時点）。ただし実際の入口は Client側ラッパー `Dominion.Client.Module.Window`（`Client/Assets/Scripts/Client/Core/Popup/`）で、WindowBase 派生の全ウィンドウ約56ファイルが間接使用
> **依存**: UniTask / R3 / Extensions（`UnityUtility`, `SingletonMonoBehaviour`, `Singleton`, `Scope`）/ Modules.InputControl（`BlockInput`）/ Modules.Scene（シーン遷移連携）。Client側ラッパーはさらに Modules.Animation（`AnimationPlayer`）・Modules.BackKey・Sound

## 概要

ポップアップウィンドウ基盤。`Window` が開閉ライフサイクル（`Open{ Prepare → SetActive(true) → OnOpen → Status=Opened }` / `Close{ OnClose → SetActive(false) → Status=Closed → DeleteOnCloseなら破棄 }`）を、`PopupManager` が多重表示管理（表示順・背面タッチブロック・シーン遷移時の破棄）を担う。
Client側の実装入口は `Dominion.Client.Module.Window` の `WindowBase` / `PopupManager` / `CommonDialog`。新しいポップアップは `WindowBase` を継承し `PopupManager.Open()` で表示する。VM接続（`WindowViewModel`）は [View](View.md) を参照（本ドキュメントは開閉・管理側を扱う）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しいポップアップ画面を作りたい | Client側 `WindowBase` 継承（partial 2ファイル形式 → 手順は下記・VM詳細は [View](View.md)） |
| ウィンドウを開きたい | `await PopupManager.Open(window)`（Client側 `Dominion.Client.Module.Window.PopupManager` の static） |
| シーン遷移で消えないウィンドウを開きたい | `PopupManager.Open(window, isGlobal: true)`（DontDestroyOnLoad 配下の Global 親に載る） |
| ウィンドウが閉じるまで待ちたい | `await window.Wait()` |
| ウィンドウを閉じたい | `await window.Close()`（PopupManager からの除去は自動） |
| 確認ダイアログ（はい/いいえ 等）を出したい | `CommonDialog.Create(type, title, content)` → `PopupManager.Open(dialog)` |
| 閉じてもインスタンスを使い回したい | `window.DeleteOnClose = false;`（デフォルト true = Close時破棄） |
| 背景（ウィンドウ外）タップで閉じるのを止めたい | `window.CloseIfTouchOutside = false;`（WindowBase、デフォルト true） |
| Android バックキーで閉じたい | 自動対応（`WindowBase.ReceiveBackKeyEvent` デフォルト true） |
| 開く前にデータ・Viewを非同期更新したい | `protected override UniTask Prepare()` を override |
| 開閉アニメ・SEを変えたい | `OpenAnimationName` / `CloseAnimationName` override、`OpenSe` / `CloseSe` プロパティ |
| 開閉完了を購読したい | `window.OnOpenAsObservable()` / `OnCloseAsObservable()` |
| 全ウィンドウの開閉をフックしたい | `PopupManager` の `OnOpenWindow / OnOpened / OnClosedWindowAsObservable()` |
| 最前面のウィンドウを知りたい | `PopupManager.Instance.Current`（`GetCurrentWindow()` も同等） |
| 何かウィンドウが開いている間待ちたい | `await UniTask.WaitWhile(() => popupManager.Current != null)` |
| 加算シーン遷移時にポップアップを保持→戻りで復元したい | `PopupStashManager.Instance.Stash()` / `Restore()` |

## 主要クラス

### 基盤側（Modules.Window）

| クラス | 種別 | 役割 |
|---|---|---|
| `Window` | abstract MonoBehaviour | 開閉ライフサイクルと `Status` 管理。`Prepare / OnOpen / OnClose` が override ポイント |
| `PopupManager<TInstance>` | abstract SingletonMonoBehaviour | 開いている Window のリスト管理（Scene用 / Global用の2系統）、親GameObject生成、TouchBlock 制御 |
| `IPopupManager` | interface | `Current` プロパティのみ。BackKey 連携用の抽象 |
| `PopupStashManager<TInstance, TScenes, TPopupManager, TSceneManager>` | abstract Singleton（非MonoBehaviour） | ScenePopups の退避/復元スタック（加算シーン遷移用）。シーン Leave 時に自動破棄 |
| `TouchBlock` | sealed MonoBehaviour | 背面タッチブロック兼 暗幕（フェードイン/アウト）。タップ通知を PopupManager へ流す |
| `PopupParent` | sealed MonoBehaviour | ポップアップ親プレハブの参照ホルダ（`Canvas` と実親 `Parent`） |

### Client側ラッパー（namespace `Dominion.Client.Module.Window` — 実装時はこちらを使う）

| クラス | 場所 | 役割 |
|---|---|---|
| `WindowBase` | `Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs` | 全ウィンドウの基底。`OnOpen/OnClose` 実装済み（`AnimationPlayer` の "Open"/"Close" 再生 + 開閉SE + BackKeyReceiver 自動付与） |
| `PopupManager` | `Client/Assets/Scripts/Client/Core/Popup/PopupManager.cs` | シーン遷移イベントと連携（InScene親の再生成・Leave時 `Clean()`）。背景タップで `Current` を Close |
| `PopupStashManager` | `Client/Assets/Scripts/Client/Core/Popup/PopupStashManager.cs` | 型引数を束ねただけの sealed（実装なし） |
| `CommonDialog` | `Client/Assets/Scripts/Client/Core/Popup/CommonDialog.cs` | 汎用確認ダイアログ（WindowBase 派生）。`CommonDialog.Create()` ファクトリで生成 |
| `WindowBackKeyReceiver` | `Client/Assets/Scripts/Client/Core/BackKey/WindowBackKeyReceiver.cs` | BackKey で `Current` のウィンドウを Close（チュートリアル中は無効） |

関連プレハブ（Client側）:

| プレハブ | 用途 |
|---|---|
| `Client/Assets/Resource (Internal)/Core/Prefabs/Manager/PopupManager.prefab` | PopupManager 本体（起動時に `InitializeObject.CreatePopupManager()` が生成・`Initialize()`） |
| `Client/Assets/Resource (Internal)/Core/Prefabs/Popup/CommonDialog.prefab` | CommonDialog（PopupManager が SerializeField 参照） |
| `Client/Assets/Resource (Internal)/Core/Prefabs/Popup/TouchBlock.prefab` | タッチブロック暗幕 |

## 使い方(実例)

### 開閉ライフサイクル全体像（実行順序と override ポイント）

```
Window.Open(blockInput = true):
  [None/Closed のみ実行。Prepare/Opened/Close 中は無視]
  Status=Prepare → BlockInput生成（全入力ロック）
  → await Prepare()          ← override: 開く前のデータ取得・View更新（GameObjectはまだ非アクティブ）
  → SetActive(true)
  → await OnOpen()           ← override: 開くアニメ（WindowBaseは BackKeyReceiver付与 + SE + "Open"アニメ）
  → 入力ロック解除 → Status=Opened → OnOpenAsObservable 発火

Window.Close(blockInput = true):
  [Opened のみ実行]
  Status=Close → BlockInput生成
  → await OnClose()          ← override: 閉じるアニメ（WindowBaseは SE + "Close"アニメ）
  → SetActive(false) → 入力ロック解除 → OnCloseAsObservable 発火 → Status=Closed
  → DeleteOnClose(デフォルトtrue) なら GameObject 破棄

PopupManager.Open(window, isGlobal = false, inputProtect = true):
  SetActive(false) → Register{Scene|Global}（Popup親へ SetParent + SetLayer + Close購読で自動リスト除去）
  → DisplayPriority 昇順ソート → UpdateContents()（siblingIndex再配置・TouchBlockを最前面ウィンドウ直下へ・FadeIn・Current更新）
  → OnOpenWindow 発火 → await window.Open(inputProtect) → OnOpenedWindow 発火
  ※ Status==Opened のwindowを渡すと登録のみ（最前面化・アニメ再実行なし。Stash復元用）
```

### 新しい Window/ポップアップを追加する手順

| 手順 | 内容 |
|---|---|
| 1 | スクリプト: `XxxWindow.cs`（`public sealed partial class XxxWindow : WindowBase, IViewRoot`）+ `XxxWindow.viewmodel.cs`（ネスト `WindowViewModel`）の2ファイル形式。`Setup()` → 内部 `Initialize()` で `viewModel = new WindowViewModel()` とボタン購読（雛形・VM詳細は [View](View.md) 実例1） |
| 2 | プレハブ: `Client/Assets/Resource (Internal)/Feature/<機能>/Prefab/.../XxxWindow.prefab` または `Resource (Internal)/Scene/<シーン>/Prefab/.../XxxWindow.prefab` に配置（例: `Feature/Citadel/Prefab/CitadelDetailWindow/CitadelDetailWindow.prefab`）。ルートに XxxWindow コンポーネント + `AnimationPlayer`（"Open"/"Close" アニメ） |
| 3 | 呼び出し側に `[SerializeField] private GameObject xxxWindowPrefab = null;` でプレハブ直接参照（**Window を ExternalAsset 経由でロードする実績はない**。全て SerializeField 参照） |
| 4 | `UnityUtility.Instantiate<XxxWindow>(null, xxxWindowPrefab)` → `Setup(...)` → `await PopupManager.Open(window)` → 必要なら `await window.Wait()`。親は null でよい（PopupManager が Popup親配下へ付け替える） |

### 実例1: 生成 → Setup → Open → Wait（標準形）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs
private async UniTask OnDetailButtonClick()
{
    if (Content == null){ return; }

    // 拠点詳細ウィンドウを開く.
    var citadelDetailWindow = UnityUtility.Instantiate<CitadelDetailWindow>(null, citadelDetailWindowPrefab);

    citadelDetailWindow.Setup(Content.UserCitadelId);

    await PopupManager.Open(citadelDetailWindow);

    await citadelDetailWindow.Wait();
}
```

`Wait()` は「ウィンドウが閉じる（非アクティブ化 or 破棄）まで」待つ。閉じた後の後続処理（結果の受け取り等）はこの後に書く。

### 実例2: 確認ダイアログ（CommonDialog）

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Trade/Sell/ItemSellWindow/ItemSellWindow.cs（抜粋）
var result = false;

var dialog = CommonDialog.Create(CommonDialog.Type.Default, title, content);

dialog.AcceptButton.text = TextData.Get(TextData.Window.ItemSellWindow_SellButton);
dialog.RejectButton.text = TextData.Get(TextData.General.Cancel);

dialog.AcceptButton.OnClick(() => result = true);

await PopupManager.Open(dialog);

await dialog.Wait();

return result;
```

`Type.Default` = はい/いいえ、`Type.Accept` = はいのみ、`Type.Close` = 閉じるのみ。`AutoCloseOnSelection = true`（Create のデフォルト）ならボタン押下で自動 Close されるので、OnClick には結果の記録だけ書く。

### 実例3: シーンを跨ぐグローバル表示（isGlobal: true）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Dialog/AppVersionDialog.cs（抜粋）
var dialog = CommonDialog.Create(CommonDialog.Type.Accept, title, content, isGlobal: true);

dialog.AcceptButton.text = TextData.Get(TextData.Window.AppVersionDialog_OpenStoreButton);

dialog.AcceptButton.OnClick(() => OpenStore());

await PopupManager.Open(dialog, isGlobal: true);

await dialog.Wait();
```

Global はシーン Leave の `Clean()` 対象外（アプリ更新・メンテ通知・DeveloperMenu 等のシステム系が使用）。通常のゲーム内ウィンドウはデフォルト（Scene 側）でよい。

### 実例4: 使い回しウィンドウ（DeleteOnClose = false）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Home/View/HomeView.cs
private async UniTask OpenBattleHistoryWindow()
{
    if (battleHistoryWindow == null)
    {
        battleHistoryWindow = UnityUtility.Instantiate<BattleHistoryWindow>(null, battleHistoryWindowPrefab);

        battleHistoryWindow.DeleteOnClose = false;
    }

    battleHistoryWindow.Setup();

    await PopupManager.Open(battleHistoryWindow);

    await battleHistoryWindow.Wait();
}
```

Close 後も GameObject が残り、`Status=Closed` から再 `Open()` 可能。再表示ごとに `Setup()` → `PopupManager.Open()` を通す。

### 実例5: Prepare override（開く前の非同期コンテンツ更新）

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Citadel/CitadelDetailWindow/CitadelDetailWindow.cs
protected override async UniTask Prepare()
{
    var tasks = new List<UniTask>()
    {
        infoView.ContentUpdate(),
        partyTabView.ContentUpdate(),
    };

    await UniTask.WhenAll(tasks);
}
```

Prepare 中は GameObject 非アクティブ + 入力ロック中なので、開くアニメ前に内容を組み立てるのに使う。何もしない場合も abstract ではないため override 不要（`UniTask.CompletedTask` を返す実装が既定）。

### 実例6: 加算シーン遷移でのポップアップ退避/復元（PopupStashManager）

ウィンドウを開いたまま加算シーンへ遷移し、戻ったときに再表示するパターン。

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs（抜粋）
// ポップアップを退避.
popupStashManager.Stash();

// 編成シーンへ遷移.
sceneManager.AppendTransition(sceneArgument);
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapScene.cs（Initialize 抜粋）
sceneManager.OnUnloadTransitionAsObservable()
    .Where(x => x.Identifier == Scenes.WorldMap)
    .Subscribe(_ => popupStashManager.Restore())
    .AddTo(this);
```

`Stash()` は現在シーンの ScenePopups 全部を PopupManager から `Unregister`（閉じアニメなし）して `Popup (Stashed)` コンテナへ退避。`Restore()` が LIFO で戻す（Status=Opened のままなので再登録のみ・アニメ再実行なし）。

## API(主要公開メンバー)

### Window（abstract MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `Status : WindowStatus` | `None / Prepare(準備中) / Opened(表示中) / Close(閉じ処理中) / Closed(閉じた)`。`WaitUntil(() => window.Status == Window.WindowStatus.Opened)` のような待機にも使う（実例: `Client/Assets/Scripts/Client/Tutorial/TutorialControllerBase.cs`） |
| `DeleteOnClose : bool` | Close 完了時に GameObject を破棄するか（SerializeField・デフォルト true） |
| `DisplayPriority : int` | PopupManager 内の表示順ソートキー（昇順）。Client側で明示設定している実績なし（全て 0 = 登録順） |
| `Open(bool blockInput = true) : UniTask` | 開く。実行中は BlockInput で全入力ロック。Prepare/Opened/Close 中の呼び出しは無視 |
| `Close(bool blockInput = true) : UniTask` | 閉じる。Opened 以外では無視 |
| `Wait() : UniTask` | 非アクティブ化 or 破棄まで毎フレームポーリングして待つ |
| `OnOpenAsObservable() / OnCloseAsObservable() : Observable<Unit>` | 開き完了 / 閉じ完了（SetActive(false) 直後・破棄前）の通知 |
| `protected virtual Prepare() / OnOpen() / OnClose() : UniTask` | override ポイント（上記ライフサイクル参照）。基底は全て CompletedTask |

### PopupManager&lt;TInstance&gt;（abstract SingletonMonoBehaviour）

| メンバー | 説明 |
|---|---|
| `static Open(Window popupWindow, bool isGlobal = false, bool inputProtect = true) : UniTask` | 登録 + 親付け替え + `window.Open()`。null を渡すと ArgumentException。Opened 済みなら登録のみ（最前面化） |
| `static Unregister(Window popupWindow)` | 管理対象から外すのみ（閉じアニメなし・ウィンドウは開いたまま）。Stash 用 |
| `Initialize()` | Global 親を生成し DontDestroyOnLoad（Client側 override がシーン遷移購読を追加） |
| `CreateInSceneParent(GameObject sceneRoot)` | InScene 親を再生成（Client側がシーン遷移イベントで自動呼び出し。手動呼び出し不要） |
| `Clean()` | ScenePopups を全て即破棄（Close は通らない）。Global は残す |
| `Current : Window` | 最前面のウィンドウ（Global 優先）。無ければ null。`GetCurrentWindow()` も同等 |
| `ScenePopups / GlobalPopups : IReadOnlyList<Window>` | 登録中リスト（DisplayPriority 昇順 = 後ろほど前面） |
| `ParentInScene / ParentGlobal : GameObject` | ポップアップの実親（CommonDialog.Create が生成先に使用） |
| `TouchBlock : TouchBlock` | 暗幕への直接アクセス |
| `OnBlockTouchAsObservable() : Observable<Unit>` | 暗幕（ウィンドウ外）タップ通知 |
| `OnOpenWindowAsObservable() / OnOpenedWindowAsObservable() / OnClosedWindowAsObservable() : Observable<Window>` | 開き始め / 開き完了 / 閉じ完了のグローバルフック |
| `protected abstract ParentInSceneLayer / ParentGlobalLayer : int` | 親レイヤー（Client実装: `Layer.Default` / `Layer.Overlap`） |

### PopupStashManager&lt;TInstance, TScenes, TPopupManager, TSceneManager&gt;（abstract Singleton）

| メンバー | 説明 |
|---|---|
| `Initialize()` | シーン Leave 購読等（Client側は起動時 InitializeObject が呼び出し済み） |
| `Stash()` | 現在シーンの ScenePopups を退避（スタックに push・非アクティブ化） |
| `Restore()` | 直近の退避を復元（PopupManager へ再登録） |
| `DiscardTop() / DiscardAll()` | 退避を破棄（中身の Window も削除） |
| `HasStash : bool` / `StashCount : int` | 退避エントリの有無 / 数 |

### TouchBlock / PopupParent

| メンバー | 説明 |
|---|---|
| `TouchBlock.FadeIn / FadeOut(CancellationToken) : UniTask` | 暗幕のフェード（PopupManager が自動制御。手動操作は基本不要） |
| `TouchBlock.OnBlockTouchAsObservable() : Observable<Unit>` | 暗幕タップ通知（PopupManager 経由で購読するのが通常） |
| `TouchBlock.SetAlpha(float) / Hide()` / `Active : bool` | 透明度直接操作 / 表示状態 |
| `PopupParent.Canvas : Canvas` / `Parent : GameObject` | 親プレハブ内の Canvas / ウィンドウの実親 |

### WindowBase（Client側・abstract）

| メンバー | 説明 |
|---|---|
| `CloseIfTouchOutside : bool` | 背景タップで Close するか（SerializeField・デフォルト true） |
| `ReceiveBackKeyEvent : bool` | BackKey を受けるか（SerializeField・デフォルト true。OnOpen 時に `WindowBackKeyReceiver` を自動付与） |
| `protected virtual OpenAnimationName / CloseAnimationName : string` | AnimationPlayer のステート名（既定 "Open" / "Close"） |
| `OpenSe / CloseSe : Sounds.Se` | 開閉SE（既定 `window_open` / `window_close`） |
| `OnOpen() / OnClose()`（実装済み） | SE再生 + アニメ再生。独自演出時は override して `base.OnOpen()` を呼ぶ（実例: CommonDialog） |

### CommonDialog（Client側）

| メンバー | 説明 |
|---|---|
| `static Create(Type type, string title, string content, float width = 950f, bool isGlobal = false) : CommonDialog` | PopupManager の CommonDialogPrefab から生成・初期化（非アクティブ状態で返る。表示は `PopupManager.Open`） |
| `Type`（enum） | `Default`（はい/いいえ）/ `Accept`（はいのみ）/ `Reject`（いいえのみ）/ `Close`（閉じるのみ） |
| `AcceptButton / RejectButton / CloseButton : UIButton` | ボタン参照（text 差し替え・OnClick 追加用。CloseButton は RejectButton の別名） |
| `title / content : string` | 表示テキスト（TextData から取得した文字列を渡す） |
| `AutoCloseOnSelection : bool` | ボタン押下で自動 Close（Create 経由なら true 設定済み） |
| `SetCustomButtons(GameObject[]) / DeleteCustomButtons()` | 独自ボタンの差し込み |
| `SetSize(Vector2)` / `ContentMaxHeight : float?` | サイズ調整 |

## 注意点・罠

- **DeleteOnClose はデフォルト true**。`Close()` で GameObject ごと破棄される。使い回す場合は Open 前に `DeleteOnClose = false`（実例4）。破棄後のフィールド参照に注意
- **`Wait()` は Open 前に呼ぶと即抜けする**（非アクティブ判定のため）。必ず `await PopupManager.Open(window)` → `await window.Wait()` の順
- **Open/Close は Status ガードで多重呼び出しを無視する**。Opened 中の再 `PopupManager.Open()` は「最前面化（再登録）」になり、Openアニメ・onOpen 通知は再実行されない
- **シーン Leave 時に ScenePopups は `Clean()` で即破棄される**（Close を通らない = OnClose も OnCloseAsObservable も発火しない）。シーンを跨ぎたいものは `isGlobal: true`、加算遷移で戻るなら PopupStashManager（実例6）
- **Instantiate の親は null でよい**。`PopupManager.Open` が Popup 親へ SetParent + SetLayer する。逆に PopupManager を通さず自前で `window.Open()` だけ呼ぶと、親・レイヤー・TouchBlock・BackKey 対象（Current）管理から外れるので原則やらない（PopupManager 管理外の Window は背景タップ・BackKey の対象にならない）
- **Open/Close 実行中は BlockInput で全入力ロック**。`Prepare()` に通信等の長い処理を書くとその間タップ不能になる。ロック不要なら `Open(blockInput: false)`
- **背景タップで閉じる挙動は Client側 PopupManager が実装**（`Current` の `CloseIfTouchOutside` を見て Close）。「ボタンでしか閉じさせない」ダイアログは `CloseIfTouchOutside = false` にする（インスペクタでも設定可）
- **BackKey は最前面（Current）のウィンドウのみ反応**（`WindowBackKeyReceiver.HandleBackKey` が `popupManager.Current != window` で弾く）。チュートリアル中は Client側 override で無効化済み
- **TouchBlock（暗幕）は1枚だけを最前面ウィンドウの直下に差し込む方式**。多重表示時は最前面のみ操作可能になる。暗幕の見た目を個別ウィンドウで変えることはできない
- **`PopupManager.Unregister` は「閉じる」ではない**。リストから外すだけでウィンドウは表示されたまま（Stash 専用と考える）。通常は `window.Close()` を呼べば Close 購読経由で自動除去される
- **PopupManager は起動時に生成済み**（`InitializeObject.CreatePopupManager()` が `PopupManager.prefab` を Instantiate → `Initialize()`。引用元: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`）。`CreateInstance()` は SerializeField が空のインスタンスを作ってしまうため使わない
- **Stash は ScenePopups のみ対象**（Global は退避されない）。退避中の Window は所有シーンの Leave 時に自動破棄される（戻らず別シーンへ抜けた場合のリーク対策が組み込み済み）
- Window の GameObject が Open/Close 途中で破棄されると入力ロック解除が GC（Scope のファイナライザ）任せになる。演出中の強制破棄は避け、`Close()` を経由する

## 関連

- [View](View.md) — `WindowViewModel`（partial 2ファイル形式）・子View の VM 自動解決。**新規 Window 実装の雛形はこちら**
- [Scene](Scene.md) — シーン遷移イベント（OnPrepare / OnLeaveComplete / 加算遷移）。PopupManager の親再生成・Clean・Stash が連動
- [UI](UI.md) — `UIButton` / `UIText` 等、ウィンドウ内部の UI 部品
- [Animation](Animation.md) — `AnimationPlayer`（WindowBase の開閉アニメ再生。`Modules.Animation`）
- [BackKey](BackKey.md) — `BackKeyReceiver` / `BackKeyManager`（Android バックキー基盤。`IPopupManager` 経由で本モジュールと連携）
- [InputControl](InputControl.md) — `BlockInput`（開閉中の入力ロック）
- [Sound](Sound.md) — `SoundPlayer.Se`（WindowBase の開閉SE）
- [Extensions/Core.md](../Extensions/Core.md) — `SingletonMonoBehaviour` / `Singleton` / `Scope`

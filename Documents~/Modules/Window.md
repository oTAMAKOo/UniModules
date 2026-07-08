# Window

> **namespace**: `Modules.Window`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Window/`（Window.cs / PopupManager.cs / PopupStashManager.cs / TouchBlock.cs / PopupParent.cs の5ファイル。`Editor/` サブフォルダなし）
> **Client側使用**: `using Modules.Window` 直接は2ファイル（2026-07時点）。ただし実際の入口は Client側ラッパー `Dominion.Client.Module.Window`（`Client/Assets/Scripts/Client/Core/Popup/`）で、WindowBase 派生の全ウィンドウ約56ファイルが間接使用
> **依存**: UniTask / R3 / Extensions（`UnityUtility`, `SingletonMonoBehaviour`, `Singleton`, `Scope`）/ Modules.InputControl（`BlockInput`）/ Modules.Scene（シーン遷移連携）。Client側ラッパーはさらに Modules.Animation（`AnimationPlayer`）・Modules.BackKey・Sound

## 概要

ポップアップウィンドウ基盤。`Window` が開閉ライフサイクル（`Open{ Prepare → SetActive(true) → OnOpen → Status=Opened }` / `Close{ OnClose → SetActive(false) → Status=Closed → DeleteOnCloseなら破棄 }`）を、`PopupManager` が多重表示管理（表示順・背面タッチブロック・シーン遷移時の破棄）を担う。
Client側の実装入口は `Dominion.Client.Module.Window` の `WindowBase` / `PopupManager` / `CommonDialog`。新しいポップアップは `WindowBase` を継承し `PopupManager.Open()` で表示する。VM接続（`WindowViewModel`）は [View](View.md) を参照（本ドキュメントは開閉・管理側を扱う）。
主要クラス（基盤側）: `Window`（開閉ライフサイクルと `Status` 管理。`Prepare / OnOpen / OnClose` が override ポイント）/ `PopupManager<TInstance>`（開いている Window のリスト管理〈Scene用 / Global用の2系統〉・親GameObject生成・TouchBlock 制御）/ `PopupStashManager<...>`（ScenePopups の退避/復元スタック。加算シーン遷移用。シーン Leave 時に自動破棄）/ `TouchBlock`（背面タッチブロック兼 暗幕）/ `PopupParent`（ポップアップ親プレハブの参照ホルダ）/ `IPopupManager`（BackKey 連携用の抽象）。

### Client側ラッパー（namespace `Dominion.Client.Module.Window` — 実装時はこちらを使う）

| クラス | 場所 | 役割 |
|---|---|---|
| `WindowBase` | `Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs` | 全ウィンドウの基底。`OnOpen/OnClose` 実装済み（`AnimationPlayer` の "Open"/"Close" 再生 + 開閉SE + BackKeyReceiver 自動付与） |
| `PopupManager` | `Client/Assets/Scripts/Client/Core/Popup/PopupManager.cs` | シーン遷移イベントと連携（InScene親の再生成・Leave時 `Clean()`）。背景タップで `Current` を Close |
| `PopupStashManager` | `Client/Assets/Scripts/Client/Core/Popup/PopupStashManager.cs` | 型引数を束ねただけの sealed（実装なし） |
| `CommonDialog` | `Client/Assets/Scripts/Client/Core/Popup/CommonDialog.cs` | 汎用確認ダイアログ（WindowBase 派生）。`CommonDialog.Create()` ファクトリで生成 |
| `WindowBackKeyReceiver` | `Client/Assets/Scripts/Client/Core/BackKey/WindowBackKeyReceiver.cs` | BackKey で `Current` のウィンドウを Close（チュートリアル中は無効） |

関連プレハブ（Client側）: `Client/Assets/Resource (Internal)/Core/Prefabs/` 配下 — `Manager/PopupManager.prefab`（PopupManager 本体）/ `Popup/CommonDialog.prefab`（PopupManager が SerializeField 参照）/ `Popup/TouchBlock.prefab`（タッチブロック暗幕）。

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

## 使い方

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
| 1 | スクリプト: `XxxWindow.cs`（`public sealed partial class XxxWindow : WindowBase, IViewRoot`）+ `XxxWindow.viewmodel.cs`（ネスト `WindowViewModel`）の2ファイル形式。`Setup()` → 内部 `Initialize()` で `viewModel = new WindowViewModel()` とボタン購読（雛形・VM詳細は [View](View.md)） |
| 2 | プレハブ: `Client/Assets/Resource (Internal)/Feature/<機能>/Prefab/.../XxxWindow.prefab` または `Resource (Internal)/Scene/<シーン>/Prefab/.../XxxWindow.prefab` に配置（例: `Feature/Citadel/Prefab/CitadelDetailWindow/CitadelDetailWindow.prefab`）。ルートに XxxWindow コンポーネント + `AnimationPlayer`（"Open"/"Close" アニメ） |
| 3 | 呼び出し側に `[SerializeField] private GameObject xxxWindowPrefab = null;` でプレハブ直接参照（**Window を ExternalAsset 経由でロードする実績はない**。全て SerializeField 参照） |
| 4 | `UnityUtility.Instantiate<XxxWindow>(null, xxxWindowPrefab)` → `Setup(...)` → `await PopupManager.Open(window)` → 必要なら `await window.Wait()`。親は null でよい（PopupManager が Popup親配下へ付け替える） |

### 定型パターン（引用元）

- 生成 → Setup → Open → Wait（標準形）: `Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs`。`Wait()` は「ウィンドウが閉じる（非アクティブ化 or 破棄）まで」待つ。閉じた後の後続処理（結果の受け取り等）はこの後に書く
- 確認ダイアログ（CommonDialog）: `Client/Assets/Scripts/Client/Feature/Trade/Sell/ItemSellWindow/ItemSellWindow.cs`。`Type.Default` = はい/いいえ、`Type.Accept` = はいのみ、`Type.Close` = 閉じるのみ。`AutoCloseOnSelection = true`（Create のデフォルト）ならボタン押下で自動 Close されるので、OnClick には結果の記録だけ書く
- シーンを跨ぐグローバル表示（`isGlobal: true`）: `Client/Assets/Scripts/Client/Core/Dialog/AppVersionDialog.cs`。Global はシーン Leave の `Clean()` 対象外（アプリ更新・メンテ通知・DeveloperMenu 等のシステム系が使用）。通常のゲーム内ウィンドウはデフォルト（Scene 側）でよい
- 使い回しウィンドウ（`DeleteOnClose = false`）: `Client/Assets/Scripts/Client/Scene/Home/View/HomeView.cs`。Close 後も GameObject が残り `Status=Closed` から再 `Open()` 可能。再表示ごとに `Setup()` → `PopupManager.Open()` を通す
- Prepare override（開く前の非同期コンテンツ更新）: `Client/Assets/Scripts/Client/Feature/Citadel/CitadelDetailWindow/CitadelDetailWindow.cs`。Prepare 中は GameObject 非アクティブ + 入力ロック中なので、開くアニメ前に内容を組み立てるのに使う。何もしない場合は override 不要（基底は CompletedTask）
- ポップアップ退避/復元（加算シーン遷移）: 退避側 `Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs`（`Stash()` → `AppendTransition`）/ 復元側 `Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapScene.cs`（`OnUnloadTransitionAsObservable` 購読で `Restore()`）。`Stash()` は現在シーンの ScenePopups 全部を PopupManager から `Unregister`（閉じアニメなし）して `Popup (Stashed)` コンテナへ退避、`Restore()` が LIFO で戻す（Status=Opened のままなので再登録のみ・アニメ再実行なし）
- 開き完了までの待機: `WaitUntil(() => window.Status == Window.WindowStatus.Opened)`（実例: `Client/Assets/Scripts/Client/Tutorial/TutorialControllerBase.cs`）
- 独自の開閉演出: `OnOpen()` / `OnClose()` を override して `base.OnOpen()` を呼ぶ（実例: `CommonDialog`）

## 注意点・罠

- **DeleteOnClose はデフォルト true**。`Close()` で GameObject ごと破棄される。使い回す場合は Open 前に `DeleteOnClose = false`（「使い回しウィンドウ」パターン）。破棄後のフィールド参照に注意
- **`Wait()` は Open 前に呼ぶと即抜けする**（非アクティブ判定のため）。必ず `await PopupManager.Open(window)` → `await window.Wait()` の順
- **Open/Close は Status ガードで多重呼び出しを無視する**。Opened 中の再 `PopupManager.Open()` は「最前面化（再登録）」になり、Openアニメ・onOpen 通知は再実行されない
- **シーン Leave 時に ScenePopups は `Clean()` で即破棄される**（Close を通らない = OnClose も OnCloseAsObservable も発火しない）。シーンを跨ぎたいものは `isGlobal: true`、加算遷移で戻るなら PopupStashManager（「ポップアップ退避/復元」パターン）
- **Instantiate の親は null でよい**。`PopupManager.Open` が Popup 親へ SetParent + SetLayer する。逆に PopupManager を通さず自前で `window.Open()` だけ呼ぶと、親・レイヤー・TouchBlock・BackKey 対象（Current）管理から外れるので原則やらない（PopupManager 管理外の Window は背景タップ・BackKey の対象にならない）
- **Open/Close 実行中は BlockInput で全入力ロック**。`Prepare()` に通信等の長い処理を書くとその間タップ不能になる。ロック不要なら `Open(blockInput: false)`
- **背景タップで閉じる挙動は Client側 PopupManager が実装**（`Current` の `CloseIfTouchOutside` を見て Close）。「ボタンでしか閉じさせない」ダイアログは `CloseIfTouchOutside = false` にする（インスペクタでも設定可）
- **BackKey は最前面（Current）のウィンドウのみ反応**（`WindowBackKeyReceiver.HandleBackKey` が `popupManager.Current != window` で弾く）。チュートリアル中は Client側 override で無効化済み
- **TouchBlock（暗幕）は1枚だけを最前面ウィンドウの直下に差し込む方式**。多重表示時は最前面のみ操作可能になる。暗幕の見た目を個別ウィンドウで変えることはできない
- **`PopupManager.Unregister` は「閉じる」ではない**。リストから外すだけでウィンドウは表示されたまま（Stash 専用と考える）。通常は `window.Close()` を呼べば Close 購読経由で自動除去される
- **PopupManager は起動時に生成済み**（`InitializeObject.CreatePopupManager()` が `PopupManager.prefab` を Instantiate → `Initialize()`。引用元: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`）。`CreateInstance()` は SerializeField が空のインスタンスを作ってしまうため使わない。PopupStashManager の `Initialize()` も起動時に InitializeObject が呼び出し済み
- **Stash は ScenePopups のみ対象**（Global は退避されない）。退避中の Window は所有シーンの Leave 時に自動破棄される（戻らず別シーンへ抜けた場合のリーク対策が組み込み済み）
- `Current` は Global 優先（GlobalPopups にあればそちらの最前面を返す。無ければ ScenePopups の最前面、どちらも無ければ null）
- `DisplayPriority`（PopupManager 内の表示順ソートキー・昇順）を Client側で明示設定している実績はない（全て 0 = 登録順）
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

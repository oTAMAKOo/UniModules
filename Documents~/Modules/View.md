# View

> **namespace**: `Modules.View`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/View/`（View.cs / ViewBase.cs / ViewModel.cs の3ファイルのみ）
> **Client側使用**: 約183ファイル（2026-07時点）
> **依存**: R3（UniRxではない） / Unity.Linq（LINQtoGameObject: `AncestorsAndSelf`） / Extensions（`LifetimeDisposable`, `UnityUtility`）

## 概要

MVVM 風の View–ViewModel 接続基盤。画面の状態を `ViewModel`（非MonoBehaviour の POCO）に集約し、画面ルート（Window / Scene 等）が `IViewRoot` として VM を1個保持、配下の子 UI 部品（`ViewBase<T>` 継承）はヒエラルキーを親方向に遡って同じ VM を自動解決・共有する。
このモジュール自体は接続機構のみで、画面ライフサイクル（初期化・表示・破棄）は持たない。ライフサイクルは Modules.Scene（`Initialize → Prepare → Enter → Leave`）と Modules.Window（`Open{Prepare → OnOpen} → Close{OnClose}`）側が担う。
主要クラス: `ViewModel`（画面状態の基底。非MonoBehaviour・`LifetimeDisposable` 継承。`OnDisposeAsObservable()` で破棄通知）/ `ViewBase<TViewModel>`（子 View 部品の基底。`ViewModel` プロパティで祖先 IViewRoot の VM を遅延解決・キャッシュ）/ `IViewRoot`（VM の提供者。`GetViewModel()` 1メソッドのみ。任意の MonoBehaviour に実装可 — Client側では WindowBase 派生・SceneBase・SidePanelView 派生等が実装）/ `IView<TViewModel>`（メンバーなしマーカー interface）/ `ViewExtensions`（VM 解決の実体。`gameObject.AncestorsAndSelf()` で祖先から IViewRoot を探索し static Dictionary にキャッシュ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 画面の状態・共有データの置き場を作りたい | `ViewModel` 継承（Window はネスト `WindowViewModel`、Scene は `XxxViewModel` 単独ファイル） |
| 子 UI 部品から画面の VM を参照したい | `ViewBase<TViewModel>` 継承 → `ViewModel` プロパティ（自動解決） |
| ウィンドウを VM の提供元にしたい | `WindowBase, IViewRoot` を実装し `GetViewModel()` で VM を返す |
| シーンを VM の提供元にしたい | `SceneBase<TArgument, TViewModel>` 継承（IViewRoot 実装済み・VM 保持も基底任せ） |
| VM の状態変更を UI に反映したい | VM に `OnXxxAsObservable()`（遅延生成 Subject）を実装し、View 側で `.Subscribe().AddTo(this)` |
| VM の破棄を検知したい | `viewModel.OnDisposeAsObservable()` |
| ロード済みシーンの VM を外部から取得したい | `SceneManager.Instance.GetViewModel<TViewModel>(scene)`（Modules.Scene 側の連携 API。未ロードなら null） |
| View を別の画面ルート配下へ移動した / VM を差し替えた | `viewRoot.RefreshViewModel()` / `view.ClearViewModelCache()` |

## 使い方

### 新しい Window 画面を実装する手順（partial 2ファイル形式）

Client側の Window は「partial 本体 + ネスト `WindowViewModel` の2ファイル」が標準形式。

| 手順 | 内容 |
|---|---|
| 1 | `XxxWindow.cs`: `public sealed partial class XxxWindow : WindowBase, IViewRoot` を作成し、`GetViewModel()` で VM フィールドを返す |
| 2 | `XxxWindow.viewmodel.cs`: 同クラスの partial にネストクラス `public sealed class WindowViewModel : ViewModel` を定義 |
| 3 | 本体に `public void Setup(...)` → 内部 `Initialize()`（`initialized` フラグで一度だけ実行。ここで `viewModel = new WindowViewModel()` とボタン購読） |
| 4 | 子部品は `ViewBase<XxxWindow.WindowViewModel>` を継承し、ヒエラルキー上で Window の子孫に配置 |
| 5 | 呼び出し側: 生成 → `Setup(...)` → `await Open()`。閉じるのは `Close()` |

実行順序（VM 生成タイミング含む）:

```
Window:  生成 → Setup()【viewModel = new WindowViewModel() ← Client実装の責務】
         → Open(){ Prepare() → SetActive(true) → OnOpen()(開くアニメ) → Status=Opened }
         → 表示中（子 View が ViewModel プロパティ経由で共有 VM を参照・購読）
         → Close(){ OnClose()(閉じアニメ) → SetActive(false) → Status=Closed → DeleteOnClose なら GameObject 破棄 }

Scene:   SetArgument() → Initialize()(購読・View初期化) → Prepare()(通信・アセット読込) → Enter()(表示開始) → … → Leave()
         ※ VM は SceneBase<TArgument, TViewModel> のフィールド初期化子で生成済み（どの時点でも取得可能）
```

### 定型パターン（引用元）

- 最小の Window + ネスト WindowViewModel: `Client/Assets/Scripts/Client/Scene/WorldMap/Window/MenuWindow/MenuWindow.cs` / 同 `MenuWindow.viewmodel.cs`。共有状態が無くても VM は作る（子 View の解決先として必要）
- 状態と変更通知を持つ ViewModel（private Subject 遅延生成 + `OnXxxAsObservable()` + 変更メソッド内で OnNext）: `Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelNameEditWindow.viewmodel.cs`。本体側は Initialize 内で VM の通知を購読して UI を更新（同 `CitadelNameEditWindow.cs`）
- 子 View 部品（`ViewBase<XxxWindow.WindowViewModel>` 継承 + ボタン購読 + `if (ViewModel == null){ return; }` ガード）: `Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelColorSelectItemView.cs`。子部品は `ViewModel` プロパティにアクセスするだけで親 Window の VM が取れる（配線コード不要）。入力イベント → VM 操作 → VM の通知 → 各 View 更新、という一方向の流れになる
- シーン画面（`SceneBase<TArgument, TViewModel>` 継承。IViewRoot 実装も VM 保持も Client側基底 `Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs` が済ませている）: `Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapScene.cs` / `WorldMapView.cs`。シーン VM は `XxxViewModel` として単独ファイル（例: 同 `WorldMapViewModel.cs`。中身のパターンは上記 ViewModel と同じ）

## 注意点・罠

- **このモジュールに OnInitialize / OnPrepare 等のライフサイクルは無い**。VM の生成は画面ルート実装の責務: Window は `Setup()`→`Initialize()` 内で `new WindowViewModel()`、Scene は基底のフィールド初期化子。ライフサイクル本体は Scene（`Initialize→Prepare→Enter→Leave`）/ Window（`Open{Prepare→OnOpen}→Close{OnClose}`）を参照
- **祖先に IViewRoot が無い状態で `ViewModel` プロパティにアクセスすると例外**（`IViewRoot interface not found in ancestors hierarchy.`）。Instantiate 直後・親付け前のアクセスに注意
- **IViewRoot の Setup 前（VM 生成前）は null が返る**（例外にはならない）。子 View 側は `if (ViewModel == null){ return; }` ガードを入れるのが実コードの慣例（「子 View 部品」パターン参照）
- **`ViewModel.Dispose()` を自動で呼ぶ仕組みは無い**。Client側にも明示呼び出しの実例は無く、実質 GC のファイナライザ（LifetimeDisposable）任せ。Dispose 済み VM はキャッシュ無効化され再解決される
- **VM 解決キャッシュは static（アプリ全域共有）**。生存中の View を別の画面ルート配下へ付け替えた場合、古い VM がキャッシュに残るため `RefreshViewModel()` / `ClearViewModelCache()` を呼ぶこと
- **R3 を使用**（`using R3;`）。`Observable<T>` / `Subject<T>` / `.AddTo(this)`。UniRx の `IObservable<T>` ではない
- **VM は MonoBehaviour ではない**。`new` で生成し、`[SerializeField]` は使えない。UI 参照は View 側に持たせ、VM には状態とロジックのみ置く
- 命名慣例: Window の VM はネストクラス `WindowViewModel`（ファイルは `*.viewmodel.cs`）、Scene の VM は `XxxViewModel` 単独ファイル

## 関連

- [Window](Window.md) — `Modules.Window.Window`（Open/Close/Prepare/OnOpen/OnClose、PopupManager）。Client側 `WindowBase`（`Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs`）の基底で、本モジュールと組み合わせて画面を構成する
- [Scene](Scene.md) — シーン遷移とライフサイクル（Initialize→Prepare→Enter→Leave）。`SceneManagerBase.GetViewModel<T>` で本モジュールと連携
- [UI](UI.md) — `UIButton.OnClick` / `OnClickAsObservable` 等、View 実装で組み合わせる UI 部品
- [R3Extension](R3Extension.md) — R3 関連の補助
- [Extensions/Core.md](../Extensions/Core.md) — `LifetimeDisposable`（ViewModel の基底）

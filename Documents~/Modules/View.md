# View

> **namespace**: `Modules.View`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/View/`（View.cs / ViewBase.cs / ViewModel.cs の3ファイルのみ）
> **Client側使用**: 約183ファイル（2026-07時点）
> **依存**: R3（UniRxではない） / Unity.Linq（LINQtoGameObject: `AncestorsAndSelf`） / Extensions（`LifetimeDisposable`, `UnityUtility`）

## 概要

MVVM 風の View–ViewModel 接続基盤。画面の状態を `ViewModel`（非MonoBehaviour の POCO）に集約し、画面ルート（Window / Scene 等）が `IViewRoot` として VM を1個保持、配下の子 UI 部品（`ViewBase<T>` 継承）はヒエラルキーを親方向に遡って同じ VM を自動解決・共有する。
このモジュール自体は接続機構のみで、画面ライフサイクル（初期化・表示・破棄）は持たない。ライフサイクルは Modules.Scene（`Initialize → Prepare → Enter → Leave`）と Modules.Window（`Open{Prepare → OnOpen} → Close{OnClose}`）側が担う。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 画面の状態・共有データの置き場を作りたい | `ViewModel` 継承（Window はネスト `WindowViewModel`、Scene は `XxxViewModel` 単独ファイル） |
| 子 UI 部品から画面の VM を参照したい | `ViewBase<TViewModel>` 継承 → `ViewModel` プロパティ（自動解決） |
| ウィンドウを VM の提供元にしたい | `WindowBase, IViewRoot` を実装し `GetViewModel()` で VM を返す |
| シーンを VM の提供元にしたい | `SceneBase<TArgument, TViewModel>` 継承（IViewRoot 実装済み・VM 保持も基底任せ） |
| VM の状態変更を UI に反映したい | VM に `OnXxxAsObservable()`（遅延生成 Subject）を実装し、View 側で `.Subscribe().AddTo(this)` |
| VM の破棄を検知したい | `viewModel.OnDisposeAsObservable()` |
| ロード済みシーンの VM を外部から取得したい | `SceneManager.Instance.GetViewModel<TViewModel>(scene)`（Modules.Scene 側の連携 API） |
| View を別の画面ルート配下へ移動した / VM を差し替えた | `viewRoot.RefreshViewModel()` / `view.ClearViewModelCache()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `ViewModel` | abstract class（非MonoBehaviour、`LifetimeDisposable` 継承） | 画面状態の基底。`OnDisposeAsObservable()` で破棄通知 |
| `ViewBase<TViewModel>` | abstract MonoBehaviour（`IView<TViewModel>` 実装） | 子 View 部品の基底。`ViewModel` プロパティで祖先 IViewRoot の VM を遅延解決・キャッシュ |
| `IViewRoot` | interface | VM の提供者。`GetViewModel()` 1メソッドのみ。Client側では WindowBase 派生・SceneBase・SidePanelView 派生等が実装 |
| `IView<TViewModel>` | interface（メンバーなしマーカー） | `ViewExtensions.GetViewModel` の適用対象。通常は ViewBase 経由で自動的に付く |
| `ViewExtensions` | static class | VM 解決の実体。`gameObject.AncestorsAndSelf()` で祖先から IViewRoot を探索し、static Dictionary にキャッシュ（20フレーム毎に死んだエントリを掃除） |

## 使い方(実例)

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

### 実例1: 最小の Window + WindowViewModel

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/Window/MenuWindow/MenuWindow.cs
public sealed partial class MenuWindow : WindowBase, IViewRoot
{
    [SerializeField]
    private UIButton closeButton = null;

    private WindowViewModel viewModel = null;

    private bool initialized = false;

    public ViewModel GetViewModel()
    {
        return viewModel;
    }

    private void Initialize()
    {
        if (initialized){ return; }

        viewModel = new WindowViewModel();

        closeButton.OnClick(() => Close().Forget());

        initialized = true;
    }

    public void Setup()
    {
        Initialize();
    }

    protected override UniTask Prepare()
    {
        return UniTask.CompletedTask;
    }
}
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/Window/MenuWindow/MenuWindow.viewmodel.cs
public sealed partial class MenuWindow
{
    public sealed class WindowViewModel : ViewModel
    {
        // 共有状態が無くても VM は作る（子 View の解決先として必要）.
    }
}
```

### 実例2: 状態と変更通知を持つ ViewModel

VM 側は「private Subject 遅延生成 + `OnXxxAsObservable()` + 変更メソッド内で OnNext」パターン。

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelNameEditWindow.viewmodel.cs（抜粋）
public sealed partial class CitadelNameEditWindow
{
    public sealed class WindowViewModel : ViewModel
    {
        private Subject<Unit> onColorChanged = null;

        public CitadelTagColorType SelectedColor { get; private set; }

        public void SetSelectedColor(CitadelTagColorType color)
        {
            if (SelectedColor == color){ return; }

            SelectedColor = color;

            if (onColorChanged != null)
            {
                onColorChanged.OnNext(Unit.Default);
            }
        }

        public Observable<Unit> OnColorChangedAsObservable()
        {
            return onColorChanged ?? (onColorChanged = new Subject<Unit>());
        }
    }
}
```

Window 本体側は Initialize 内で VM の通知を購読して UI を更新する。

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelNameEditWindow.cs（Initialize 抜粋）
windowViewModel.OnColorChangedAsObservable()
    .Subscribe(_ => UpdateColorSelection())
    .AddTo(this);

acceptButton.OnClick(() => OnAccept().Forget());
```

### 実例3: 子 View 部品（ViewBase + ボタン購読）

子部品は `ViewModel` プロパティにアクセスするだけで親 Window の VM が取れる（配線コード不要）。入力イベント → VM 操作 → VM の通知 → 各 View 更新、という一方向の流れになる。

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelColorSelectItemView.cs（抜粋）
public sealed class CitadelColorSelectItemView : ViewBase<CitadelNameEditWindow.WindowViewModel>
{
    [SerializeField]
    private UIButton button = null;

    private bool initialized = false;

    private void Initialize()
    {
        if (initialized){ return; }

        button.OnClickAsObservable()
            .Subscribe(_ =>
            {
                if (ViewModel == null){ return; }

                // 親ウィンドウの VM を直接操作（結果は VM の通知経由で全 View に反映される）.
                ViewModel.SetSelectedColor(ColorType);
            })
            .AddTo(this);

        initialized = true;
    }
}
```

### 実例4: シーン画面（SceneBase&lt;TArgument, TViewModel&gt;）

シーンでは IViewRoot 実装も VM 保持も Client 側基底クラスが済ませている。

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs（抜粋）
public abstract class SceneBase<TArgument, TViewModel> : SceneBase<TArgument>, IViewRoot
    where TArgument : SceneArgument, new() where TViewModel : ViewModel, new()
{
    protected TViewModel viewModel = new TViewModel();

    public ViewModel GetViewModel()
    {
        return viewModel;
    }
}
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapScene.cs（宣言のみ抜粋）
public sealed class WorldMapScene : SceneBase<WorldMapSceneArgument, WorldMapViewModel>

// 引用元: Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapView.cs（宣言のみ抜粋）
public sealed class WorldMapView : ViewBase<WorldMapViewModel>
```

シーン VM は `XxxViewModel` として単独ファイル（例: `Client/Assets/Scripts/Client/Scene/WorldMap/WorldMapViewModel.cs`）。中身のパターンは実例2と同じ。

## API(主要公開メンバー)

### ViewModel（abstract / 非MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `Dispose()` | 破棄。`OnDispose()` → `OnDisposeAsObservable` 発火 → `Disposable`（CompositeDisposable）解放 |
| `OnDisposeAsObservable() : Observable<Unit>` | 破棄通知（ViewBase はこれを購読してローカルキャッシュを自動 null 化） |
| `protected virtual OnDispose()` | 派生クラスのクリーンアップフック |
| `IsDisposed : bool`（LifetimeDisposable 由来） | 破棄済みか。ViewExtensions のキャッシュ無効化判定にも使用 |
| `Disposable : CompositeDisposable`（LifetimeDisposable 由来） | VM 内部の購読の `AddTo` 先 |

### ViewBase&lt;TViewModel&gt;（abstract MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `protected ViewModel : TViewModel` | 遅延解決プロパティ。初アクセス時に祖先 IViewRoot から取得してローカルキャッシュ。VM が Dispose されると自動で null に戻り再解決可能 |
| `ClearViewModelCache()` | ローカルキャッシュ破棄。VM 差し替え・親付け替え時に使用 |

### IViewRoot / IView&lt;TViewModel&gt;

| メンバー | 説明 |
|---|---|
| `IViewRoot.GetViewModel() : ViewModel` | VM を返す（実装側は VM フィールドを返すだけ）。任意の MonoBehaviour に実装可（Window でなくてもよい） |
| `IView<TViewModel>` | メンバーなし。ViewExtensions の拡張メソッドを受けるためのマーカー |

### ViewExtensions（static）

| メンバー | 説明 |
|---|---|
| `GetViewModel<TViewModel>(this IView<TViewModel>) : TViewModel` | `AncestorsAndSelf()` で最も近い祖先の IViewRoot を探索し VM 取得。結果は static Dictionary にキャッシュ。**IViewRoot が見つからないと例外** |
| `RefreshViewModel(this IViewRoot)` | その Root の VM を参照している全キャッシュエントリを無効化 |
| `RefreshViewModel<TViewModel>(this IView<TViewModel>)` | その View 1個分のキャッシュエントリを無効化 |

### 連携 API（Modules.Scene 側 / SceneManagerBase.View.cs）

| メンバー | 説明 |
|---|---|
| `SceneManagerBase.GetViewModel<TViewModel>(TScenes scene)` | ロード済みの指定シーン（IViewRoot 実装）の VM を取得。未ロードなら null |
| `SceneManagerBase.GetViewModel<TViewModel>()` | ロード済み全シーンから型が一致する最初の VM を検索 |

## 注意点・罠

- **このモジュールに OnInitialize / OnPrepare 等のライフサイクルは無い**。VM の生成は画面ルート実装の責務: Window は `Setup()`→`Initialize()` 内で `new WindowViewModel()`、Scene は基底のフィールド初期化子。ライフサイクル本体は Scene（`Initialize→Prepare→Enter→Leave`）/ Window（`Open{Prepare→OnOpen}→Close{OnClose}`）を参照
- **祖先に IViewRoot が無い状態で `ViewModel` プロパティにアクセスすると例外**（`IViewRoot interface not found in ancestors hierarchy.`）。Instantiate 直後・親付け前のアクセスに注意
- **IViewRoot の Setup 前（VM 生成前）は null が返る**（例外にはならない）。子 View 側は `if (ViewModel == null){ return; }` ガードを入れるのが実コードの慣例（実例3参照）
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

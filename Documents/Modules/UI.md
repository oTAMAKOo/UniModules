# UI

> **namespace**: `Modules.UI`（直下） / `Modules.UI.Extension` / `Modules.UI.VirtualScroll` / `Modules.UI.SpriteLoader` / `Modules.UI.Layout` / `Modules.UI.Focus` / `Modules.UI.Reactive` / `Modules.UI.ScreenRotation` / `Modules.UI.SpriteNumber` / `Modules.UI.TextHyperlink` / `Modules.UI.DummyContent` / `Modules.UI.Particle` / `Modules.Devkit.UI`（エディタ専用）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/UI/`
> **Client側使用**: 約90ファイル（2026-07時点）
> **依存**: R3（UniRxではない） / UniTask / DOTween / TextMeshPro / Extensions / Modules.Cache / Modules.ExternalAssets

## 概要

uGUI 標準コンポーネントのラッパー（Extension）と、仮想スクロールリスト・スプライトローダー・レイアウト補助・チュートリアル用フォーカス等の UI 基盤群。
イベントは全て R3 の `Observable<T>` を `OnXxxAsObservable()` で公開する共通パターン。

**最重要**: `Modules.UI.Extension` の各クラス（`UIButton` 等）は abstract。Client 側は
`Client/Assets/Scripts/Client/Core/UI/` にある **`Dominion.Client` namespace の具象クラス**（`Dominion.Client.UIButton` 等）を使う。
ゲームコードは `Dominion.Client.*` namespace 内にあるため using 不要でそのまま `UIButton` と書ける（`using Modules.UI.Extension` を書くことはほぼ無い）。
`VirtualScroll` / `ProgressBar` 等の Modules.UI 直下クラスは `using Modules.UI;` で使う。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ボタンクリック処理 | `UIButton.OnClick(Action)` / `OnClickAsObservable()` |
| ボタン長押し | `UIButton.OnLongPressAsObservable()` + `SetLongPressDuration(float)` |
| テキスト表示（TMP） | `UIText.text`（値は `TextData.Get` から） |
| 画像・色・アルファ変更 | `UIImage.sprite` / `.color` / `.alpha` |
| **リストを大量表示（※自作禁止）** | `VirtualScroll<T>` + `VirtualScrollItem<T>` |
| **グリッド状リスト表示** | `GridVirtualScroll<T>` + `GridVirtualScrollItem<T, TComponent>` |
| リストの特定位置へスクロール | `VirtualScroll<T>.ScrollToItem(index, ScrollTo.Center, duration)` |
| リスト更新時にスクロール位置維持 | `VirtualScroll<T>.UpdateContents(keepScrollPosition: true)` |
| 外部アセットの Sprite を Image に表示 | `ImageSpriteLoader.SetSprite(loadPath)` |
| SpriteAtlas 内の Sprite を Image に表示 | `ImageAtlasSpriteLoader.SetSprite(atlasLoadPath, spriteName)` |
| HP バー等のゲージ | `ProgressBar.FillAmount` |
| チュートリアルで UI を最前面フォーカス | `FocusManager.AddFocus(focusId)` + `FocusTarget`（対象に付与） |
| スクロール位置を一時固定 / 先頭に戻す | `UIScrollView.LockPosition()` / `ResetPosition()` |
| テキスト等の自動サイズに上限を付ける | `ContentSizeFitterMaxWidth` / `ContentSizeFitterMaxHeight` |
| 別 RectTransform の preferred サイズに追従 | `PreferredSizeCopy.SetCopySource(rt)` |
| アスペクト比固定の RectTransform | `FixedAspectRectTransform` |
| 子階層の Graphic を一括で色変更 | `GraphicGroup.ColorTint` |
| 描画なしのタッチ判定（ブロッカー） | `GraphicCast` |
| 頂点カラーでグラデーション | `ColorGradation` |
| 画像の左右/上下反転（scale=-1 を使わず） | `FlipGraphic` |
| UI を発光させる | `GlowGraphic.emissionColor` |
| CanvasGroup 等のフェード制御 | `FadeGraphic`（非 MonoBehaviour） |
| スワイプ検知 | `UISwipeHandler.OnSwipeLeftAsObservable()` 等 |
| ドラッグ移動（慣性付き） | `DragObject`（DragTarget.cs） |
| ドラッグイベント購読のみ | `UIDragAndDropHandler` |
| アイテムに吸着するスクロール | `SnapScrollRect` |
| 数字をスプライトで表示（ダメージ表記等） | `SpriteNumberImage.Set(string)` |
| TMP テキスト内のリンククリック | `HyperlinkClickEventTrigger.OnHyperlinkActionAsObservable()` |
| ParticleSystem を uGUI 上に描画 | `UIParticleSystem` |
| ボタン無効時に文字色/画像を連動変更 | `ButtonInteractableTextColor` / `ButtonInteractableImageSprite` |
| 画面の回転（90度/180度） | `RotationManager.RotateType` + `RotationRoot` |
| Canvas にカメラを自動割当 | `UICanvasCamera`（カメラ側に付与、`UICanvas` が自動検索） |
| ワールド座標対象に UI を追従 | `ScreenPositionTracker.SetTarget(transform, offset)` |

## uGUI 標準 → 本プロジェクトでの対応表

新規 UI 実装時は uGUI 標準コンポーネントを直接フィールドに持たず、以下を使う。
具象クラスの実体: `Client/Assets/Scripts/Client/Core/UI/*.cs`（namespace `Dominion.Client`）。

| uGUI 標準 | 使うべきもの | 追加機能 |
|---|---|---|
| `Button` | `UIButton`（Dominion.Client） | SE 自動再生（`UIButtonSe`）・`OnClick`/長押し Observable・Navigation 無効化 |
| `TextMeshProUGUI` | `UIText`（Dominion.Client） | `text` プロパティ（`SetText` 経由） |
| `Image` | `UIImage`（Dominion.Client） | `sprite`/`color`/`alpha`・エディタ用ダミー画像機構 |
| `RawImage` | `UIRawImage`（Dominion.Client） | `texture`・空テクスチャ時自動非表示（ダミー登録時） |
| `ScrollRect` | `UIScrollView`（Dominion.Client）または `VirtualScroll<T>` | 位置ロック・自動スクロール無効化（`autoScrollDisable`）・横スクロールのホイール対応 |
| `Slider` | `UISlider`（Dominion.Client） | `value` |
| `Toggle` | `UIToggle`（Dominion.Client） | `isOn`・`OnChangeAsObservable()` |
| `TMP_InputField` | `UIInputField`（Dominion.Client） | `text`・`OnEndEditAsObservable()` |
| `TMP_Dropdown` | `UIDropdown`（Dominion.Client） | `OnChangeAsObservable()` |
| `Scrollbar` | `UIScrollbar`（Dominion.Client） | ラッパーのみ |
| `Canvas`（ルート） | `UICanvas`（Dominion.Client） | `UICanvasCamera` の自動割当・CanvasScaler 自動設定（AppConfig の基準解像度） |
| `Image`(type=Filled) でゲージ自作 | `ProgressBar` | Filled/Resize/Sprites の3モード・ステップ対応 |
| 透明 `Image` でタッチブロック | `GraphicCast` | 頂点を生成しない（描画コストゼロ） |
| `LayoutGroup` で数百件のリスト | **禁止** → `VirtualScroll<T>` | セル使い回しで生成数を画面内+α に抑える |

## 主要クラス

### Extension（uGUI ラッパー基底） — `Modules.UI.Extension`

| クラス | 種別 | 役割 |
|---|---|---|
| `UIComponentBehaviour` / `UIComponent<T>` | abstract MonoBehaviour | ラッパー共通基底。`component` プロパティで対象コンポーネントを遅延取得 |
| `UIButton` | abstract（`UIComponent<Button>`） | クリック/押下/長押し/キャンセルの Observable と Action 登録 |
| `UIText` | abstract（`UIComponent<TextMeshProUGUI>`） | `text` プロパティ |
| `UIImage` | abstract partial（`UIComponent<Image>`） | `sprite`/`color`/`alpha`・ダミーアセット（editor partial） |
| `UIRawImage` | abstract partial（`UIComponent<RawImage>`） | `texture`・空テクスチャ自動非表示 |
| `UIScrollView` | abstract（`UIComponent<ScrollRect>`） | `LockPosition`/`UnLockPosition`/`ResetPosition`/`ContentRoot` |
| `UICanvas` | abstract（`UIComponent<Canvas>`） | カメラ自動割当＋CanvasScaler 自動設定（`ReferenceResolution` は派生で定義） |
| `UICanvasCamera` | sealed MonoBehaviour | `UICanvas` の割当対象カメラのマーカー。`static GetCanvasCameraForLayer(layerMask)` |
| `UISlider` / `UIToggle` / `UIInputField` / `UIDropdown` / `UIScrollbar` | abstract | 各標準コンポーネントのラッパー |

### VirtualScroll（仮想スクロール） — `Modules.UI`（enum は `Modules.UI.VirtualScroll`）

| クラス | 種別 | 役割 |
|---|---|---|
| `VirtualScroll<T>` | abstract UIBehaviour | 単列リスト本体。セルを使い回して大量データを表示 |
| `VirtualScrollItem<T>` | abstract MonoBehaviour | リストアイテム基底。`UpdateContents(content, token)` を実装する |
| `GridVirtualScroll<T>` | abstract（`VirtualScroll<GridElement>`） | グリッド表示。1行=GridElement に分割して VirtualScroll に流す |
| `GridVirtualScrollItem<T, TComponent>` | abstract（`VirtualScrollItem<GridElement>`） | グリッド1行分。`elementPrefab` を行内に複数生成 |
| `IVirtualScroll` / `IVirtualScrollItem` | interface | 非ジェネリック参照用（`ScrollRect` / `Index`） |
| `IVirtualScrollEventHook` | interface | VirtualScroll と同一 GameObject に付けると初期化時に `OnInitialize(IVirtualScroll)` が呼ばれる拡張点 |
| enum `Direction` / `ScrollType` / `ScrollTo` / `ContentFit` | enum | Vertical・Horizontal / Limited・Loop / First・Center・Last / 内容寄せ |

### SpriteLoader — `Modules.UI.SpriteLoader`

| クラス | 種別 | 役割 |
|---|---|---|
| `ImageSpriteLoader` | sealed MonoBehaviour（要 Image） | `ExternalAsset.LoadAsset<Sprite>` でロードして Image に設定＋キャッシュ。ロード完了まで/失敗時は非表示 |
| `ImageAtlasSpriteLoader` | sealed MonoBehaviour（要 Image） | `ExternalAsset.LoadAsset<SpriteAtlas>` からスプライト名指定で設定＋キャッシュ |

### Layout — `Modules.UI.Layout`

| クラス | 種別 | 役割 |
|---|---|---|
| `ContentSizeFitterMaxHeight` | sealed UIBehaviour, ILayoutElement（要 ContentSizeFitter） | preferredHeight に上限。超過時は FitMode を切替えて maxHeight に固定 |
| `ContentSizeFitterMaxWidth` | 同上 | 幅版 |
| `PreferredSizeCopy` | sealed LayoutElement | `copySource` の preferred サイズを自サイズに反映（min/max/padding/flexible 指定可） |

### Focus（チュートリアル等の最前面表示） — `Modules.UI.Focus`

| クラス | 種別 | 役割 |
|---|---|---|
| `FocusManager` | Singleton（`Extensions.Singleton<T>`） | focusId の集合を管理。`SetFocusCanvas` で基準 Canvas 設定 |
| `FocusTarget` | sealed MonoBehaviour | フォーカス対象に付与。フォーカス時に Canvas+GraphicRaycaster を自動付与し sortingOrder を基準 Canvas+1 に |

### Modules.UI 直下（単機能コンポーネント）

| クラス | 種別 | 役割 |
|---|---|---|
| `ButtonEventTrigger` | sealed MonoBehaviour | 押下/解放/長押し/キャンセル検知。`ButtonExtensions`（`Extensions`）経由で自動付与される内部部品 |
| `ProgressBar` | sealed MonoBehaviour | ゲージ。FillMode: Filled（Image.fillAmount）/ Resize（幅変更）/ Sprites（コマ切替）＋ステップ刻み |
| `FadeGraphic` | 通常クラス（非 MonoBehaviour） | `FadeIn`/`FadeOut`（UniTask）。alpha 変化は Observable で通知し利用側が反映 |
| `GraphicCast` | sealed Graphic | 頂点なしのレイキャスト専用 Graphic |
| `GraphicGroup` | sealed Graphic | 子階層 Graphic へ `ColorTint` を乗算適用（`ignoreTargets` で除外） |
| `ColorGradation` | sealed BaseMeshEffect | 頂点カラーグラデーション（Simple 4色 / Gradient 多段） |
| `FlipGraphic` | MonoBehaviour, IMeshModifier | メッシュ反転（Horizontal/Vertical） |
| `GlowGraphic` | sealed MonoBehaviour | `Custom/uGUI/Glow` シェーダーで HDR 発光 |
| `FitForScreen` | sealed MonoBehaviour | ルート Canvas サイズへ全面フィット（親スケール打消し） |
| `FixedAspectRectTransform` | sealed LayoutElement | `baseSize` のアスペクト比に固定（`matchWidth` で基準軸選択） |
| `UIAutoScaler` | sealed UIBehaviour | `target` の RectTransform サイズ変化に合わせ localScale を調整 |
| `ScreenPositionTracker` | sealed UIBehaviour | 対象 Transform のスクリーン位置に毎フレーム追従 |
| `DragObject`（**ファイル名は DragTarget.cs**） | sealed UIBehaviour | ドラッグ移動＋慣性＋Canvas 内制限。`OnBeginDrag/OnDrag/OnEndDragAsObservable` |
| `UIDragAndDropHandler` | sealed MonoBehaviour | ドラッグ開始/中/終了の位置(Vector2)を Observable 通知 |
| `UISwipeHandler` | sealed MonoBehaviour | 4方向スワイプ検知（時間・距離閾値付き） |
| `SnapScrollRect` | sealed ScrollRect 派生 | ドラッグ終了時に最寄りの登録ターゲットへ吸着。`OnSnapAsObservable` |
| `FullWidthCharacterInputField` | sealed MonoBehaviour | 旧 uGUI InputField の全角 IME 入力バグ（文字倍加）対策 |

### その他サブ namespace

| クラス | namespace | 種別 | 役割 |
|---|---|---|---|
| `SpriteNumberBase<T>` / `SpriteNumberImage` | `Modules.UI.SpriteNumber` | abstract / sealed MonoBehaviour | 文字→Sprite 対応表で数字列を並べて表示。桁毎アニメ対応 |
| `SpriteNumberAnimation` | `Modules.UI.SpriteNumber` | sealed MonoBehaviour | アニメ完了通知（AnimationEvent から `CompleteAnimation()` を呼ぶ） |
| `HyperlinkEventHandler` | `Modules.UI.TextHyperlink` | abstract UIBehaviour | TMP の `<link="id">` 取得基盤。`OnHyperlinkActionAsObservable()` で link ID 通知 |
| `HyperlinkClickEventTrigger` / `HyperlinkLongPressEventTrigger` | `Modules.UI.TextHyperlink` | sealed | クリック / 長押しでリンク検知 |
| `ButtonInteractableTextColor` / `ButtonInteractableImageSprite` | `Modules.UI.Reactive` | sealed MonoBehaviour | `UIButton.interactable` を監視し文字色 / 画像+色を自動切替（**ファイル名 ButtonInteractablemageSprite.cs は typo**） |
| `RotationManager` / `RotationRoot` | `Modules.UI.ScreenRotation` | Singleton / sealed MonoBehaviour | 画面回転（None/±90度/180度）。RotationRoot が sizeDelta 入替＋回転 |
| `DummySprite` / `DummyText` | `Modules.UI.DummyContent` | sealed partial MonoBehaviour | エディタ専用ダミー表示（ビルド非含有）。実行時は空なら自動非表示 |
| `UIParticleSystem` | `Modules.UI.Particle` | sealed MaskableGraphic | ParticleSystem を uGUI メッシュとして描画（マスク対応） |

### Editor 専用（`Editor/` サブフォルダ）

全て CustomEditor（インスペクタ表示調整のみ、ランタイム機能なし）:
`UIComponentInspector` / `UIImageInspector` / `UIRawImageInspector` / `ColorGradationInspector` / `DragTargetInspector` / `DummySpriteInspector` / `DummyTextInspector` / `FocusTargetInspector`（FocusId 表示・コピー） / `GraphicGroupInspector` / `ContentSizeFitterMaxHeight(Width)Inspector` / `PreferredSizeCopyInspector` / `ProgressBarInspector` / `RotationRootInspector` / `SnapScrollRectInspector` / `UIParticleSystemInspector`。
例外: `FixSpriteAtlasSource`（`Modules.Devkit.UI`, static）は SpriteAtlas のプラットフォーム設定一括修正ツール。

## 使い方(実例)

### 1. UIButton / UIText / UIImage の基本（SerializeField + OnClick）

```csharp
// 引用: Client/Assets/Scripts/Client/Feature/Battle/BattleHistoryWindow/BattleHistoryItemView.cs（抜粋）
[SerializeField]
private UIText resultBadgeText = null;
[SerializeField]
private UIImage resultBadgeImage = null;
[SerializeField]
private UIButton moveButton = null;

public override UniTask Initialize(CancellationToken cancelToken)
{
    moveButton.OnClick(() => OnMoveButtonClick());   // AddTo(this) 済みなので破棄処理不要.

    return UniTask.CompletedTask;
}

private void UpdateResultBadge(WorldMapBattleRecordData content)
{
    var isVictory = content.Result != null && content.Result.ResultType == BattleResultType.Victory;

    resultBadgeText.text = isVictory
        ? TextData.Get(TextData.Window.BattleHistoryWindow_ResultVictory)
        : TextData.Get(TextData.Window.BattleHistoryWindow_ResultDefeat);

    resultBadgeImage.color = isVictory ? victoryBadgeColor : defeatBadgeColor;
}
```

Observable で購読する場合（ViewModel へ流す等）:

```csharp
// 引用: Client/Assets/Scripts/Client/Feature/Citadel/CitadelNameEditWindow/CitadelColorSelectItemView.cs（抜粋）
button.OnClickAsObservable()
    .Subscribe(_ =>
    {
        if (ViewModel == null){ return; }

        ViewModel.SetSelectedColor(ColorType);
    })
    .AddTo(this);
```

### 2. VirtualScroll（単列リスト）— 3点セットで作る

構成: ①リスト View（`VirtualScroll<T>` 派生・中身は空でよい） ②アイテム View（`VirtualScrollItem<T>` 派生） ③呼び出し側（`SetContents` → `UpdateContents`）。
itemPrefab（アイテム View 付き）・ScrollRect 等は Inspector で設定する。

```csharp
// ① 引用: Client/Assets/Scripts/Client/Feature/Battle/BattleHistoryWindow/BattleHistoryListView.cs（全文）
using Modules.UI;
using Dominion.Client.Data;

namespace Dominion.Client.Feature.Battle.BattleHistory
{
    /// <summary> 戦闘履歴のリスト表示 (中身の表示は BattleHistoryItemView が担当) </summary>
    public sealed class BattleHistoryListView : VirtualScroll<WorldMapBattleRecordData>
    {
    }
}
```

```csharp
// ② 引用: Client/Assets/Scripts/Client/Feature/Battle/BattleHistoryWindow/BattleHistoryItemView.cs（抜粋）
public sealed class BattleHistoryItemView : VirtualScrollItem<WorldMapBattleRecordData>
{
    // Initialize はセル生成時に1回だけ呼ばれる（ボタン購読等はここで）.
    public override UniTask Initialize(CancellationToken cancelToken)
    {
        moveButton.OnClick(() => OnMoveButtonClick());

        return UniTask.CompletedTask;
    }

    // UpdateContents はセルが使い回される度に呼ばれる（表示状態は毎回全て設定する）.
    public override UniTask UpdateContents(WorldMapBattleRecordData content, CancellationToken cancelToken)
    {
        UpdateResultBadge(content);
        UpdateTile(content);

        return UniTask.CompletedTask;
    }

    private void OnMoveButtonClick()
    {
        if (Content == null){ return; }    // Content = 現在このセルに割り当てられたデータ.

        viewModel.RequestMove(Content);
    }
}
```

```csharp
// ③ 引用: Client/Assets/Scripts/Client/Feature/Battle/BattleHistoryWindow/BattleHistoryWindow.cs（抜粋）
var records = windowViewModel.GetDisplayRecords();

listView.SetContents(records);

await listView.UpdateContents();
```

### 3. GridVirtualScroll（グリッドリスト）

行 View は `GridVirtualScrollItem<データ型, 要素View型>` を継承し、要素1個分の更新だけ書く。
`elementPrefab` / `elementParent`（行内の親）は Inspector で設定。列数は Inspector の `lineElementCount` か `SetLineElementCount()`。

```csharp
// 引用: Client/Assets/Scripts/Client/Feature/Storage/StorageWindow/ScrollView/StorageGridScrollItemView.cs（抜粋）
public sealed class StorageGridScrollItemView : GridVirtualScrollItem<StorageData, StorageGridScrollElementView>
{
    protected override void OnCreateElement(StorageGridScrollElementView element)
    {
        element.Initialize();
    }

    protected override async UniTask UpdateContents(int index, StorageData content, StorageGridScrollElementView element, CancellationToken cancelToken)
    {
        await element.UpdateContents(content);
    }
}
```

```csharp
// 引用: Client/Assets/Scripts/Client/Feature/Storage/StorageWindow/TabContents/TabContentView.cs（抜粋）
scrollView.SetContents(ViewModel.Contents);           // GridVirtualScroll<T>.SetContents が行分割してくれる.

await scrollView.UpdateContents(keepScrollPosition);  // タブ切替時などは true で位置維持.
```

### 4. SpriteLoader（外部アセットの画像表示）

```csharp
// 引用: Client/Assets/Scripts/Client/Scene/Character/CharacterScene.cs（抜粋）
[SerializeField]
private ImageSpriteLoader backgroundLoader = null;

await backgroundLoader.SetSprite(BackgroundSpriteLoadPath);
```

```csharp
// 引用: Client/Assets/Scripts/Client/Module/Orb/OrbSlotView.cs（抜粋）
var orbRecord = OrbMaster.GetRecordByOrbId(orb.OrbId);

await orbImageLoader.SetSprite(OrbAtlasLoadPath, orbRecord.SpriteName);
```

### 5. Focus（チュートリアルの最前面フォーカス）

`FocusTarget` を対象 UI に付与し（FocusId は Inspector で GUID 自動生成）、コードからは focusId で制御する。

```csharp
// 引用: Client/Assets/Scripts/Client/Tutorial/TutorialControllerBase.cs（抜粋）
focusManager.SetFocusCanvas(tutorialObject.Canvas);   // 基準Canvas（この上に表示される）を先に設定.

public void SetFocus(string focusId)
{
    var focusManager = FocusManager.Instance;

    if (string.IsNullOrEmpty(focusId)){ return; }

    focusManager.AddFocus(focusId);
}

// 全解除.
FocusManager.Instance.RemoveAllFocus();
```

### 6. ProgressBar（ゲージ）

```csharp
// 引用: Client/Assets/Scripts/Client/Scene/Battle/View/BattleUnit/Parts/BattleUnitGaugeView.cs（抜粋）
[SerializeField]
private ProgressBar gauge = null;

private void ApplyDisplay()
{
    if (gauge != null)
    {
        gauge.FillAmount = 0 < maxValue ? Mathf.Clamp01((float)displayValue / maxValue) : 0f;
    }
}
```

## API(主要公開メンバー)

### UIButton（`Modules.UI.Extension` / 具象は `Dominion.Client.UIButton`）

| メンバー | 説明 |
|---|---|
| `Button Button` / `bool interactable` | 内包する uGUI Button / 有効状態 |
| `void Initialize()` | 初期化（OnEnable で自動実行、二重実行ガード付き） |
| `Observable<Unit> OnClickAsObservable()` | クリック |
| `Observable<Unit> OnPressAsObservable()` | 押下した瞬間 |
| `Observable<float> OnReleaseAsObservable()` | 離した瞬間（値=押下時間秒） |
| `Observable<Unit> OnLongPressAsObservable()` | 長押し成立（デフォルト0.5秒） |
| `Observable<float> OnLongPressReleaseAsObservable()` | 長押し後に離した（値=押下時間秒） |
| `Observable<Unit> OnCancelAsObservable()` | 領域外へ出た等で押下キャンセル |
| `void SetLongPressDuration(float duration)` | 長押し判定時間変更 |
| `IDisposable OnClick(Action)` / `OnPress` / `OnRelease` / `OnLongPress` / `OnLongPressRelease` / `OnCancel` | Subscribe+`AddTo(this)` 込みの簡易登録 |
| （Dominion.Client 側）`UIText UIText` / `string text` / `UIButtonSe Se` | ボタンラベル連携 / クリックSE種別（button_positive 等） |

### UIText / UIImage / UIRawImage / UIScrollView / UICanvas（`Modules.UI.Extension`）

| クラス.メンバー | 説明 |
|---|---|
| `UIText.Text` / `UIText.text` | TextMeshProUGUI 本体 / テキスト設定（SetText 経由） |
| `UIImage.Image` / `.sprite` / `.color` / `.alpha` | Image 本体 / スプライト / 色 / アルファのみ変更 |
| `UIRawImage.RawImage` / `.texture` | RawImage 本体 / テクスチャ（ダミー登録時、null なら自動非表示） |
| `UIScrollView.ScrollRect` / `.ContentRoot` | ScrollRect 本体 / content の GameObject |
| `UIScrollView.LockPosition()` / `UnLockPosition()` | 毎フレーム位置を固定/解除（更新中のガタつき防止） |
| `UIScrollView.ResetPosition()` | 先頭（上端/左端）に戻す |
| `UICanvas.Canvas` / `.CanvasCameraModify` / `.CanvasScalerModify` / `ModifyCanvasScaler()` | Canvas 本体 / カメラ・スケーラー自動調整の有効切替 / スケーラー再適用 |
| `UICanvasCamera.GetCanvasCameraForLayer(int layerMask)` | static。layerMask に一致する UICanvasCamera 付きカメラを取得 |
| `UIToggle.isOn` / `OnChangeAsObservable()` | トグル状態 / 変更通知（Observable\<bool\>） |
| `UIInputField.text` / `OnEndEditAsObservable()` | 入力文字列 / 編集確定通知（Observable\<string\>） |
| `UIDropdown.Dropdown` / `OnChangeAsObservable()` | TMP_Dropdown 本体 / 選択 index 通知（Observable\<int\>） |
| `UISlider.value` / `UIScrollbar.Scrollbar` | 値 / 本体 |

### VirtualScroll\<T\>（`Modules.UI`）

Inspector 設定: `scrollType`（Limited/Loop）, `direction`（Vertical/Horizontal）, `itemPrefab`, `scrollRect`, `contentFit`, `additionalGeneration`（画面外予備セル数 2〜10）, `edgeSpacing`, `itemSpacing`, `hitBoxEnable`。

| メンバー | 説明 |
|---|---|
| `void SetContents(IEnumerable<T> contents)` | 表示データ設定（表示はまだ更新されない） |
| `UniTask UpdateContents(bool keepScrollPosition = false)` | セル生成＋表示更新。**これを await して初めて表示される** |
| `IReadOnlyList<T> Contents` | 設定済みデータ |
| `IEnumerable<VirtualScrollItem<T>> ListItems` / `bool HasListItem` | 管理下のセル（使い回し分のみ、データ件数分ではない） |
| `float ScrollPosition` | 現在のスクロール位置（anchoredPosition ベース。get/set） |
| `void ScrollToItem(int index, ScrollTo to)` | 即時スクロール（First/Center/Last 寄せ） |
| `UniTask ScrollToItem(int index, ScrollTo to, float duration, Ease ease = Ease.Unset)` | DOTween アニメ付きスクロール |
| `void Cancel()` | 実行中の UpdateContents / アイテム更新を中断 |
| `ScrollRect ScrollRect` / `GameObject ItemPrefab` | 参照取得 |
| `void SetEdgeSpacing(float)` / `SetItemSpacing(float)` / `EdgeSpacing` / `ItemSpacing` | 端・アイテム間の余白 |
| `Observable<IVirtualScrollItem> OnCreateItemAsObservable()` | セル生成時 |
| `Observable<IVirtualScrollItem> OnUpdateItemAsObservable()` | セル内容更新時（スクロールによる使い回し毎） |
| `Observable<Unit> OnUpdateContentsAsObservable()` | UpdateContents 完了時 |
| protected virtual `OnInitialize()` / `OnCreateItem(item)` / `OnUpdateItem(item, token)` / `OnUpdateContents(token)` | 派生クラス用フック（UniTask） |

### VirtualScrollItem\<T\>（`Modules.UI`）

| メンバー | 説明 |
|---|---|
| `int Index` | 現在割り当てられたデータ index（Loop 時は循環） |
| `T Content` | 現在のデータ（範囲外は null → セル自体が非アクティブ化される） |
| `RectTransform RectTransform` | キャッシュ付き取得 |
| `virtual UniTask Initialize(CancellationToken)` | 生成時1回。ボタン購読等はここ |
| `abstract UniTask UpdateContents(T content, CancellationToken)` | 表示反映（使い回しの度に呼ばれる） |

### GridVirtualScroll\<T\> / GridVirtualScrollItem\<T, TComponent\>（`Modules.UI`）

| メンバー | 説明 |
|---|---|
| `GridVirtualScroll<T>.SetContents(IEnumerable<T>)` | `lineElementCount` 毎に行分割して設定 |
| `GridVirtualScroll<T>.SetLineElementCount(int)` / `GridLineElementCount` | 1行の要素数 |
| `GridVirtualScroll<T>.GetAllElements<TComponent>()` | 全行の全要素コンポーネント取得 |
| protected virtual `BuildElements(IEnumerable<T>)` | 行分割ロジックの差し替え（並び順カスタム用） |
| `GridElement.StartIndex` / `.Elements` | 行の先頭 index / 行内データ |
| `GridVirtualScrollItem` SerializeField: `elementPrefab` / `elementParent` | 行内要素のプレハブ / 生成先 |
| `GridVirtualScrollItem.Elements` | 行内の要素コンポーネント一覧 |
| protected virtual `OnCreateElement(TComponent)` | 要素生成時1回 |
| protected abstract `UniTask UpdateContents(int index, T content, TComponent element, CancellationToken)` | 要素1個の表示反映 |

### SpriteLoader（`Modules.UI.SpriteLoader`）

| メンバー | 説明 |
|---|---|
| `ImageSpriteLoader.SetSprite(string loadPath)` | UniTask。ExternalAsset から Sprite ロード→Image へ。同一パスはスキップ、null なら Image 非表示 |
| `ImageSpriteLoader.LoadPath` | 現在のロードパス |
| `ImageAtlasSpriteLoader.SetSprite(string atlasLoadPath, string spriteName)` | UniTask。SpriteAtlas ロード→スプライト名で取得。アトラスは使い回し |
| `ImageAtlasSpriteLoader.AtlasLoadPath` / `.SpriteName` | 現在のアトラスパス / スプライト名 |

### FocusManager / FocusTarget（`Modules.UI.Focus`）

| メンバー | 説明 |
|---|---|
| `FocusManager.Instance` | Singleton アクセス（自動生成） |
| `SetFocusCanvas(Canvas canvas)` | 基準 Canvas 設定（**フォーカス表示前に必須**） |
| `AddFocus(string focusId)` / `RemoveFocus(string focusId)` / `RemoveAllFocus()` | focusId 単位でフォーカス制御 |
| `Contains(string focusId)` / `HasFocus` / `Targets` | 状態参照 |
| `OnUpdateFocusAsObservable()` | フォーカス集合の変更通知 |
| `FocusTarget.FocusId` / `IsFocus` | GUID（未設定時自動生成） / フォーカス中か |
| `FocusTarget.Focus(Canvas)` / `Release()` | 直接制御（通常は FocusManager 経由） |

### その他（`Modules.UI` 直下ほか）

| クラス.メンバー | 説明 |
|---|---|
| `ProgressBar.FillAmount` | 0〜1。設定で即時反映＋`OnValueChangedAsObservable()` 通知 |
| `ProgressBar.Steps` / `CurrentStep` / `AddFill()` / `RemoveFill()` | 段階ゲージ用 |
| `ProgressBar.Mode` / `TargetImage` / `Sprites` / `MinWidth` / `MaxWidth` | FillMode 毎の設定 |
| `new FadeGraphic(float duration, Func<float> getAlpha, bool ignoreTimeScale = false)` | コンストラクタ。getAlpha は現在値の取得コールバック |
| `FadeGraphic.FadeIn()` / `FadeOut()` | UniTask。`OnChangeAlphaAsObservable()`/`OnChangeActiveAsObservable()` を購読して反映する |
| `GraphicGroup.ColorTint` / `IgnoreTargets` / `UpdateContents()` | 子 Graphic 一括色。子構成変更後は `UpdateContents()` を呼ぶ |
| `ColorGradation.Mode` / `Direction` / `ColorTop/Bottom/Left/Right` / `VerticalGradient` / `HorizontalGradient` / `Refresh()` | グラデーション設定。実行中の変更後は `Refresh()` |
| `FlipGraphic.Horizontal` / `.Vertical` | 反転切替 |
| `GlowGraphic.emissionColor` | HDR 発光色（public フィールド、実行中は自動反映） |
| `DragObject.Target` / `Horizontal` / `Vertical` / `Inertia` / `ConstrainWithinCanvas` / `StopMovement()` / `OnBeginDrag(End/－)AsObservable()` | ドラッグ移動対象と制約 |
| `UIDragAndDropHandler.OnDragStartAsObservable()` / `OnDragAsObservable()` / `OnDragEndAsObservable()` | スクリーン座標(Vector2)通知 |
| `UISwipeHandler.OnSwipeLeft/Right/Up/DownAsObservable()` / `IsSwipe` | スワイプ通知 |
| `SnapScrollRect.RegisterTargets(GameObject[])` / `Snap(GameObject)` / `StopSnap()` / `OnSnapAsObservable()` | 吸着対象登録と通知 |
| `SpriteNumberImage.Set(string text)` / `ClearText()` / `SetColor(Color)` / `SetLayout(LayoutType)` / `SetSpan(float)` / `OnAnimationStart(Finish)AsObservable()` | スプライト数字表示 |
| `HyperlinkEventHandler.OnHyperlinkActionAsObservable()` | `<link="id">` の id を通知（Observable\<string\>） |
| `RotationManager.Instance.RotateType` | 設定すると登録済み RotationRoot 全てに適用 |
| `ScreenPositionTracker.SetTarget(Transform target, Vector2 offset)` | 追従対象変更 |
| `UIAutoScaler.UpdateScale()` | 手動再計算 |
| `PreferredSizeCopy.SetCopySource(RectTransform)` / `UpdateLayoutImmediate()` / `horizontalMin/Max` / `verticalMin/Max` | コピー元変更・即時反映・クランプ |
| `ContentSizeFitterMaxHeight.MaxHeight` / `ContentSizeFitterMaxWidth.MaxWidth` | 上限値 |
| `ButtonInteractableTextColor.EnableColor` / `DisableColor` | interactable 連動色 |
| `ButtonInteractableImageSprite.EnableSprite` / `DisableSprite` | interactable 連動スプライト |

## 注意点・罠

- **`Modules.UI.Extension` の各 UI クラスは abstract**。使う・継承するのは `Dominion.Client` 側の具象クラス（`Client/Assets/Scripts/Client/Core/UI/`）。基盤側を直接継承した新クラスを乱造しない。
- **VirtualScroll は `SetContents()` → `await UpdateContents()` の2段階**。UpdateContents を呼び忘れると何も表示されない。データ変更後も再度 UpdateContents が必要（位置維持は `keepScrollPosition: true`）。
- **VirtualScroll のセルサイズは固定**。itemPrefab の RectTransform サイズを初回に1度だけ取得する（`itemSize`）。可変高さのリストには使えない。
- itemPrefab には `VirtualScrollItem<T>` 派生コンポーネントを付けておくこと（`UnityUtility.Instantiate<VirtualScrollItem<T>>` で取得される）。
- **セルは使い回される**。`VirtualScrollItem.UpdateContents` では表示状態（active 切替含む）を毎回すべて設定する。前回データの表示が残る事故が典型的バグ。
- `VirtualScrollItem.Initialize` はセル生成時に1回だけ。ボタン購読はここで行い、`UpdateContents` 内で毎回 Subscribe しない（多重購読になる）。
- `hitBoxEnable=true`（デフォルト）で viewport 全面に `GraphicCast` 製 "HitBox" が自動生成される（最背面配置）。リスト背面のタッチを拾いたくない場合は Inspector でオフ。
- `ScrollType.Loop` は `movementType=Unrestricted` を強制。Client 側 `UIScrollView.autoScrollDisable` と干渉しないよう content を +1px 拡張する実装が入っている（`VirtualScroll.cs` 内コメント参照）。
- `GridVirtualScrollItem.Initialize` は行オブジェクトに `Canvas` + `GraphicRaycaster` を自動付与する（描画分割のため）。
- `UIButton` は初期化時に `Navigation.Mode.None` を強制設定（Tab フォーカス移動禁止）。
- `ButtonEventTrigger`（長押し系の内部実装）は `pointerId > 0`（2本目以降のマルチタッチ）を無視する。
- `FocusManager` / `RotationManager` は `Extensions.Singleton<T>`（非 MonoBehaviour）。`Instance` 参照で自動生成、`CreateInstance` 呼び出し不要。
- Focus 使用前に `FocusManager.SetFocusCanvas(canvas)` が必須（未設定だと Focus が適用されない）。
- `UIImage` / `UIRawImage` / `DummySprite` / `DummyText` の `assetGuid`/`spriteId` はエディタ専用ダミーアセット機構。ビルドには含まれず、ダミー登録済み箇所は実行時に画像未設定なら自動非表示になる。実行時の画像は SpriteLoader 等で設定する。
- SpriteLoader のロードパスは ExternalAssets の規約に従う。`OnDestroy` でキャッシュを自動破棄するので手動解放は不要。
- クラス名とファイル名の不一致が2件: `DragTarget.cs` → クラス `DragObject`、`ButtonInteractablemageSprite.cs` → クラス `ButtonInteractableImageSprite`（namespace `Modules.UI.Reactive`）。grep 時に注意。
- `SpriteNumber` / `TextHyperlink` / `SnapScrollRect` / `DragObject` / `UISwipeHandler` / `UIDragAndDropHandler` / `RotationManager` 等は Client 側コードでの使用実績なし（2026-07時点。プレハブ直付け or 未使用）。ただし新規実装で同等機能が必要な場合は必ずこれらを使う。
- 本モジュールの Observable は **R3**（`R3.Observable<T>` / `R3.Subject<T>`）。UniRx の `IObservable<T>` ではない。購読は `.AddTo(this)` で自動解除する。

## 関連

- [View](View.md) — 画面・Window 実装基盤（本モジュールのコンポーネントを配置する上位層）
- [TextData](TextData.md) — `UIText` に表示する文字列の取得元（直書き禁止）
- [ExternalAsset](ExternalAsset.md) — `ImageSpriteLoader` / `ImageAtlasSpriteLoader` のロード元
- [Cache](Cache.md) — SpriteLoader 内部のキャッシュ機構（`Cache<T>` / `SpriteAtlasCache`）
- [Sound](Sound.md) — Client 側 `UIButton` のクリック SE 再生
- [R3Extension](R3Extension.md) — Observable まわりの拡張
- [ObjectPool](ObjectPool.md) — リスト以外でオブジェクトを使い回す場合

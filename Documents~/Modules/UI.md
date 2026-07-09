# UI

> **namespace**: `Modules.UI`（直下） / `Modules.UI.Extension` / `Modules.UI.VirtualScroll` / `Modules.UI.SpriteLoader` / `Modules.UI.Layout` / `Modules.UI.Focus` / `Modules.UI.Reactive` / `Modules.UI.ScreenRotation` / `Modules.UI.SpriteNumber` / `Modules.UI.TextHyperlink` / `Modules.UI.DummyContent` / `Modules.UI.Particle` / `Modules.Devkit.UI`（エディタ専用）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/UI/`
> **依存**: R3（UniRxではない） / UniTask / DOTween / TextMeshPro / Extensions / Modules.Cache / Modules.ExternalAssets

## 概要

uGUI 標準コンポーネントのラッパー（Extension）と、仮想スクロールリスト・スプライトローダー・レイアウト補助・チュートリアル用フォーカス等の UI 基盤群。
イベントは全て R3 の `Observable<T>` を `OnXxxAsObservable()` で公開する共通パターン。
主要クラス: Extension（uGUI ラッパーの abstract 基底群）/ `VirtualScroll<T>`・`GridVirtualScroll<T>`（仮想スクロール）/ `ImageSpriteLoader`・`ImageAtlasSpriteLoader`（外部アセット画像）/ `FocusManager`+`FocusTarget`（最前面フォーカス）/ `ProgressBar`・`GraphicCast` 等の単機能コンポーネント多数（逆引き参照）。

**最重要**: `Modules.UI.Extension` の各クラス（`UIButton` 等）は abstract。利用側は具象クラスを実装して使う。
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

## uGUI 標準 → ラッパー対応表

新規 UI 実装時は uGUI 標準コンポーネントを直接フィールドに持たず、以下の Extension ラッパー（利用側で abstract を継承した具象クラス）を使う。

| uGUI 標準 | 使うべきもの（基底） | 追加機能（具象実装が担うもの含む） |
|---|---|---|
| `Button` | `UIButton` | クリックSE・`OnClick`/長押し Observable・Navigation 無効化 |
| `TextMeshProUGUI` | `UIText` | `text` プロパティ（`SetText` 経由） |
| `Image` | `UIImage` | `sprite`/`color`/`alpha`・エディタ用ダミー画像機構 |
| `RawImage` | `UIRawImage` | `texture`・空テクスチャ時自動非表示（ダミー登録時） |
| `ScrollRect` | `UIScrollView` または `VirtualScroll<T>` | 位置ロック・自動スクロール無効化（`autoScrollDisable`）・横スクロールのホイール対応 |
| `Slider` | `UISlider` | `value` |
| `Toggle` | `UIToggle` | `isOn`・`OnChangeAsObservable()` |
| `TMP_InputField` | `UIInputField` | `text`・`OnEndEditAsObservable()` |
| `TMP_Dropdown` | `UIDropdown` | `OnChangeAsObservable()` |
| `Scrollbar` | `UIScrollbar` | ラッパーのみ |
| `Canvas`（ルート） | `UICanvas` | `UICanvasCamera` の自動割当・CanvasScaler 自動設定 |
| `Image`(type=Filled) でゲージ自作 | `ProgressBar` | Filled/Resize/Sprites の3モード・ステップ対応 |
| 透明 `Image` でタッチブロック | `GraphicCast` | 頂点を生成しない（描画コストゼロ） |
| `LayoutGroup` で数百件のリスト | **禁止** → `VirtualScroll<T>` | セル使い回しで生成数を画面内+α に抑える |

## 使い方

- **UIButton / UIText / UIImage の基本**: `[SerializeField]` フィールドで参照し、`button.OnClick(Action)`（Subscribe + `AddTo(this)` 込みの簡易登録）か `OnClickAsObservable().Subscribe(...).AddTo(this)` で購読
- **VirtualScroll（単列リスト）は3点セットで作る**: ①リスト View（`VirtualScroll<T>` 派生・中身は空でよい） ②アイテム View（`VirtualScrollItem<T>` 派生。`Initialize` はセル生成時に1回だけ=ボタン購読等はここ、`UpdateContents` はセルが使い回される度=表示状態は毎回全て設定、`Content` が現在このセルに割り当てられたデータ） ③呼び出し側は `SetContents(contents)` → `await UpdateContents()` の2段階。itemPrefab（アイテム View 付き）・ScrollRect 等は Inspector で設定する
- **GridVirtualScroll（グリッドリスト）**: 行 View は `GridVirtualScrollItem<データ型, 要素View型>` を継承し、要素1個分の更新（`UpdateContents(index, content, element, token)`）だけ書く。`elementPrefab` / `elementParent`（行内の親）は Inspector で設定。列数は Inspector の `lineElementCount` か `SetLineElementCount()`。呼び出し側は同じく `SetContents` → `await UpdateContents(keepScrollPosition)`（タブ切替時などは true で位置維持）
- **SpriteLoader（外部アセットの画像表示）**: `await loader.SetSprite(loadPath)` / `await loader.SetSprite(atlasLoadPath, spriteName)`
- **Focus（チュートリアルの最前面フォーカス）**: `FocusTarget` を対象 UI に付与し（FocusId は Inspector で GUID 自動生成）、`FocusManager.SetFocusCanvas(基準Canvas)` を先に設定してから `AddFocus(focusId)` / `RemoveAllFocus()` で制御
- **ProgressBar（ゲージ）**: `gauge.FillAmount` に 0〜1 を設定

## 注意点・罠

- **`Modules.UI.Extension` の各 UI クラスは abstract**。使う・継承するのは利用側の具象クラス。基盤側を直接継承した新クラスを乱造しない
- **VirtualScroll は `SetContents()` → `await UpdateContents()` の2段階**。UpdateContents を呼び忘れると何も表示されない。データ変更後も再度 UpdateContents が必要（位置維持は `keepScrollPosition: true`）
- **VirtualScroll のセルサイズは固定**。itemPrefab の RectTransform サイズを初回に1度だけ取得する（`itemSize`）。可変高さのリストには使えない
- itemPrefab には `VirtualScrollItem<T>` 派生コンポーネントを付けておくこと（`UnityUtility.Instantiate<VirtualScrollItem<T>>` で取得される）
- **セルは使い回される**。`VirtualScrollItem.UpdateContents` では表示状態（active 切替含む）を毎回すべて設定する。前回データの表示が残る事故が典型的バグ。`VirtualScroll<T>.ListItems` で取得できるセルも使い回し分のみ（データ件数分ではない）
- `VirtualScrollItem.Initialize` はセル生成時に1回だけ。ボタン購読はここで行い、`UpdateContents` 内で毎回 Subscribe しない（多重購読になる）
- `hitBoxEnable=true`（デフォルト）で viewport 全面に `GraphicCast` 製 "HitBox" が自動生成される（最背面配置）。リスト背面のタッチを拾いたくない場合は Inspector でオフ
- `ScrollType.Loop` は `movementType=Unrestricted` を強制。利用側 `UIScrollView.autoScrollDisable` と干渉しないよう content を +1px 拡張する実装が入っている（`VirtualScroll.cs` 内コメント参照）
- `GridVirtualScrollItem.Initialize` は行オブジェクトに `Canvas` + `GraphicRaycaster` を自動付与する（描画分割のため）
- `UIButton` は初期化時に `Navigation.Mode.None` を強制設定（Tab フォーカス移動禁止）
- `ButtonEventTrigger`（長押し系の内部実装）は `pointerId > 0`（2本目以降のマルチタッチ）を無視する
- `FocusManager` / `RotationManager` は `Extensions.Singleton<T>`（非 MonoBehaviour）。`Instance` 参照で自動生成、`CreateInstance` 呼び出し不要
- Focus 使用前に `FocusManager.SetFocusCanvas(canvas)` が必須（未設定だと Focus が適用されない）
- `UIImage` / `UIRawImage` / `DummySprite` / `DummyText` の `assetGuid`/`spriteId` はエディタ専用ダミーアセット機構。ビルドには含まれず、ダミー登録済み箇所は実行時に画像未設定なら自動非表示になる。実行時の画像は SpriteLoader 等で設定する
- SpriteLoader のロードパスは ExternalAssets の規約に従う。`OnDestroy` でキャッシュを自動破棄するので手動解放は不要
- クラス名とファイル名の不一致が2件: `DragTarget.cs` → クラス `DragObject`、`ButtonInteractablemageSprite.cs` → クラス `ButtonInteractableImageSprite`（namespace `Modules.UI.Reactive`）。grep 時に注意
- 本モジュールの Observable は **R3**（`R3.Observable<T>` / `R3.Subject<T>`）。UniRx の `IObservable<T>` ではない。購読は `.AddTo(this)` で自動解除する

## 関連

- [View](View.md) — 画面・Window 実装基盤（本モジュールのコンポーネントを配置する上位層）
- [TextData](TextData.md) — `UIText` に表示する文字列の取得元（直書き禁止）
- [ExternalAsset](ExternalAsset.md) — `ImageSpriteLoader` / `ImageAtlasSpriteLoader` のロード元
- [Cache](Cache.md) — SpriteLoader 内部のキャッシュ機構（`Cache<T>` / `SpriteAtlasCache`）
- [Sound](Sound.md) — `UIButton` 具象実装のクリック SE 再生で組み合わせる
- [R3Extension](R3Extension.md) — Observable まわりの拡張
- [ObjectPool](ObjectPool.md) — リスト以外でオブジェクトを使い回す場合

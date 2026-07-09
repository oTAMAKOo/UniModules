# OffScreenRendering

> **namespace**: `Modules.OffScreenRendering`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/OffScreenRendering/`
> **依存**: R3 / Extensions（`UnityUtility`） / uGUI（`RawImage`, `IPointerClickHandler`）

## 概要

専用カメラで RenderTexture へオフスクリーン描画し、その結果を uGUI の `RawImage` に表示するための基盤。3D/2Dモデルを UI 上に表示する用途（キャラビューア等）を想定。`RawImage` 上のクリック位置を RenderTexture 側カメラの Ray に逆変換してヒット判定する仕組み（`RenderTextureRaycaster`）も持つ。

主要クラス: `RenderTarget`（RenderTexture を生成して同 GameObject の Camera の `targetTexture` に設定）/ `RenderTextureRaycaster`（abstract。クリック位置→描画カメラの Ray に変換し `Raycast(Ray)` を呼ぶ）/ `Collider2DRayCast`（`Physics2D.RaycastAll` でヒットした GameObject 群を通知する具象）。

シンボルゲート無しでコンパイル対象。基盤内の派生クラスは `Live2DRaycaster`（[Live2D](Live2D.md)、`ENABLE_LIVE2D` 定義時のみ有効）のみ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| カメラ映像を RenderTexture に出力したい | `RenderTarget`（Camera と同じ GameObject に付与） |
| RenderTexture を任意サイズで作り直したい | `RenderTarget.CreateRenderTexture(width, height, depth, format)` |
| RawImage 表示中のオフスクリーン映像をクリック判定したい | `RenderTextureRaycaster` 派生（`RawImage` と同じ GameObject に付与） |
| クリックで 2D コライダーをヒット判定したい | `Collider2DRayCast.OnRaycastHitAsObservable()` |
| 独自のヒット判定をしたい | `RenderTextureRaycaster` を継承し `Raycast(Ray)` を実装 |

## 注意点・罠

- `RenderTextureRaycaster.Initialize()` は**手動呼び出し必須**（呼ばないとクリック判定も無効）。また SerializeField の `renderTarget` を inspector で割り当てていないと `Initialize` で NullReference になる。
- `Initialize()` はカメラを `UnityUtility.FindCameraForLayer(1 << gameObject.layer)` から「`RenderTarget` が付いていないカメラ」で検索する。RawImage のレイヤーを描画する UI カメラが存在しないと null 参照で落ちる。
- `Initialize()` 時点の RawImage 表示サイズで RenderTexture を固定生成する。レイアウト確定前に呼ぶとサイズがずれ、以後追従もしない（リサイズ対応は無い）。
- 入力は `IPointerClickHandler`（クリック/タップ）のみ。ドラッグ・ホバー等は未対応。RawImage が Raycast Target であることが前提。
- `RenderTarget` は `Awake` で SerializeField サイズ（既定 100x100）の RenderTexture を勝手に作る（`[ExecuteAlways]` のためエディタ編集中も動く）。`RenderTextureRaycaster.Initialize` が後から正しいサイズで作り直す構造。
- `Collider2DRayCast` の Ray 距離は 10 固定。カメラからコライダーまでの距離が 10 を超える配置では判定できない。また `OnRaycastHitAsObservable()` はヒット無しの時は流れない。
- 生成した RenderTexture の明示的な破棄 API は無い（作り直し時のみ `SafeDelete`）。大量に使い捨てる場合はリークに注意。

## 関連

- [Live2D](Live2D.md) — `RenderTextureRaycaster` の派生 `Live2DRaycaster`（`ENABLE_LIVE2D` 定義時のみ有効）
- [Rendering](Rendering.md) — 描画系ユーティリティ
- [UI](UI.md) — uGUI 基盤

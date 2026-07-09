# Live2D

> **namespace**: `Modules.Live2D`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Live2D/`
> **依存**: Live2D Cubism SDK（`Live2D.Cubism.Core` / `Live2D.Cubism.Framework.Raycasting`） / R3 / Modules.OffScreenRendering

## 概要

Live2D Cubism モデルをオフスクリーン描画（RenderTexture → RawImage）で UI 表示した際に、クリック位置から Cubism モデルのパーツ（`CubismDrawable`）をヒット判定するための基盤。中身は `Live2DRaycaster` 1クラスのみ（sealed MonoBehaviour、[OffScreenRendering](OffScreenRendering.md) の `RenderTextureRaycaster` 派生）。

利用側で `ENABLE_LIVE2D` シンボルが未定義の場合、`Raycast` は空実装になり公開メンバーも消える（クラス定義自体はコンパイルされる）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Live2D モデルを表示したい | Cubism SDK 導入 + `ENABLE_LIVE2D` 定義（利用側で有効化されていない場合は使えない） |
| RenderTexture 経由の UI クリック判定だけしたい | このモジュールではなく [OffScreenRendering](OffScreenRendering.md)（`Collider2DRayCast` か独自派生） |
| （Live2D有効時）クリックした Cubism パーツを知る | `Live2DRaycaster.SetParams(cubismRaycaster)` → `OnRaycastHitAsObservable()` |

## 注意点・罠

- 有効化には (1) Live2D Cubism SDK（Unity 向け）導入、(2) `ENABLE_LIVE2D` の Scripting Define Symbols 追加、(3) モデル表示側（`RenderTarget` カメラ + Cubism モデル + `CubismRaycaster`）のセットアップ一式が必要。
- クラス定義自体は常にコンパイルされる（`#if` がメンバー内側のため）。シンボル未定義でもコンポーネントとして貼れてしまうが、クリックしても何も起きない。
- （有効時）`SetParams` 未設定のままクリックすると NullReference。
- 有効時も基底の制約をそのまま受ける: `Initialize()` 手動呼び出し必須、inspector で `renderTarget` 割り当て必須、クリック（`IPointerClickHandler`）のみ対応。詳細は [OffScreenRendering](OffScreenRendering.md) の罠を参照。
- ヒット結果バッファは4件固定（`new CubismRaycastHit[4]`）。重なりが5枚以上ある場合は取りこぼす。

## 関連

- [OffScreenRendering](OffScreenRendering.md) — 基底クラス `RenderTextureRaycaster` / `RenderTarget`
- [CriWare](CriWare.md) / [Movie](Movie.md) — 同様に「SDK導入 + シンボル定義で有効化」するモジュールの例

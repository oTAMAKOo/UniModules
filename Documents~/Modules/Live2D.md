# Live2D

> **namespace**: `Modules.Live2D`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Live2D/`
> **Client側使用**: 0ファイル（2026-07時点。プレハブ/シーンからの参照も無し）
> **依存**: Live2D Cubism SDK（`Live2D.Cubism.Core` / `Live2D.Cubism.Framework.Raycasting`、**未導入**） / R3 / Modules.OffScreenRendering

## 概要

Live2D Cubism モデルをオフスクリーン描画（RenderTexture → RawImage）で UI 表示した際に、クリック位置から Cubism モデルのパーツ（`CubismDrawable`）をヒット判定するための基盤。中身は `Live2DRaycaster` 1クラスのみ（sealed MonoBehaviour、[OffScreenRendering](OffScreenRendering.md) の `RenderTextureRaycaster` 派生）。

**本プロジェクトでは実質未使用（機能はコンパイル対象外）**。`ENABLE_LIVE2D` はどこにも定義されておらず（`Client/ProjectSettings/ProjectSettings.asset` の `scriptingDefineSymbols` / `Client/Assets/csc.rsp` とも無し）、Live2D Cubism SDK 自体も Assets / Packages に存在しない。クラス定義だけはコンパイルされるが、`Raycast` は空実装になり公開メンバーも消える。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **Live2D モデルを表示したい（本プロジェクト）** | 不可。Cubism SDK 未導入（導入 + `ENABLE_LIVE2D` 定義から必要） |
| RenderTexture 経由の UI クリック判定だけしたい | このモジュールではなく [OffScreenRendering](OffScreenRendering.md)（`Collider2DRayCast` か独自派生） |
| （Live2D有効時）クリックした Cubism パーツを知る | `Live2DRaycaster.SetParams(cubismRaycaster)` → `OnRaycastHitAsObservable()` |

## 注意点・罠

- **本プロジェクトでは機能がコンパイル対象外**。Live2D 前提の実装を書かないこと。有効化には (1) Live2D Cubism SDK（Unity 向け）導入、(2) `ENABLE_LIVE2D` の Scripting Define Symbols 追加、(3) モデル表示側（`RenderTarget` カメラ + Cubism モデル + `CubismRaycaster`）のセットアップ一式が必要。
- クラス定義自体は常にコンパイルされる（`#if` がメンバー内側のため）。シンボル未定義でもコンポーネントとして貼れてしまうが、クリックしても何も起きない。
- （有効時）`SetParams` 未設定のままクリックすると NullReference。
- 有効時も基底の制約をそのまま受ける: `Initialize()` 手動呼び出し必須、inspector で `renderTarget` 割り当て必須、クリック（`IPointerClickHandler`）のみ対応。詳細は [OffScreenRendering](OffScreenRendering.md) の罠を参照。
- ヒット結果バッファは4件固定（`new CubismRaycastHit[4]`）。重なりが5枚以上ある場合は取りこぼす。

## 関連

- [OffScreenRendering](OffScreenRendering.md) — 基底クラス `RenderTextureRaycaster` / `RenderTarget`（こちらは使用可能な状態）
- [CriWare](CriWare.md) / [Movie](Movie.md) — 同様に「SDK未導入 + シンボル未定義でコンパイル対象外」なモジュールの例

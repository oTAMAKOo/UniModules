# SortingLayerSetter

> **namespace**: `Modules.SortingLayerSetter`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/SortingLayerSetter/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。prefab / scene からの参照もなし）
> **依存**: Unity.Linq（LINQ to GameObject） / Extensions / Extensions.Devkit（エディタ）

## 概要

`Renderer` 系コンポーネント（MeshRenderer / ParticleSystemRenderer 等、標準インスペクタに SortingLayer 欄が出ないもの）の **sortingLayerID / sortingOrder をインスペクタから設定する**ためのコンポーネント。
`ApplyChildObjects` を有効にすると子孫の全 Renderer に sortingOrder を一括適用できる。uGUI（Canvas 配下）には効かない。

主要クラス: `SortingLayerSetter`（sealed MonoBehaviour、`[ExecuteAlways]`。Awake / プロパティ set 時に自身の Renderer へ適用）/ `SortingLayerSetterInspector`（エディタ専用。SortingLayer 名のポップアップ選択 + Apply ボタン）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| MeshRenderer 等の SortingLayer をインスペクタで設定したい | `SortingLayerSetter` をアタッチ（Editor 拡張でポップアップ選択） |
| 実行時にソート順を変えたい | `SortingLayerSetter.SortingOrder = n`（set で即反映） |
| 子孫の Renderer もまとめて設定したい | `ApplyChildObjects = true` → `SetSortingLayer()` |

## 注意点・罠

- 保存値（`sortingLayer`）は `SortingLayer.id`（一意ID）基準。value（序数）や名前ではない
- `sortingLayer` フィールドは `[HideInInspector]`。設定はエディタ拡張のポップアップ（または `SortingLayer` プロパティ）経由のみ
- `ApplyChildObjects` は子孫に **sortingOrder のみ**適用し、sortingLayerID は適用しない（自身のみ両方適用）。また set しただけでは再適用されない（`SetSortingLayer()` を呼ぶ）
- `[ExecuteAlways]` のため Awake 適用はエディタ（非再生時）でも走る

## 関連

- [Rendering](Rendering.md) — 描画順・レンダリング関連の基盤
- [UI](UI.md) — uGUI 側の表示順は Canvas / GraphicGroup 系で制御

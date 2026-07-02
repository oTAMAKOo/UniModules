# SortingLayerSetter

> **namespace**: `Modules.SortingLayerSetter`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/SortingLayerSetter/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。prefab / scene からの参照もなし）
> **依存**: Unity.Linq（LINQ to GameObject） / Extensions / Extensions.Devkit（エディタ）

## 概要

`Renderer` 系コンポーネント（MeshRenderer / ParticleSystemRenderer 等、標準インスペクタに SortingLayer 欄が出ないもの）の **sortingLayerID / sortingOrder をインスペクタから設定する**ためのコンポーネント。
`ApplyChildObjects` を有効にすると子孫の全 Renderer に sortingOrder を一括適用できる。uGUI（Canvas 配下）には効かない。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| MeshRenderer 等の SortingLayer をインスペクタで設定したい | `SortingLayerSetter` をアタッチ（Editor 拡張でポップアップ選択） |
| 実行時にソート順を変えたい | `SortingLayerSetter.SortingOrder = n`（set で即反映） |
| 子孫の Renderer もまとめて設定したい | `ApplyChildObjects = true` → `SetSortingLayer()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `SortingLayerSetter` | sealed MonoBehaviour（`[ExecuteAlways]`） | Awake / プロパティ set 時に自身の Renderer へ sortingLayerID / sortingOrder を適用 |
| `SortingLayerSetterInspector` | エディタ専用（`Editor/`、`ScriptlessEditor` 継承） | SortingLayer 名のポップアップ選択 + Apply ボタン |

## 使い方(最小の想定例)

Client側に使用実績がないため想定例。基本はインスペクタでの設定運用。

```csharp
// 想定例（本プロジェクトに実使用コードなし）.
var setter = UnityUtility.GetComponent<SortingLayerSetter>(gameObject);

// set した時点で Renderer に即反映される.
setter.SortingOrder = 10;

// 子孫へ一括反映したい場合.
setter.ApplyChildObjects = true;
setter.SetSortingLayer();
```

## API(主要公開メンバー)

### SortingLayerSetter

| メンバー | 説明 |
|---|---|
| `int SortingLayer`（get/set） | SortingLayer の **id**（`UnityEngine.SortingLayer.layers[n].id`）。set で即適用 |
| `int SortingOrder`（get/set） | sortingOrder。set で即適用 |
| `bool ApplyChildObjects`（get/set） | true なら `SetSortingLayer()` 時に子孫 Renderer にも適用（set しただけでは再適用されない） |
| `void SetSortingLayer()` | 現在値を Renderer に適用（Awake でも自動実行） |

## 注意点・罠

- **2026-07 に2件の不具合を修正済み**: ①インスペクタのコピペ残骸（存在しない `RunCollectContents` のリフレクション呼び出しで、レイヤー変更時に例外）を削除 ②保存値の意味を `SortingLayer.value`（序数）から **`SortingLayer.id`（一意ID）** に是正（従来は Default 以外のレイヤーで `sortingLayerID` に無効値が入り効かなかった）
- **修正②により、修正前に保存された `sortingLayer` の値（value基準）は互換性がない**。他プロジェクトで本コンポーネントの配置済みデータがある場合はインスペクタから再設定が必要（Default レイヤー(0)のみ使用なら影響なし）
- `sortingLayer` フィールドは `[HideInInspector]`。設定はエディタ拡張のポップアップ（または `SortingLayer` プロパティ）経由のみ
- `ApplyChildObjects` は子孫に **sortingOrder のみ**適用し、sortingLayerID は適用しない（自身のみ両方適用）
- `[ExecuteAlways]` のため Awake 適用はエディタ（非再生時）でも走る

## 関連

- [Rendering](Rendering.md) — 描画順・レンダリング関連の基盤
- [UI](UI.md) — uGUI 側の表示順は Canvas / GraphicGroup 系で制御

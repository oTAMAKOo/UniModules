# Camera

カメラの描画範囲の比率を固定する機能。
画面の比率が違っても、カメラが描く範囲を指定した幅と高さの比率に保ち、余った部分を帯にできる。

## 基本情報

> **namespace**: `Modules.FixedAspectCamera`（**`Modules.Camera` ではない**。フォルダ名と不一致）
> **場所**: `Modules/Camera/FixedAspectCamera.cs`（1ファイルのみ）
> **依存**: UnityEngine のみ

## 概要

カメラの描画アスペクト比を固定するコンポーネント `FixedAspectCamera`（sealed MonoBehaviour、`[ExecuteAlways]` `[RequireComponent(typeof(Camera))]`）。指定比率（`fixedWidth / fixedHeight`）と画面の比率（`Screen.width / Screen.height`）を比べ、`Camera.rect` を中央寄せで縮めて、描画範囲を指定比率にする。指定比率が画面より横長なら高さを `画面比率 / 指定比率` 倍にして上下に帯を作り、それ以外は幅を `指定比率 / 画面比率` 倍にして左右に帯を作る。`Awake` で1回適用し、以降は `Update` で `Camera.aspect` が指定比率と異なる間、毎フレーム再計算する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| カメラの描画領域を固定アスペクト比にしたい | Camera と同じ GameObject に `FixedAspectCamera` をアタッチし `fixedWidth` / `fixedHeight` を設定 |
| 実行時に比率を変えたい | `FixedWidth` / `FixedHeight` プロパティ |
| （参考）Windowsのウィンドウ自体を固定比にしたい | [StandAloneWindows](../StandAloneWindows/Manual.md) の `AspectRatioHandler` |

## 注意点・罠

- **namespace は `Modules.FixedAspectCamera`**（`using Modules.Camera` は存在しない）
- `fixedWidth` / `fixedHeight` の初期値は 0。**未設定のまま動くと 0 除算で aspectRate が NaN** になり正しく動作しない（インスペクタでの設定必須）
- プロパティ set は比率（`aspectRate`）を片方ずつ再計算するだけで、`Camera.rect` への反映は次の `Update`。両方変える場合の途中状態に注意（Width → Height の順で set しても最終的には両方の値で計算される）
- 帯（rect 外）の領域は**このコンポーネントでは塗られない**。黒帯等にしたい場合は背面に全画面クリア用カメラ（`Depth` 低・SolidColor）を別途置く
- `Awake` / `Update` ベースの実装。アタッチするだけで動く反面、明示的な Setup フックはない
- `[ExecuteAlways]` のためエディタ（非再生）でも `Camera.rect` を書き換える

## 関連

- [StandAloneWindows](../StandAloneWindows/Manual.md) — `AspectRatioHandler`（Windows ウィンドウ自体のアスペクト固定）
- [Rendering](../Rendering/Manual.md) — 描画パイプライン関連
- [DeviceOrientation](../DeviceOrientation/Manual.md) — 画面向き関連

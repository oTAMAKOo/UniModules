# Camera

> **namespace**: `Modules.FixedAspectCamera`（**`Modules.Camera` ではない**。フォルダ名と不一致）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Camera/FixedAspectCamera.cs`（1ファイルのみ）
> **Client側使用**: 0ファイル（2026-07時点・未使用。prefab / scene からの参照もなし）
> **依存**: UnityEngine のみ

## 概要

カメラの描画アスペクト比を固定するコンポーネント `FixedAspectCamera`（sealed MonoBehaviour、`[ExecuteAlways]` `[RequireComponent(typeof(Camera))]`）。画面が指定比率より横長ならピラーボックス（左右帯）、縦長ならレターボックス（上下帯）になるよう `Camera.rect` を毎フレーム調整する。
本プロジェクトのカメラ運用は Client 側 `CommonCamera`（`InitializeObject.core.cs` の `CreateCommonCamera()`）ベースで、本コンポーネントは未使用。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| カメラの描画領域を固定アスペクト比にしたい | Camera と同じ GameObject に `FixedAspectCamera` をアタッチし `fixedWidth` / `fixedHeight` を設定 |
| 実行時に比率を変えたい | `FixedWidth` / `FixedHeight` プロパティ |
| （参考）Windowsのウィンドウ自体を固定比にしたい | [StandAloneWindows](StandAloneWindows.md) の `AspectRatioHandler` |

## 注意点・罠

- **namespace は `Modules.FixedAspectCamera`**（`using Modules.Camera` は存在しない）
- `fixedWidth` / `fixedHeight` の初期値は 0。**未設定のまま動くと 0 除算で aspectRate が NaN** になり正しく動作しない（インスペクタでの設定必須）
- プロパティ set は片方ずつ再計算するため、両方変える場合の途中状態に注意（Width → Height の順で set しても最終的には正しくなる）
- 帯（rect 外）の領域は**このコンポーネントでは塗られない**。黒帯等にしたい場合は背面に全画面クリア用カメラ（`Depth` 低・SolidColor）を別途置く
- `Awake` / `Update` ベースの既存実装（プロジェクトの「ライフサイクルメソッド原則禁止」ルール以前からの基盤コード）。アタッチするだけで動く反面、明示的な Setup フックはない
- `[ExecuteAlways]` のためエディタ（非再生）でも `Camera.rect` を書き換える

## 関連

- [StandAloneWindows](StandAloneWindows.md) — `AspectRatioHandler`（Windows ウィンドウ自体のアスペクト固定）
- [Rendering](Rendering.md) — 描画パイプライン関連
- [DeviceOrientation](DeviceOrientation.md) — 画面向き関連

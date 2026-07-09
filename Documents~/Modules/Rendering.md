# Rendering

> **namespace**: `Modules.Rendering.Universal`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Rendering/Universal/`（CameraStackManager.cs / CameraStackTarget.cs）。全体が `#if ENABLE_UNIVERSALRENDERPIPELINE`（利用側でシンボル定義が必要）
> **依存**: URP（`UnityEngine.Rendering.Universal.UniversalAdditionalCameraData`）/ R3 + R3.Triggers（`OnDestroyAsObservable`）/ Extensions（`Singleton`, `UnityUtility`）

## 概要

URP のカメラスタック（Base カメラ + Overlay カメラ群）を priority 順に自動構成する基盤。
Overlay にしたいカメラへ `CameraStackTarget` を付けておき（`autoStack` で有効化時に自動登録）、`CameraStackManager.SwitchMainCamera()` で Base カメラを切り替えると、登録済み Overlay が priority 昇順で新 Base のスタックに積み直される。
シーンごとに Base カメラが変わっても Overlay 構成（UI・Devkit 等）を維持するための仕組み。
主要クラス: `CameraStackManager`（sealed Singleton・非MonoBehaviour。現在の Base カメラ管理・Overlay リスト（priority 昇順）・スタック再構築・破棄カメラの自動除去）/ `CameraStackTarget`（sealed MonoBehaviour・`[RequireComponent(UniversalAdditionalCameraData)]`。Overlay 対象カメラのマーカー。`priority`（uint）と `autoStack`（OnEnable 時自動登録）を持つ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Base カメラを切り替えたい（スタック引き継ぎ） | `CameraStackManager.Instance.SwitchMainCamera(camera)` |
| カメラを Overlay としてスタックに載せたい | 対象カメラに `CameraStackTarget` を付与（インスペクタで `priority` / `autoStack` 設定） |
| コードから Overlay 登録したい | `cameraStackTarget.AddStack()` または `CameraStackManager.Instance.AddStackCamera(camera)` |
| Overlay をスタックから外したい | `RemoveStackCamera(camera)`（破棄時は自動除去） |
| 描画順を制御したい | `CameraStackTarget.priority`（**昇順に積まれる = 小さいほど奥**） |
| スタックを組み直したい | `UpdateCurrentCameraStack()` |

## 使い方

- 起動時に Base カメラを登録: 対象カメラを `Instantiate` した後 `cameraStackManager.SwitchMainCamera(baseCamera)`
- Overlay 側はプレハブ設定のみ（コード不要）。Base カメラ用のプレハブに Overlay 用カメラをぶら下げ、各 Overlay カメラに `CameraStackTarget` を付けて `autoStack = true` にする構成が典型

## 注意点・罠

- **`ENABLE_UNIVERSALRENDERPIPELINE` 未定義環境ではクラスごと存在しない**。利用側で常時定義する運用が前提
- `CameraStackTarget.autoStack` は **OnEnable で自動登録**する既存実装（Unity ライフサイクル使用）。`CameraStackManager.Instance` が初アクセス時に生成される Singleton のため初期化順の問題は起きにくいが、登録が `SwitchMainCamera` より先でも `UpdateCurrentCameraStack` が Base 未設定時は何もしない（Base 設定時に積み直されるので問題ない）
- `AddStackCamera` はカメラの `renderType` を**問答無用で Overlay に変更**する。Base にしたいカメラへ `CameraStackTarget` を付けないこと
- `priority` は**昇順で積まれる**（小さい = 先に描画 = 奥、大きい = 手前）
- Overlay カメラの破棄は自動除去されるが、**非アクティブ化では除去されない**（スタックに残る。URP は無効カメラを描画しないため実害は薄いが、明示的に外すなら `RemoveStackCamera` + `UpdateCurrentCameraStack`）
- `RemoveStackCamera` 単体ではスタック再構築されない（`UpdateCurrentCameraStack()` を続けて呼ぶ）
- `OnEnable` 側の自動登録に対応する自動解除（OnDisable）はない

## 関連

- [Window](Window.md) — Overlay レイヤーのカメラに載る UI（PopupManager の Global 親等）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `UnityUtility.GetComponent`

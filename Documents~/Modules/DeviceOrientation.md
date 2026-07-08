# DeviceOrientation

> **namespace**: `Modules.DeviceOrientation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/DeviceOrientation/`（`DeviceOrientationManagerBase.cs` の1ファイルのみ）
> **Client側使用**: using は1ファイル（2026-07時点）= `Dominion.Client.Core.DeviceOrientationManager`（`Client/Assets/Scripts/Client/Core/DeviceOrientationManager.cs`、中身は空の継承）。起動時に `InitializeObject.manager.cs` が生成・初期化
> **依存**: R3 / Extensions（`Singleton`）/ Modules.ApplicationEvent

## 概要

画面の向き（ScreenOrientation）の適用と監視を行う Singleton 基底クラス。
デフォルト実装は**横持ち固定（LandscapeLeft/Right の自動回転のみ許可）**で、本プロジェクトはそのまま使用。
レジューム時の再適用（OS 側で設定が戻るのを防ぐ）と、向き変更の Observable 通知を持つ。
主要クラス: `DeviceOrientationManagerBase<TInstance>`（abstract Singleton・非MonoBehaviour。向きの適用（virtual `Apply`）・毎フレーム監視・レジューム時再適用・変更通知）/ Client側 `Dominion.Client.Core.DeviceOrientationManager`（空継承。基底のデフォルト = 横持ち設定をそのまま採用）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 現在の画面向きを知りたい | `DeviceOrientationManager.Instance.Orientation` |
| 画面向きの変更を購読したい | `OnOrientationChangedAsObservable()` |
| 向き設定を再適用したい | `Apply()` |
| 縦持ち対応など向きポリシーを変えたい | Client側 `DeviceOrientationManager` で `Apply(ScreenOrientation?)` を override |

## 使い方

- 起動時初期化: `DeviceOrientationManager.CreateInstance()` → `Initialize()`。実例: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs` の `InitializeDeviceOrientationManager()`。`InitializeManager()` 内の**最初**に呼ばれる（画面向きは他の初期化に先立って確定させる）
- Client側の実装は空継承でデフォルト横持ち（`Client/Assets/Scripts/Client/Core/DeviceOrientationManager.cs`）。基底のデフォルト `Apply` は autorotate を LandscapeLeft/Right のみ許可 + `Screen.orientation = AutoRotation` を適用する

## 注意点・罠

- `Initialize()` 必須（呼ばないと Apply も監視も動かない）。本プロジェクトでは起動時に実施済みのため、利用側は `Instance` 参照だけでよい
- 監視は `ObserveEveryValueChanged` による毎フレームポーリング。変更のたびに `Apply(orientation)` が再実行される
- `OnOrientationChangedAsObservable()` は AutoRotation への変化は通知されない
- デフォルト `Apply` の引数 `orientation` は未使用（何を渡しても横持ち設定を適用）。縦横切替のような制御をしたければ override 側で引数を解釈する
- 二重 `Initialize()` のガードはない（購読が重複する）。呼び出しは起動時1回に限定する

## 関連

- [ApplicationEvent](ApplicationEvent.md) — レジューム時再適用のイベント供給元（`OnResumeAsObservable`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance`）

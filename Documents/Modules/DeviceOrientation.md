# DeviceOrientation

> **namespace**: `Modules.DeviceOrientation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/DeviceOrientation/`（`DeviceOrientationManagerBase.cs` の1ファイルのみ）
> **Client側使用**: using は1ファイル（2026-07時点）= `Dominion.Client.Core.DeviceOrientationManager`（`Client/Assets/Scripts/Client/Core/DeviceOrientationManager.cs`、中身は空の継承）。起動時に `InitializeObject.manager.cs` が生成・初期化
> **依存**: R3 / Extensions（`Singleton`）/ Modules.ApplicationEvent

## 概要

画面の向き（ScreenOrientation）の適用と監視を行う Singleton 基底クラス。
デフォルト実装は**横持ち固定（LandscapeLeft/Right の自動回転のみ許可）**で、本プロジェクトはそのまま使用。
レジューム時の再適用（OS 側で設定が戻るのを防ぐ）と、向き変更の Observable 通知を持つ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 現在の画面向きを知りたい | `DeviceOrientationManager.Instance.Orientation` |
| 画面向きの変更を購読したい | `OnOrientationChangedAsObservable()` |
| 向き設定を再適用したい | `Apply()` |
| 縦持ち対応など向きポリシーを変えたい | Client側 `DeviceOrientationManager` で `Apply(ScreenOrientation?)` を override |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `DeviceOrientationManagerBase<TInstance>` | abstract Singleton（`Extensions.Singleton<T>` 継承・非MonoBehaviour） | 向きの適用（virtual `Apply`）・毎フレーム監視・レジューム時再適用・変更通知 |
| Client側 `Dominion.Client.Core.DeviceOrientationManager` | sealed（Client実装） | 空継承（基底のデフォルト = 横持ち設定をそのまま採用） |

## 使い方(実例)

### 実例1: 起動時初期化（他マネージャより先に実行）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
private void InitializeDeviceOrientationManager()
{
    var deviceOrientationManager = DeviceOrientationManager.CreateInstance();

    deviceOrientationManager.Initialize();
}
```

`InitializeManager()` 内の**最初**に呼ばれる（画面向きは他の初期化に先立って確定させる）。

### 実例2: Client側の実装（空継承でデフォルト横持ち）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/DeviceOrientationManager.cs
public sealed class DeviceOrientationManager : DeviceOrientationManagerBase<DeviceOrientationManager>
{
}
```

### 基底のデフォルト Apply（参考: 何が適用されるか）

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/DeviceOrientation/DeviceOrientationManagerBase.cs
public virtual void Apply(ScreenOrientation? orientation = null)
{
    //------ 横持ち時の設定 ------

    Screen.autorotateToPortrait = false;
    Screen.autorotateToPortraitUpsideDown = false;
    Screen.autorotateToLandscapeLeft = true;
    Screen.autorotateToLandscapeRight = true;
    Screen.orientation = ScreenOrientation.AutoRotation;
}
```

## API(主要公開メンバー)

### DeviceOrientationManagerBase&lt;TInstance&gt;

| メンバー | 説明 |
|---|---|
| `Initialize()` | `Apply()` 実行 + 向き監視開始（`ObserveEveryValueChanged`）+ レジューム時再適用の購読。起動時に1回必須 |
| `Orientation : ScreenOrientation` | 現在の向き（`Screen.orientation` の薄いラッパー） |
| `virtual Apply(ScreenOrientation? orientation = null)` | 向きポリシーの適用。override ポイント（デフォルトは横持ち固定） |
| `OnOrientationChangedAsObservable() : Observable<ScreenOrientation>` | 向き変更通知（AutoRotation への変化は通知されない） |

## 注意点・罠

- `Initialize()` 必須（呼ばないと Apply も監視も動かない）。本プロジェクトでは起動時に実施済みのため、利用側は `Instance` 参照だけでよい
- 監視は `ObserveEveryValueChanged` による毎フレームポーリング。変更のたびに `Apply(orientation)` が再実行される
- デフォルト `Apply` の引数 `orientation` は未使用（何を渡しても横持ち設定を適用）。縦横切替のような制御をしたければ override 側で引数を解釈する
- 二重 `Initialize()` のガードはない（購読が重複する）。呼び出しは起動時1回に限定する

## 関連

- [ApplicationEvent](ApplicationEvent.md) — レジューム時再適用のイベント供給元（`OnResumeAsObservable`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance`）

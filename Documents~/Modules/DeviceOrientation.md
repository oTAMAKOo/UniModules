# DeviceOrientation

> **namespace**: `Modules.DeviceOrientation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/DeviceOrientation/`（`DeviceOrientationManagerBase.cs` の1ファイルのみ）
> **依存**: R3 / Extensions（`Singleton`）/ Modules.ApplicationEvent

## 概要

画面の向き（ScreenOrientation）の適用と監視を行う Singleton 基底クラス。
デフォルト実装は**横持ち固定（LandscapeLeft/Right の自動回転のみ許可）**。
レジューム時の再適用（OS 側で設定が戻るのを防ぐ）と、向き変更の Observable 通知を持つ。
主要クラス: `DeviceOrientationManagerBase<TInstance>`（abstract Singleton・非MonoBehaviour。向きの適用（virtual `Apply`）・毎フレーム監視・レジューム時再適用・変更通知）。利用側は空継承で基底のデフォルト（横持ち設定）をそのまま使うか、`Apply(ScreenOrientation?)` を override してポリシーを差し替える。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 現在の画面向きを知りたい | `DeviceOrientationManager.Instance.Orientation` |
| 画面向きの変更を購読したい | `OnOrientationChangedAsObservable()` |
| 向き設定を再適用したい | `Apply()` |
| 縦持ち対応など向きポリシーを変えたい | 利用側の派生クラスで `Apply(ScreenOrientation?)` を override |

## 使い方

- 起動時初期化: 派生クラスの `CreateInstance()` → `Initialize()`。画面向きは他の初期化に先立って確定させるため、起動シーケンスの最初に近い位置で呼ぶ
- デフォルトは横持ち（`LandscapeLeft`/`LandscapeRight` のみ許可 + `Screen.orientation = AutoRotation`）で、変更が不要ならそのまま使う

## 注意点・罠

- `Initialize()` 必須（呼ばないと Apply も監視も動かない）
- 監視は `ObserveEveryValueChanged` による毎フレームポーリング。変更のたびに `Apply(orientation)` が再実行される
- `OnOrientationChangedAsObservable()` は AutoRotation への変化は通知されない
- デフォルト `Apply` の引数 `orientation` は未使用（何を渡しても横持ち設定を適用）。縦横切替のような制御をしたければ override 側で引数を解釈する
- 二重 `Initialize()` のガードはない（購読が重複する）。呼び出しは起動時1回に限定する

## 関連

- [ApplicationEvent](ApplicationEvent.md) — レジューム時再適用のイベント供給元（`OnResumeAsObservable`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance`）

# ApplicationEvent

> **namespace**: `Modules.ApplicationEvent`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ApplicationEvent/`
> **Client側使用**: 約5ファイル（2026-07時点）
> **依存**: R3 / Extensions（`SingletonMonoBehaviour<T>`, `UnityUtility`）

## 概要

アプリのサスペンド（バックグラウンド移行）/ レジューム復帰 / 低メモリ警告 / 終了を **R3 Observable として配信**する基盤。
各クラスが Unity の `OnApplicationPause` / `OnApplicationQuit` / `Application.lowMemory` を個別に実装する必要はなく、**static メソッドの購読だけ**でイベントを受けられる（MonoBehaviour でないクラスからも購読可能）。
ハンドラ実体（GameObject）は起動時に InitializeObject が1つだけ常駐生成する。
主要クラス: `ApplicationEventHandler`（`SingletonMonoBehaviour<T>` 継承。Unity コールバックを static な R3 Observable に変換する常駐オブジェクト）の1クラスのみ。サスペンド重複ガードあり（サスペンド中の再サスペンド通知は無視、レジュームはサスペンド済みの時だけ通知）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| バックグラウンド移行（サスペンド）を検知したい | `ApplicationEventHandler.OnSuspendAsObservable()` |
| 復帰（レジューム）を検知したい | `ApplicationEventHandler.OnResumeAsObservable()`（中断していた秒数 double が流れる） |
| アプリ終了を検知したい | `ApplicationEventHandler.OnQuitAsObservable()` |
| メモリ不足警告を受けたい | `ApplicationEventHandler.OnLowMemoryAsObservable()` |
| 中断時にセーブデータを確実に書き込みたい | **OnSuspend と OnQuit の両方**を購読して Flush（SaveDataManager が実例） |
| 長時間中断後にサーバー再同期したい | OnResume の経過秒数で判定（SystemModel が実例） |

## 使い方

- **中断・終了時にセーブを即時書き込み（購読の定番形）**: `OnSuspendAsObservable` と `OnQuitAsObservable` の両方を購読して `Flush(true)`、`.AddTo(Disposable)` で寿命管理（実例: `Client/Assets/Scripts/Client/Core/SaveData/SaveDataManager.cs`）
- **復帰時のサーバー再同期**（`OnResumeAsObservable` の経過秒数で再同期要否を判定、エラー時はタイトルへ）: `Client/Assets/Scripts/Client/Model/System/SystemModel.cs`
- **終了時のリソース解放**（全サウンド解放）: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`
- **実体の生成**（起動時に1回だけ・通常は書かない）: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs` の `CreateApplicationEventHandler()`

## 注意点・罠

- **`OnApplicationPause` / `OnApplicationQuit` を各クラスに直書きしない**。本モジュールの購読に統一する（プロジェクトの「Unity ライフサイクルメソッド原則禁止」ルールとも整合。ハンドラ自身の Unity コールバック実装は既存基盤として例外）
- Subject は static のため購読自体はいつでも可能だが、**GameObject 実体が無いとイベントは発火しない**。実体は起動時に InitializeObject が生成済みなので、Client 実装で `CreateInstance()` を呼ぶ必要はない
- モバイルではサスペンドのまま OS にプロセスを kill され **`OnQuit` が来ないことがある**。永続化・確定処理は OnQuit だけでなく **OnSuspend でも**行う（SaveDataManager の Flush が実例）
- `OnResume` の経過秒数は `DateTime.Now`（端末時計）ベース。サーバー時間ではないので、厳密な時刻判定には `systemModel.LocalTime` 側の仕組みを使う
- static Subject は解放されないため、購読側は必ず `.AddTo(Disposable)` / `.AddTo(this)` で寿命管理する
- エディタ実行ではモバイル実機とコールバックの発火タイミングが異なる場合がある（実機で要確認）。SystemModel の復帰処理も `#if !UNITY_EDITOR` でエディタを除外している
- 常駐オブジェクトは `SceneManager`（Client側）の重複管理対象に登録されており、シーンを跨いで1つに保たれる

## 関連

- [Scene](Scene.md) — シーン遷移基盤。Client側 SceneManager が本ハンドラを重複管理対象に登録
- [LocalData](LocalData.md) — サスペンド/終了時に Flush されるローカル永続データ（SaveDataManager 経由）
- [Sound](Sound.md) — 終了時の全サウンド解放（InitializeObject.manager 参照）
- [Extensions/Core.md](../Extensions/Core.md) — `SingletonMonoBehaviour<T>`

# ApplicationEvent

アプリの中断・復帰・終了を通知する機能。
サスペンド、レジューム、低メモリ警告、終了を、どのクラスからでも購読して受け取れる。
起動時にハンドラの実体を 1 つ生成しておく必要がある。

## 基本情報

> **namespace**: `Modules.ApplicationEvent`
> **場所**: `Modules/ApplicationEvent/`（`ApplicationEventHandler.cs` の1ファイルのみ）
> **依存**: R3 / Extensions（`SingletonMonoBehaviour<T>`）

## 概要

アプリのサスペンド（バックグラウンド移行）/ レジューム復帰 / 低メモリ警告 / 終了を **R3 Observable として配信**する基盤。
各クラスが Unity の `OnApplicationPause` / `OnApplicationQuit` / `Application.lowMemory` を個別に実装する必要はなく、**static メソッドの購読だけ**でイベントを受けられる（MonoBehaviour でないクラスからも購読可能）。
ハンドラ実体（GameObject）は起動時に利用側で1つだけ常駐生成する（`CreateInstance()`）。
主要クラス: `ApplicationEventHandler`（sealed。`SingletonMonoBehaviour<T>` 継承。Unity コールバックを static な R3 Observable に変換する常駐オブジェクト）の1クラスのみ。サスペンド重複ガードあり（サスペンド中の再サスペンド通知は無視、レジュームはサスペンド済みの時だけ通知）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| バックグラウンド移行（サスペンド）を検知したい | `ApplicationEventHandler.OnSuspendAsObservable()` |
| 復帰（レジューム）を検知したい | `ApplicationEventHandler.OnResumeAsObservable()`（中断していた秒数 double が流れる） |
| アプリ終了を検知したい | `ApplicationEventHandler.OnQuitAsObservable()` |
| メモリ不足警告を受けたい | `ApplicationEventHandler.OnLowMemoryAsObservable()` |
| 中断時にセーブデータを確実に書き込みたい | **OnSuspend と OnQuit の両方**を購読して書き出す |
| 長時間中断後にサーバー再同期したい | OnResume の経過秒数で判定 |

## 使い方

- **中断・終了時にセーブを即時書き込み（購読の定番形）**: `OnSuspendAsObservable` と `OnQuitAsObservable` の両方を購読して書き出し、`.AddTo(Disposable)` で寿命管理
- **復帰時のサーバー再同期**: `OnResumeAsObservable` の経過秒数で再同期要否を判定
- **終了時のリソース解放**: `OnQuitAsObservable` を購読してサウンド・キャッシュ等を解放
- **実体の生成**（起動時に1回だけ）: `ApplicationEventHandler.CreateInstance()` を利用側の初期化フローから呼ぶ

## 注意点・罠

- **`OnApplicationPause` / `OnApplicationQuit` を各クラスに直書きしない**。本モジュールの購読に統一する（ハンドラ自身の Unity コールバック実装は基盤内の既存動作としてそのまま利用）
- Subject は static のため購読自体はいつでも可能だが、**GameObject 実体が無いとイベントは発火しない**。実体は `CreateInstance()` を起動時に一度だけ呼ぶ
- モバイルではサスペンドのまま OS にプロセスを kill され **`OnQuit` が来ないことがある**。永続化・確定処理は OnQuit だけでなく **OnSuspend でも**行う
- `OnResume` の経過秒数は `DateTime.Now`（端末時計）ベース。サーバー時間ではないので、厳密な時刻判定にはサーバー時刻ベースの仕組みを別途使う
- static Subject は解放されないため、購読側は必ず `.AddTo(Disposable)` / `.AddTo(this)` で寿命管理する
- エディタ実行ではモバイル実機とコールバックの発火タイミングが異なる場合がある（実機で要確認）
- `OnSuspend` / `OnResume` / `OnLowMemory` / `OnQuit` の各 Observable の型は `Observable<Unit>`（`OnResume` だけ `Observable<double>`）。完了（OnCompleted）は流れない
- `CreateInstance()` は GameObject を生成するだけで `DontDestroyOnLoad` を呼ばない。常駐オブジェクトはシーン跨ぎで1つに保つため、Scene モジュールの重複管理対象（`UniqueComponents`）に登録して使うのを推奨
- 低メモリ警告は `Awake` で `Application.lowMemory` に登録して受ける。登録は解除されないため、実体が複数回生成される（破棄して作り直す・シーンに重複して置く）と、その回数ぶん `OnLowMemory` が重複して流れる

## 関連

- [Scene](../Scene/Manual.md) — シーン遷移基盤。派生 SceneManager の重複管理対象に本ハンドラを登録
- [LocalData](../LocalData/Manual.md) — サスペンド/終了時に書き出すローカル永続データ
- [Sound](../Sound/Manual.md) — 終了時の全サウンド解放に利用
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `SingletonMonoBehaviour<T>`

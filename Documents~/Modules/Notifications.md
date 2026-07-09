# Notifications

> **namespace**: `Modules.Notifications`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Notifications/`（`LocalPushNotification.cs` + `.android.cs` / `.ios.cs` の partial 3ファイル）
> **依存**: R3 / Extensions（`Singleton`, `SecurePrefs`, UnixTime変換拡張）/ Modules.ApplicationEvent / Unity Mobile Notifications パッケージ（`Unity.Notifications.Android` / `Unity.Notifications.iOS`）

## 概要

ローカルプッシュ通知（OSスケジュール通知）の登録基盤。ジェネリック Singleton 基底 `LocalPushNotification<TInstance>` を継承して使う（`CurrentTime` の供給が abstract）。
「`Set()` でメモリに積む → **アプリのサスペンド/終了時にまとめて OS へ予約** → レジューム時に全消去」という方式で、フォアグラウンド中は OS に通知が残らない。
主要クラス: `LocalPushNotification<TInstance>`（通知の登録管理とサスペンド時の OS 予約）/ `Info`（通知1件のパラメータ。UnixTime/Title/Message 必須。生成時に SecurePrefs 連番で Identifier 採番）。
プラットフォーム別 partial（`#if (UNITY_ANDROID or UNITY_IOS) && !UNITY_EDITOR`）が `AddSchedule` / `ClearNotifications` を実装。

利用側は `LocalPushNotification<TInstance>` を継承し、`CurrentTime` にサーバー時刻等を供給する具象クラスを定義する。Android チャンネル登録は `RegisterChannel` を任意タイミングで呼ぶ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 通知を予約したい | `LocalPushNotification.Instance.Set(new Info(unixTime, title, message))` |
| 予約を取り消したい | `Remove(id)`（`Set` の戻り値IDを渡す） |
| 「サスペンド直前」に通知内容を組み立てたい | `OnNotifyRegisterAsObservable()` を購読して中で `Set()` |
| 通知のON/OFFを切り替えたい | `Enable = true / false`（OFF 時は既存予約もクリア） |
| Android の通知権限をリクエストしたい | `RequestNotificationPermission()`（Android専用 partial） |
| Android の通知チャンネルを登録したい | `RegisterChannel(channelId, title, importance, description)`（Android専用 partial） |

## 使い方

- 起動時初期化: 派生クラスの `CreateInstance()` → `Initialize()` → Android 実機は `RequestNotificationPermission().Forget()` → ユーザー設定に応じて `Enable` を設定
- Android チャンネル登録は起動フローの適切なタイミングで `RegisterChannel(...)` を呼ぶ
- サスペンド時に通知を組み立てて登録（標準パターン）: `OnNotifyRegisterAsObservable()` を購読 → 中で通知可否判定 → タイトル・本文を用意 → `new LocalPushNotification.Info(time, title, message)` を `Set()`
- 設定画面でのON/OFF: `LocalPushNotification.Instance.Enable = enable;`

## 注意点・罠

- **エディタでは OS 予約されない**（`#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`）。動作確認は実機のみ
- `Set()` は即時に OS へ予約しない。**実際の予約はサスペンド/終了時**（`Schedule()`）。またレジューム時に全予約がクリアされるため、通知は毎サスペンドごとに `OnNotifyRegisterAsObservable` 内で組み立て直すのが前提（積みっぱなしにしない。`Schedule()` 後に内部リストもクリアされる）
- `Enable = false` のまま `Set()` すると **-1 が返り登録されない**
- 発火時刻が `CurrentTime` より過去の場合はエラーログを出してスキップされる（例外にはならない）
- Android はチャンネル未登録だと通知が出ない（`RegisterChannel` は AddSchedule 前に必須）。iOS はバッジをレジューム時に 0 リセット

## 関連

- [ApplicationEvent](ApplicationEvent.md) — サスペンド/レジューム/終了イベントの供給元
- [LocalData](LocalData.md) — 通知ON/OFF設定の永続化に利用可
- [TextData](TextData.md) — 通知タイトル・本文の取得元
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SecurePrefs`

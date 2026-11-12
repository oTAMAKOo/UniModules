# Notifications

ローカルプッシュ通知を予約する機能。
アプリを閉じたときに OS へ通知をまとめて予約し、戻ったときに消すことができる。
iOS と Android の実機でのみ動き、利用側で継承したクラスを用意する。

## 基本情報

> **namespace**: `Modules.Notifications`
> **場所**: `Modules/Notifications/`（`LocalPushNotification.cs` + `.android.cs` / `.ios.cs` の partial 3ファイル）
> **依存**: R3 / UniTask（Android の partial）/ Extensions（`Singleton`, `SecurePrefs`, `UnixTimeToDateTime` 拡張）/ Modules.ApplicationEvent（`ApplicationEventHandler`）/ Unity Mobile Notifications パッケージ（`Unity.Notifications.Android` / `Unity.Notifications.iOS`）

## 概要

ローカルプッシュ通知（OSスケジュール通知）の登録基盤。ジェネリック Singleton 基底 `LocalPushNotification<Tinstance>` を継承して使う（`CurrentTime`（UnixTime の `ulong`）の供給が abstract）。
「`Set()` でメモリに積む → **アプリのサスペンド/終了時にまとめて OS へ予約** → レジューム時に全消去」という方式で、フォアグラウンド中は OS に通知が残らない。
主要クラス: `LocalPushNotification<Tinstance>`（通知の登録管理とサスペンド時の OS 予約）/ `Info`（`LocalPushNotification<Tinstance>` の入れ子クラス。通知1件のパラメータ。UnixTime/Title/Message 必須。任意で `BadgeCount`（既定 1）/ `SmallIconResource` / `LargeIconResource` / `Color`。生成時に SecurePrefs 連番で Identifier 採番）。
プラットフォーム別 partial（`.android.cs` は `#if UNITY_ANDROID && !UNITY_EDITOR`、`.ios.cs` は `#if UNITY_IOS && !UNITY_EDITOR`）が `AddSchedule` / `ClearNotifications` を実装。

利用側は `LocalPushNotification<Tinstance>` を継承し、`CurrentTime` にサーバー時刻等を供給する具象クラスを定義する。Android チャンネル登録は `RegisterChannel` を任意タイミングで呼ぶ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 通知を予約したい | `派生クラス.Instance.Set(new 派生クラス.Info(unixTime, title, message))`（戻り値は通知の ID。`long`） |
| 予約を取り消したい | `Remove(id)`（`Set` の戻り値IDを渡す） |
| 「サスペンド直前」に通知内容を組み立てたい | `OnNotifyRegisterAsObservable()` を購読して中で `Set()` |
| 通知のON/OFFを切り替えたい | `Enable = true / false`（値が変わったとき、OS に予約済み・表示済みの通知を消す） |
| Android の通知権限をリクエストしたい | `RequestNotificationPermission()`（Android専用 partial。結果は `OnRequestPermissionResult(status)` を override して受け取る） |
| Android の通知チャンネルを登録したい | `RegisterChannel(channelId, title, importance, description)`（Android専用 partial） |

## 使い方

- 起動時初期化: 派生クラスの `CreateInstance()` → `Initialize()` → Android 実機は `RequestNotificationPermission().Forget()` → ユーザー設定に応じて `Enable` を設定
- Android チャンネル登録は起動フローの適切なタイミングで `RegisterChannel(...)` を呼ぶ
- サスペンド時に通知を組み立てて登録（標準パターン）: `OnNotifyRegisterAsObservable()` を購読 → 中で通知可否判定 → タイトル・本文を用意 → `new 派生クラス.Info(time, title, message)` を `Set()`
- 設定画面でのON/OFF: `派生クラス.Instance.Enable = enable;`

## 注意点・罠

- **エディタでは OS 予約されない**（`#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`）。動作確認は実機のみ
- `Set()` は即時に OS へ予約しない。**実際の予約はサスペンド/終了時**（`Schedule()`）。またレジューム時に全予約がクリアされるため、通知は毎サスペンドごとに `OnNotifyRegisterAsObservable` 内で組み立て直すのが前提（積みっぱなしにしない。`Schedule()` 後に内部リストもクリアされる）
- `Enable = false` のまま `Set()` すると **-1 が返り登録されない**。`Remove()` も `Enable = false` のあいだは何もしない
- **`Initialize()` は `Enable` を false に戻す**。`Enable` の設定と `Set()` は `Initialize()` のあとに行う（`Initialize()` より前は通知のリストが作られていない）
- `Enable` を切り替えても、メモリに積んだ通知（`Set()` 済みで未予約のもの）は消えない。消えるのは OS 側の通知だけ
- 予約の時刻は「`Info.UnixTime` − `CurrentTime`」の差で決める（Android は端末の現在時刻に差を足す、iOS は差を待ち時間として渡す）
- **iOS の通知の許可を求める処理は本モジュールに無い**（`RequestNotificationPermission` は Android の partial だけ）。Mobile Notifications パッケージの設定か、利用側の処理で行う
- `RequestNotificationPermission` / `RegisterChannel` は Android 実機でしかコンパイルされない。呼び出し側も `#if UNITY_ANDROID && !UNITY_EDITOR` で囲む
- 発火時刻が `CurrentTime` より過去の場合はエラーログを出してスキップされる（例外にはならない）
- Android はチャンネル未登録だと通知が出ない（`RegisterChannel` は AddSchedule 前に必須）。iOS は通知を消すとき（レジューム時・予約の直前・`Enable` の切り替え時）にバッジを 0 に戻す
- Android のアイコンは `Info` の既定値が `notify_icon_small` / `notify_icon_large`。Mobile Notifications の設定（`ProjectSettings/NotificationsSettings.asset` の `DrawableResources`）に**同じ Id で登録**しないと解決されない（別 Id で登録する場合は `Info.SmallIconResource` / `LargeIconResource` を上書きする）

## 関連

- [ApplicationEvent](../ApplicationEvent/Manual.md) — サスペンド/レジューム/終了イベントの供給元
- [LocalData](../LocalData/Manual.md) — 通知ON/OFF設定の永続化に利用可
- [TextData](../TextData/Manual.md) — 通知タイトル・本文の取得元
- [Prefs](../Prefs/Manual.md) — `SecurePrefs`（通知 ID の連番の保存先）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Singleton<T>`

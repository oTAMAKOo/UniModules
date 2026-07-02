# Notifications

> **namespace**: `Modules.Notifications`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Notifications/`（`LocalPushNotification.cs` + `.android.cs` / `.ios.cs` の partial 3ファイル）
> **Client側使用**: using は1ファイル（2026-07時点）= 継承実装 `Dominion.Client.Notifications.LocalPushNotification`（`Client/Assets/Scripts/Client/Core/LocalPushNotification.cs`）。この Instance 経由で InitializeObject / GameStartupManager / NotifyModel / UserResourceModel / PushNotifyWindow が利用
> **依存**: R3 / Extensions（`Singleton`, `SecurePrefs`, UnixTime変換拡張）/ Modules.ApplicationEvent / Unity Mobile Notifications パッケージ（`Unity.Notifications.Android` / `Unity.Notifications.iOS`）

## 概要

ローカルプッシュ通知（OSスケジュール通知）の登録基盤。ジェネリック Singleton 基底 `LocalPushNotification<TInstance>` を継承して使う（`CurrentTime` の供給が abstract）。
「`Set()` でメモリに積む → **アプリのサスペンド/終了時にまとめて OS へ予約** → レジューム時に全消去」という方式で、フォアグラウンド中は OS に通知が残らない。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 通知を予約したい | `LocalPushNotification.Instance.Set(new Info(unixTime, title, message))` |
| 予約を取り消したい | `Remove(id)`（`Set` の戻り値IDを渡す） |
| 「サスペンド直前」に通知内容を組み立てたい | `OnNotifyRegisterAsObservable()` を購読して中で `Set()` |
| 通知のON/OFFを切り替えたい | `Enable = true / false`（OFF 時は既存予約もクリア） |
| Android の通知権限をリクエストしたい | `RequestNotificationPermission()`（Android専用 partial） |
| Android の通知チャンネルを登録したい | `RegisterChannel(channelId, title, importance, description)`（Android専用 partial） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `LocalPushNotification<TInstance>` | abstract Singleton（`Extensions.Singleton<T>` 継承） | 通知の登録管理とサスペンド時の OS 予約。`CurrentTime : ulong`（UnixTime）が abstract |
| `LocalPushNotification<TInstance>.Info` | sealed class | 通知1件のパラメータ（UnixTime/Title/Message 必須。BadgeCount/アイコン/Color はオプション）。生成時に SecurePrefs 連番で Identifier 採番 |
| Client側 `Dominion.Client.Notifications.LocalPushNotification` | sealed（Client実装） | `CurrentTime` に `SystemModel.CurrentUnixTime`（サーバー時刻）を供給。`Setup()` で Android チャンネル登録 |

プラットフォーム別 partial（`#if (UNITY_ANDROID or UNITY_IOS) && !UNITY_EDITOR`）が `AddSchedule` / `ClearNotifications` を実装。

## 使い方(実例)

### 実例1: 起動時初期化（CreateInstance → Initialize → 権限 → Enable）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
private void InitializeLocalPushNotify()
{
    var localPushNotification = LocalPushNotification.CreateInstance();

    localPushNotification.Initialize();

    #if UNITY_ANDROID && !UNITY_EDITOR

    localPushNotification.RequestNotificationPermission().Forget();

    #endif

    var playData = LocalDataManager.Get<PlayData>();

    localPushNotification.Enable = playData.LocalPushNotify.EnableNotify;
}
```

Android チャンネル登録はログイン後の `GameStartupManager.Setup()` → `localPushNotification.Setup()` で実施（`Client/Assets/Scripts/Client/Manager/GameStartupManager.cs`）。

### 実例2: サスペンド時に通知を組み立てて登録（標準パターン）

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/NotifyModel.cs（抜粋）
localPushNotification.OnNotifyRegisterAsObservable()
    .Subscribe(_ => NotifyRegister())
    .AddTo(Disposable);

private void NotifyRegister()
{
    if (!localPushNotification.CheckNotificationEnable()) { return; }

    var title = TextData.Get(TextData.LocalPushNotification.Notification_Title);
    var message = messageTable.SampleOne();
    var time = notifyDatetime.ToUnixTime();

    var userRetentionNotify = new LocalPushNotification.Info(time, title, message);

    localPushNotification.Set(userRetentionNotify);
}
```

### 実例3: 設定画面でのON/OFF

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Window/PushNotify/PushNotifyWindow.cs（抜粋）
var localPushNotify = LocalPushNotification.Instance;

localPushNotify.Enable = enable;
```

## API(主要公開メンバー)

### LocalPushNotification&lt;TInstance&gt;

| メンバー | 説明 |
|---|---|
| `Initialize()` | ApplicationEvent（OnQuit/OnSuspend → Schedule、OnResume → Clear）を購読。起動時に1回必須 |
| `Enable : bool` | 通知の有効/無効。値が変わると既存の OS 予約をクリア |
| `Set(Info info) : long` | 通知登録（メモリ上）。戻り値は ID。**disable 時は -1** |
| `Remove(long id)` | 登録解除（-1 は無視） |
| `OnNotifyRegisterAsObservable() : Observable<Unit>` | サスペンド/終了時「OS 予約直前」の通知。ここで各機能が `Set()` する |
| `abstract CurrentTime : ulong` | 現在 UnixTime の供給（Client実装はサーバー時刻） |
| `RequestNotificationPermission() : UniTask` | 【Android専用】通知権限リクエスト（結果は `OnRequestPermissionResult` virtual） |
| `RegisterChannel(channelId, title, Importance, description)` | 【Android専用】通知チャンネル登録。**AddSchedule 前に必須** |

### Info（通知1件）

| メンバー | 説明 |
|---|---|
| `Info(ulong unixTime, string title, string message)` | 発火時刻（UnixTime）・タイトル・本文。Identifier は自動採番（SecurePrefs 永続連番） |
| `BadgeCount : int` | バッジ数（デフォルト 1） |
| `LargeIconResource / SmallIconResource : string` | 【Android】アイコンリソース名（デフォルト "notify_icon_large" / "notify_icon_small"） |
| `Color : Color32?` | 【Android】通知色 |

## 注意点・罠

- **エディタでは OS 予約されない**（`#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`）。動作確認は実機のみ
- `Set()` は即時に OS へ予約しない。**実際の予約はサスペンド/終了時**（`Schedule()`）。またレジューム時に全予約がクリアされるため、通知は毎サスペンドごとに `OnNotifyRegisterAsObservable` 内で組み立て直すのが前提（積みっぱなしにしない。`Schedule()` 後に内部リストもクリアされる）
- `Enable = false` のまま `Set()` すると **-1 が返り登録されない**。Client側は `CheckNotificationEnable()`（マスターロード済み・ログイン済み・ユーザー設定ON）で事前判定している
- 発火時刻が `CurrentTime` より過去の場合はエラーログを出してスキップされる（例外にはならない）
- テキストは必ず `TextData` から取得する（コーディング規約。実例2参照）
- Android はチャンネル未登録だと通知が出ない（`Setup()` がログイン後に呼ばれる点に注意）。iOS はバッジをレジューム時に 0 リセット

## 関連

- [ApplicationEvent](ApplicationEvent.md) — サスペンド/レジューム/終了イベントの供給元
- [LocalData](LocalData.md) — 通知ON/OFF設定（`PlayData.LocalPushNotify`）の永続化
- [TextData](TextData.md) — 通知タイトル・本文の取得元（`TextData.LocalPushNotification.*`）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SecurePrefs`

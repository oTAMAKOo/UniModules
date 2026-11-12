# Vivox

ボイスチャットとテキストチャットの機能。
ログイン、チャンネルへの参加と退出、メッセージの送受信、位置に応じた音声の更新ができる。
Vivox SDK の導入と ENABLE_VIVOX シンボルの定義が必要。

## 基本情報

> **namespace**: `Modules.Vivox`
> **場所**: `Modules/Vivox/`
> **依存**: VivoxUnity SDK（全コードが `#if ENABLE_VIVOX`。利用側で定義） / UniTask / R3 / Extensions（`Singleton<T>`） / Modules.Devkit.Console

## 概要

Vivox（ボイス・テキストチャットサービス）SDK のラッパー `VivoxManager`。ログイン、チャンネル参加/退出、テキスト送受信、3D ポジショナルボイス、参加者イベントの R3 Observable 化を提供する。

全ファイルが `#if ENABLE_VIVOX` で囲まれており、利用側で `ENABLE_VIVOX` シンボルが未定義の場合はコンパイル対象外になる。CriWare（[CriWare](../CriWare/Manual.md)）と同じ「SDK 導入 + シンボル定義で有効化する」条件付きコンパイルモジュール。

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `VivoxManager` | Singleton（`Extensions.Singleton<T>`、partial 3ファイル構成） | 全機能の窓口。`VivoxManager.cs`（接続・チャンネル・参加者）/ `.message.cs`（テキスト送受信）/ `.voice.cs`（3Dボイス・デバイス） |
| `VivoxManager.ConnectType` | enum | `TextOnly` / `AudioOnly` / `TextAndAudio` |
| `VivoxManager.ParticipantStatusChangedData` / `ParticipantValueChangedData` / `ParticipantValueUpdatedData` | class | 参加者イベントのデータ型（入退室 / 発話検知の bool / 音量の double） |
| `VivoxManager.PositionalData` | struct | 位置情報（`speakerPos` / `listenerPos` / `listenerAtOrient` / `listenerUpOrient`） |

## API(主要公開メンバー・概要のみ)

| メンバー | 説明 |
|---|---|
| `void Setup(domain, issuer, server, token)` | 接続パラメータ設定（最初に呼ぶ） |
| `void SetAccount(uniqueId, displayName)` → `UniTask<bool> Login()` / `bool Logout()` | ログイン / ログアウト |
| `AccountId GetAccount(uniqueId, displayName)` | 送信先などに使う `AccountId` を作る |
| `LoginSession` / `LoginState` / `ActiveChannels` | 現在のログインセッション・ログイン状態・参加中のチャンネル |
| `void Release()` | クライアントを解放する |
| `ChannelId CreateChannelId(name, ChannelType, ...)` → `UniTask<bool> JoinChannel(channelId, ConnectType, ...)` | チャンネル参加 |
| `bool LeaveChannel(channelName)` / `void DisconnectAllChannels()` | 退出 |
| `UniTask SendMessage(channel, message, ...)` / `UniTask SendDirectedMessage(account, message, ...)` | チャンネル / ダイレクトメッセージ送信 |
| `SetPositionalData(PositionalData)` + `BeginPositionalUpdate()` | 3D ポジショナルボイスの位置更新（`UpdatePositionInterval` 秒間隔・既定1秒、音声接続中の Positional チャンネルのみ。前回と同じ位置なら送らない） |
| `OnRequestUpdatePositionalAsObservable()` / `CurrentPositional` | 位置更新の直前に発火するイベント（ここで `SetPositionalData` を呼ぶ）/ 現在の位置情報 |
| `AudioInputDevices` / `AudioOutputDevices` | 入出力デバイス（`IAudioDevices`） |
| `On(LoggingIn/LoggedIn/LoggingOut/LoggedOut/RecoveryStateChanged)AsObservable()` | ログイン状態イベント |
| `On(ChannelConnecting/Connected/Disconnecting/Disconnected)AsObservable()` | チャンネル状態イベント |
| `On(Added/Removed/Detected/AudioEnergyChanged)ParticipantAsObservable()` | 参加者の入退室・発話検知・音量イベント |
| `OnReceivedMessageAsObservable()` / `OnReceivedDirectedMessageAsObservable()` | テキスト受信イベント |

## 注意点・罠

- 使用するには VivoxUnity SDK 導入 + `ENABLE_VIVOX` シンボル定義が必要
- `Singleton<T>` 継承のため `VivoxManager.Instance` 初回アクセスで生成され、`OnCreate` で `Client.Initialize()` が走る（`Application.quitting` で自動 Release）
- `Login()` / `JoinChannel()` は Begin/End コールバックを `UniTask.WaitWhile` でポーリング待機する実装（キャンセレーショントークン非対応）
- ログは `UnityConsole`（イベント名 "Vivox"）経由のため**本番ビルドでは出力されない**（[Devkit](../Devkit/Manual.md) 参照）
- チャンネルのメッセージは `OnReceivedMessageAsObservable()`、ダイレクトメッセージは `OnReceivedDirectedMessageAsObservable()` に流れる。購読していない側の通知は捨てられる
- `JoinChannel` / `SendMessage` / `SendDirectedMessage` / `BeginPositionalUpdate` は `Login()` 済みが前提（`loginSession` が null だと NullReference）。`LeaveChannel` も未ログインではエラーログの組み立てで NullReference になる
- ログイン・チャンネル接続のトークンの有効期限は90秒固定（`TokenExpirationDuration`）

## 関連

- [CriWare](../CriWare/Manual.md) — 同じく「SDK 導入 + シンボル定義で有効化」する条件付きコンパイルモジュールの前例
- [Devkit](../Devkit/Manual.md) — ログ出力先の `UnityConsole`
- [Sound](../Sound/Manual.md) — ゲーム内サウンド再生（ボイスチャットとは別系統）

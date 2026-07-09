# Vivox

> **namespace**: `Modules.Vivox`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Vivox/`
> **依存**: VivoxUnity SDK / UniTask / R3 / Extensions（`Singleton<T>`） / Modules.Devkit.Console

## 概要

Vivox（ボイス・テキストチャットサービス）SDK のラッパー `VivoxManager`。ログイン、チャンネル参加/退出、テキスト送受信、3D ポジショナルボイス、参加者イベントの R3 Observable 化を提供する。

全ファイルが `#if ENABLE_VIVOX` で囲まれており、利用側で `ENABLE_VIVOX` シンボルが未定義の場合はコンパイル対象外になる。CriWare（[CriWare](CriWare.md)）と同じ「SDK 導入 + シンボル定義で有効化する」休眠モジュール。

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `VivoxManager` | Singleton（`Extensions.Singleton<T>`、partial 3ファイル構成） | 全機能の窓口。`VivoxManager.cs`（接続・チャンネル・参加者）/ `.message.cs`（テキスト送受信）/ `.voice.cs`（3Dボイス・デバイス） |
| `VivoxManager.ConnectType` | enum | `TextOnly` / `AudioOnly` / `TextAndAudio` |
| `VivoxManager.ParticipantStatusChangedData` ほか | class / struct | 参加者イベント・位置情報（`PositionalData`）のデータ型 |

## API(主要公開メンバー・概要のみ)

| メンバー | 説明 |
|---|---|
| `void Setup(domain, issuer, server, token)` | 接続パラメータ設定（最初に呼ぶ） |
| `void SetAccount(uniqueId, displayName)` → `UniTask<bool> Login()` / `bool Logout()` | ログイン / ログアウト |
| `ChannelId CreateChannelId(name, ChannelType, ...)` → `UniTask<bool> JoinChannel(channelId, ConnectType, ...)` | チャンネル参加 |
| `bool LeaveChannel(channelName)` / `void DisconnectAllChannels()` | 退出 |
| `UniTask SendMessage(channel, message, ...)` / `UniTask SendDirectedMessage(account, message, ...)` | チャンネル / ダイレクトメッセージ送信 |
| `SetPositionalData(PositionalData)` + `BeginPositionalUpdate()` | 3D ポジショナルボイスの位置更新（`UpdatePositionInterval` 秒間隔、Positional チャンネルのみ） |
| `AudioInputDevices` / `AudioOutputDevices` | 入出力デバイス（`IAudioDevices`） |
| `On(LoggingIn/LoggedIn/LoggingOut/LoggedOut/RecoveryStateChanged)AsObservable()` | ログイン状態イベント |
| `On(ChannelConnecting/Connected/Disconnecting/Disconnected)AsObservable()` | チャンネル状態イベント |
| `On(Added/Removed/Detected/AudioEnergyChanged)ParticipantAsObservable()` | 参加者の入退室・発話検知・音量イベント |
| `OnReceivedMessageAsObservable()` / `OnReceivedDirectedMessageAsObservable()` | テキスト受信イベント |

## 注意点・罠

- 使用するには VivoxUnity SDK 導入 + `ENABLE_VIVOX` シンボル定義が必要
- `Singleton<T>` 継承のため `VivoxManager.Instance` 初回アクセスで生成され、`OnCreate` で `Client.Initialize()` が走る（`Application.quitting` で自動 Release）
- `Login()` / `JoinChannel()` は Begin/End コールバックを `UniTask.WaitWhile` でポーリング待機する実装（キャンセレーショントークン非対応）
- ログは `UnityConsole`（イベント名 "Vivox"）経由のため**本番ビルドでは出力されない**（[Devkit](Devkit.md) 参照）
- `SendDirectedMessage` の受信通知は `onReceivedMessage` の null チェック後に `onReceivedDirectedMessage.OnNext` を呼ぶ実装（`OnDirectedMessagLogRecieved`）。Directed 購読だけして Channel 購読が無いと NRE の可能性がある既知の癖

## 関連

- [CriWare](CriWare.md) — 同じく「SDK 導入 + シンボル定義で有効化」する休眠モジュールの前例
- [Devkit](Devkit.md) — ログ出力先の `UnityConsole`
- [Sound](Sound.md) — ゲーム内サウンド再生（ボイスチャットとは別系統）

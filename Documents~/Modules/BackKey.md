# BackKey

> **namespace**: `Modules.BackKey`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/BackKey/`（BackKeyManager.cs + `Receiver/` BackKeyReceiver.cs / WindowBackKeyReceiver.cs / ButtonBackKeyReceiver.cs）
> **Client側使用**: using は1ファイル（2026-07時点）= `InitializeObject.manager.cs`（初期化のみ）。ほかに Receiver 派生2クラス（`Client/Assets/Scripts/Client/Core/BackKey/`）。実利用はウィンドウ経由の間接使用（WindowBase が自動付与）
> **依存**: R3 / Extensions（`Singleton`, `IsEmpty`, `UnityUtility`）/ UniTask + Modules.Window（WindowBackKeyReceiver）/ Modules.UI.Extension（ButtonBackKeyReceiver の `UIButton`）

## 概要

Android の戻るキー（Escape キー）ハンドリング基盤。`BackKeyManager` がキー押下を監視し、登録された `BackKeyReceiver` 群を **Priority 降順**に呼び出して、最初に `true` を返した Receiver で処理を打ち切る（チェーン・オブ・レスポンシビリティ）。
本プロジェクトでの主用途は「戻るキーで最前面のポップアップを閉じる」で、[Window](Window.md) の `WindowBase` が Receiver を自動付与するため**通常は意識せずに使える**。
主要クラス: `BackKeyManager`（sealed Singleton・非MonoBehaviour。キー監視 → Receiver を Priority 降順に実行）/ `BackKeyReceiver`（abstract MonoBehaviour。OnEnable/OnDisable で Manager へ自動登録/解除。`HandleBackKey() : bool` が実装ポイント）/ `WindowBackKeyReceiver`（`PopupManager.Current` が自分の Window のときだけ `window.Close()`。Priority 10000）/ `ButtonBackKeyReceiver`（付与された `UIButton` のクリックをレイキャストエミュレートで発火）。

Client側派生（`Client/Assets/Scripts/Client/Core/BackKey/`）:

- `Dominion.Client.WindowBackKeyReceiver` — チュートリアル中は無効化 + `PopupManager.Instance` を供給。**`WindowBase.OnOpen` が `GetOrAddComponent` で自動付与**（手動アタッチ不要）
- `Dominion.Client.ButtonBackKeyReceiver` — チュートリアル中無効化のみ。**定義のみでプレハブ・シーンへの適用実績なし**（2026-07時点）

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 戻るキーでウィンドウを閉じたい | 何もしない（`WindowBase.ReceiveBackKeyEvent` デフォルト true で自動対応） |
| 特定ウィンドウだけ戻るキー無効にしたい | `window.ReceiveBackKeyEvent = false;`（WindowBase・インスペクタでも設定可） |
| 戻るキーで任意処理をしたい | `BackKeyReceiver` を継承し `HandleBackKey()` を実装（GameObject に付けるだけで自動登録） |
| 戻るキーでボタンを押した扱いにしたい | `ButtonBackKeyReceiver` 継承（Client側 `Dominion.Client.ButtonBackKeyReceiver` が既存。ただし現状未使用） |
| 処理順を制御したい | `Priority` プロパティ（大きいほど先に処理。Window用は 10000） |
| 全 Receiver を無効化したい | `BackKeyManager.Instance.ClearReceiver()` |

## 使い方

- 起動時初期化: `BackKeyManager.CreateInstance()` → `Initialize()`（引用元: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`）
- ウィンドウへの自動付与（間接使用の実体）: `WindowBase.OnOpen` が `receiveBackKeyEvent` 有効時に `UnityUtility.GetOrAddComponent<WindowBackKeyReceiver>(gameObject)` を実行（引用元: `Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs`）
- Client側 Receiver 実装（チュートリアル中は `HandleBackKey` で false を返して無効化 + `GetPopupManager()` で `PopupManager.Instance` を供給）: `Client/Assets/Scripts/Client/Core/BackKey/WindowBackKeyReceiver.cs`

## 注意点・罠

- **`#if UNITY_ANDROID` のみ有効**。iOS・Standalone では何も起きない（エディタでも Android ビルドターゲット時のみキー判定が動く。エディタでは Escape キーで発火）
- `BackKeyReceiver` は **OnEnable/OnDisable で自動登録/解除**する既存実装（Unity ライフサイクル使用）。`BackKeyManager.Initialize()` 前に OnEnable が走ると `receivers` が null で例外になるため、Manager 初期化は起動シーケンス冒頭で済ませてある
- ウィンドウ用途で自前実装は不要。**`WindowBase` 派生なら自動対応**（`ReceiveBackKeyEvent` で個別 OFF 可）。反応するのは `PopupManager.Current`（最前面）のウィンドウだけ
- `WindowBackKeyReceiver` の Priority は 10000 固定（OnInitialize で上書き）。これより優先したい Receiver は 10001 以上にする
- 同一 Priority の実行順は登録順に依存する（`List.Sort` は不安定ソートのため保証なし）
- `ButtonBackKeyReceiver` は `targetGraphic` 未設定のボタンでは動かない。また実際に EventSystem のレイキャストを行うため、別 UI に遮られていると発火しない（仕様）
- 旧 InputSystem（`Input.GetKeyDown`）前提

## 関連

- [Window](Window.md) — `WindowBase` / `PopupManager`（`IPopupManager` 経由で連携。戻るキー→最前面ウィンドウ Close の実体）
- [UI](UI.md) — `UIButton`（ButtonBackKeyReceiver の対象）
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>`

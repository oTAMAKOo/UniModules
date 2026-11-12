# BackKey

Android の戻るキーを処理する機能。
戻るキーで最前面のウィンドウを閉じる、ボタンを押した扱いにする、任意の処理を優先度順に呼ぶ、ができる。
Android のビルドターゲットでのみ動く。

## 基本情報

> **namespace**: `Modules.BackKey`
> **場所**: `Modules/BackKey/`（BackKeyManager.cs + `Receiver/` BackKeyReceiver.cs / WindowBackKeyReceiver.cs / ButtonBackKeyReceiver.cs）
> **依存**: R3 / Extensions（`Singleton`, `IsEmpty`, `UnityUtility`）/ UniTask + Modules.Window（WindowBackKeyReceiver）/ Modules.UI.Extension（ButtonBackKeyReceiver の `UIButton`）

## 概要

Android の戻るキー（Escape キー）ハンドリング基盤。`BackKeyManager` がキー押下を監視し、登録された `BackKeyReceiver` 群を **Priority 降順**に呼び出して、最初に `true` を返した Receiver で処理を打ち切る（チェーン・オブ・レスポンシビリティ）。
主用途は「戻るキーで最前面のポップアップを閉じる」で、`WindowBackKeyReceiver` を利用側で自動付与すれば意識せずに使える。
主要クラス: `BackKeyManager`（sealed Singleton・非MonoBehaviour。キー監視 → Receiver を Priority 降順に実行）/ `BackKeyReceiver`（abstract MonoBehaviour。OnEnable/OnDisable で Manager へ自動登録/解除。`HandleBackKey() : bool` が実装ポイント）/ `WindowBackKeyReceiver`（abstract。`PopupManager.Current` が自分の Window のときだけ `window.Close()`。Priority 10000）/ `ButtonBackKeyReceiver`（abstract。同じ GameObject の `UIButton` のクリックをレイキャストエミュレートで発火）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 戻るキーでウィンドウを閉じたい | `WindowBackKeyReceiver` の派生（`GetPopupManager()` を実装）を Window と同じ GameObject へ付与（`GetOrAddComponent` パターン） |
| 戻るキーで任意処理をしたい | `BackKeyReceiver` を継承し `HandleBackKey()` を実装（GameObject に付けるだけで自動登録。処理したら true、次の Receiver へ回すなら false を返す） |
| 戻るキーでボタンを押した扱いにしたい | `ButtonBackKeyReceiver` の派生を `UIButton` と同じ GameObject へ付与（`RequireComponent(typeof(UIButton))`） |
| 処理順を制御したい | `Priority` プロパティ（大きいほど先に処理。既定 0、SerializeField。Window用は 10000） |
| 登録済みの Receiver をすべて外したい | `BackKeyManager.Instance.ClearReceiver()`（外れた Receiver は次に有効化されるまで再登録されない） |
| 手動で登録・解除したい | `BackKeyManager.Instance.AddReceiver(receiver)` / `RemoveReceiver(receiver)` |

## 使い方

- 起動時初期化: `BackKeyManager.CreateInstance()` → `Initialize()` を起動シーケンスの冒頭で実施
- ウィンドウへの自動付与: Window の `OnOpen` 等で `UnityUtility.GetOrAddComponent<WindowBackKeyReceiver>(gameObject)` を実行するのが定石
- `WindowBackKeyReceiver` の派生: abstract の `GetPopupManager()` を実装して `PopupManager` 派生の `Instance` を供給。無効化条件（チュートリアル中など）は `HandleBackKey` で false を返して対応

## 注意点・罠

- **`#if UNITY_ANDROID` のみ有効**。iOS・Standalone では何も起きない（エディタでも Android ビルドターゲット時のみキー判定が動く。エディタでは Escape キーで発火）
- `BackKeyReceiver` は **OnEnable/OnDisable で自動登録/解除**する既存実装（Unity ライフサイクル使用）。`BackKeyManager.Initialize()` 前に OnEnable が走ると `receivers` が null で例外になるため、Manager 初期化は起動シーケンス冒頭で済ませる必要がある
- 反応するのは `PopupManager.Current`（最前面）のウィンドウだけ
- `WindowBackKeyReceiver` の Priority は 10000 固定（OnInitialize で上書き）。これより優先したい Receiver は 10001 以上にする
- 同一 Priority の実行順は保証されない（`List.Sort` は不安定ソートのため、登録順どおりになるとは限らない）
- **並べ替えは登録・解除のときだけ行われる**。登録済みの Receiver の `Priority` を変えても、次にいずれかの Receiver が登録・解除されるまで順番に反映されない
- キーの判定は毎フレームの PostLateUpdate で行われる
- `ButtonBackKeyReceiver` は `targetGraphic` 未設定のボタン・`interactable` が false のボタンでは動かない（false を返して次の Receiver へ回る）。また実際に EventSystem のレイキャストを行うため、別 UI に遮られていると発火しない（仕様）
- 旧 InputSystem（`Input.GetKeyDown`）前提

## 関連

- [Window](../Window/Manual.md) — `Window` / `PopupManager`（`IPopupManager` 経由で連携。戻るキー→最前面ウィンドウ Close の実体）
- [UI](../UI/Manual.md) — `UIButton`（ButtonBackKeyReceiver の対象）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Singleton<T>`

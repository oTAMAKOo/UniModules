# UniTaskExtension

UniTask を起動の早い段階で初期化する機能。
シーン読み込み前に走る初期化処理の中でも UniTask を使えるようにする。
自動で実行され、コードから呼び出すものは無い。

## 基本情報

> **namespace**: `Modules.UniTaskExtension`
> **場所**: `Modules/UniTaskExtension/`（`UniTaskInitializer.cs` の1ファイルのみ）
> **依存**: UniTask ライブラリ本体（`PlayerLoopHelper`）

## 概要

UniTask の PlayerLoop 初期化（`PlayerLoopHelper.Initialize`）を、**`AfterAssembliesLoaded`** タイミングで実行するためだけのモジュール。ファイルは `UniTaskInitializer.cs` の1つのみ（`UniTaskInitializer.OnAfterAssembliesLoaded()` が `[RuntimeInitializeOnLoadMethod]` で自動実行される。手動呼び出し不要）。
UniTask 本体の自己初期化が `BeforeSceneLoad` で走る環境でも、他の `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` メソッド（実行順不定）の中で UniTask を安全に使える。
初期化には現在の PlayerLoop（`PlayerLoop.GetCurrentPlayerLoop()`。`UNITY_2019_3_OR_NEWER` でない環境では `GetDefaultPlayerLoop()`）を渡す。
**アプリケーションコードから呼び出すクラスは無い**。「UniTask を使う」実装の入口は `using Cysharp.Threading.Tasks;`（ライブラリ本体）と `Extensions.UniTaskExtensions`（[Extensions/Methods.md](../../Documents~/Extensions/Methods.md)）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 内で UniTask を使いたい | 何もしなくてよい（本モジュールが `AfterAssembliesLoaded` で初期化済み） |
| 非同期処理を書きたい | `using Cysharp.Threading.Tasks;` の `UniTask` |
| fire-and-forget したい | `task.Forget()`（必須）。GameObject 寿命に紐付けるなら `task.Forget(component)` / `task.Forget(gameObject)`（`Extensions.UniTaskExtensions`） |
| Observable ⇔ UniTask 変換したい | `observable.ToUniTask()`（[Extensions/Methods.md](../../Documents~/Extensions/Methods.md)）/ `ObservableEx.FromUniTask`（[R3Extension](../R3Extension/Manual.md)） |

## 注意点・罠

- **`Extensions.UniTaskExtensions`（`Extensions/Methods/UniTaskExtensions.cs`）とは別物**。あちらは Observable⇔UniTask 変換・`Forget(component)` 等の拡張メソッド群（[Extensions/Methods.md](../../Documents~/Extensions/Methods.md)）。本モジュールは初期化のみ
- UniTask 本体にも `[RuntimeInitializeOnLoadMethod]` の自己初期化がある。そのタイミングは UniTask の版と Unity のバージョンで決まり（`BeforeSceneLoad` または `AfterAssembliesLoaded`）、本モジュールはどちらの場合でも `AfterAssembliesLoaded` で初期化する。**`AfterAssembliesLoaded` より前**（`SubsystemRegistration` 等）で UniTask を使うコードを書く場合は、さらに先に `PlayerLoopHelper.Initialize` を呼ぶ必要がある
- R3 のデフォルト TimeProvider/FrameProvider 登録（`UnityProviderInitializer`）も同じ `AfterAssembliesLoaded` で走る（順序は不定）。起動最初期に R3 の時間系オペレータと UniTask を組み合わせる場合はタイミングに注意

## 関連

- [R3Extension](../R3Extension/Manual.md) — `ObservableEx.FromUniTask`（UniTask → Observable 変換）/ `AsyncHandler`（イベント発火側が UniTask で購読者完了を待つ）
- [Extensions/Methods.md](../../Documents~/Extensions/Methods.md) — `UniTaskExtensions`（`ToUniTask` / `Forget(component)` / `TakeUntilDestroy` 等の実用拡張）

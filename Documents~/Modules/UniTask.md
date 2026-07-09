# UniTask

> **namespace**: `Modules.UniTaskExtension`（**フォルダ名 `UniTask/` と不一致**。実コードで確認済み）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/UniTask/`
> **依存**: UniTask ライブラリ本体

## 概要

UniTask の PlayerLoop 初期化を、ライブラリ標準（`BeforeSceneLoad`）より早い **`AfterAssembliesLoaded`** タイミングで実行するためだけのモジュール。ファイルは `UniTaskInitializer.cs` の1つのみ（`[RuntimeInitializeOnLoadMethod]` による自動実行。手動呼び出し不要）。
これにより、他の `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` メソッド（実行順不定）の中でも UniTask を安全に使える。
**アプリケーションコードから呼び出すクラスは無い**。「UniTask を使う」実装の入口は `using Cysharp.Threading.Tasks;`（ライブラリ本体）と `Extensions.UniTaskExtensions`（[Extensions/Methods.md](../Extensions/Methods.md)）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 内で UniTask を使いたい | 何もしなくてよい（本モジュールが `AfterAssembliesLoaded` で初期化済み） |
| 非同期処理を書きたい | `using Cysharp.Threading.Tasks;` の `UniTask` |
| fire-and-forget したい | `task.Forget()`（必須）。GameObject 寿命に紐付けるなら `task.Forget(component)`（`Extensions.UniTaskExtensions`） |
| Observable ⇔ UniTask 変換したい | `observable.ToUniTask()`（[Extensions/Methods.md](../Extensions/Methods.md)）/ `ObservableEx.FromUniTask`（[R3Extension](R3Extension.md)） |

## 注意点・罠

- **namespace はフォルダ名と不一致**: `Modules.UniTask` ではなく `Modules.UniTaskExtension`。grep 時に注意
- **`Extensions.UniTaskExtensions`（`Extensions/Methods/UniTaskExtensions.cs`）とは別物**。あちらは Observable⇔UniTask 変換・`Forget(component)` 等の拡張メソッド群（[Extensions/Methods.md](../Extensions/Methods.md)）。本モジュールは初期化のみ
- UniTask 本体にも `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` の自己初期化があるが、本モジュールはそれより早い `AfterAssembliesLoaded` で先回りしている。**`AfterAssembliesLoaded` より前**（`SubsystemRegistration` 等）で UniTask を使うコードを書く場合は、さらに先に `PlayerLoopHelper.Initialize` を呼ぶ必要がある
- R3 のデフォルト TimeProvider/FrameProvider 登録（`UnityProviderInitializer`）も同じ `AfterAssembliesLoaded` で走る（順序は不定）。起動最初期に R3 の時間系オペレータと UniTask を組み合わせる場合はタイミングに注意

## 関連

- [R3Extension](R3Extension.md) — `ObservableEx.FromUniTask`（UniTask → Observable 変換）/ `AsyncHandler`（イベント発火側が UniTask で購読者完了を待つ）
- [Extensions/Methods.md](../Extensions/Methods.md) — `UniTaskExtensions`（`ToUniTask` / `Forget(component)` / `TakeUntilDestroy` 等の実用拡張）

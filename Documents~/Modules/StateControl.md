# StateControl

> **namespace**: `Modules.StateControl`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/StateControl/`（`StateController.cs` / `StateNode.cs` の2ファイル）
> **Client側使用**: 0ファイル（基盤内使用も0、2026-07時点）
> **依存**: UniTask / R3 / Extensions（`LifetimeDisposable`）

## 概要

enum をキーにした非同期ステートマシン。ステートごとに `Enter` / `Leave` を `UniTask` で実装したノードを登録し、`Request` で遷移する（前ステートの `Leave` 完了 → 次ステートの `Enter` 実行）。遷移引数（`StateArgument`）の受け渡しと、遷移開始/完了の R3 通知を持つ。

主要クラス: `StateController<T>`（本体。ノード登録・遷移実行・キャンセル管理・遷移通知）/ `StateNode<T>`（引数なしノード。`Enter` / `Leave` を override）/ `StateNode<T, TArgument>`（引数付きノード）/ `StateArgument`（遷移引数の基底）。

**本プロジェクトでは未使用**（ガード無しでコンパイルはされている）。画面遷移・ゲームフロー等でステートマシンが必要になった場合は、自作する前に本クラスの採用を検討すること。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| enumベースのステートマシンが欲しい | `StateController<TEnum>` を生成 → `Register(state, node)` → `Request(state)` |
| ステート処理を書く | `StateNode<T>` 継承で `Enter` / `Leave` を override |
| 遷移時にパラメータを渡す | `StateNode<T, TArgument>`（`TArgument : StateArgument`）＋ `Request(next, argument)` |
| 実行中でも強制的に遷移したい | `Request(next, force: true)`（実行中の Enter/Leave をキャンセル） |
| 遷移を監視したい | `OnChangeStateStartAsObservable()` / `OnChangeStateFinishAsObservable()`（`ChangeStateInfo.from/to`） |

## 注意点・罠

- `Request` は fire-and-forget（`ChangeState(...).Forget()`）。遷移完了を待ちたい場合は `OnChangeStateFinishAsObservable` を併用する（await できる API は無い）。
- 実行中（`IsExecute`）の `Request` は force なしだと**黙って捨てられる**（ログも出ない）。連打・多重要求に注意。
- 未登録ステートへの遷移は `KeyNotFoundException`。ただし `Forget()` 内で throw されるため呼び出し元では catch できない。
- `StateNode<T, TArgument>.Enter` の呼び出しは **CancellationToken を渡していない**（`node.Enter(argument)` のみ。実装: `StateController.ChangeState`）。引数付きノードの `Enter` は force キャンセルが効かない点に注意。
- 引数付きノードは `Request` の `TArgument` 型と完全一致した `StateNode<T, TArgument>` にのみディスパッチされる（型が合わないと Enter/Leave がスキップされる）。引数なし `Request` は内部で `StateEmptyArgument` を使うため、引数なしノード（`StateNode<T>`）と組み合わせて使う。
- 遷移の再入・キュー機構は無い（Enter 中に次の Request をしたい場合は force 必須）。
- `Current`（現在ステート）は未遷移時は enum の default を返す。

## 関連

- [Scene](Scene.md) — 画面（シーン）遷移はこちらの基盤が担当（本モジュールとは独立）
- [UniTask](UniTask.md) / [R3Extension](R3Extension.md) — Enter/Leave の非同期基盤・通知基盤

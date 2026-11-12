# StateControl

enum で状態を切り替えるステートマシン。
状態ごとの開始・終了処理を非同期で書き、引数を渡した遷移や遷移の通知ができる。

## 基本情報

> **namespace**: `Modules.StateControl`
> **場所**: `Modules/StateControl/`（`StateController.cs` / `StateNode.cs` の2ファイル）
> **依存**: UniTask / R3 / Extensions（`LifetimeDisposable`）

## 概要

enum をキーにした非同期ステートマシン。ステートごとに `Enter` / `Leave` を `UniTask` で実装したノードを登録し、`Request` で遷移する（前ステートの `Leave` 完了 → 次ステートの `Enter` 実行）。遷移引数（`StateArgument`）の受け渡しと、遷移開始/完了の R3 通知を持つ。

主要クラス: `StateController<T>`（本体。sealed・`LifetimeDisposable` 継承。ノード登録・遷移実行・キャンセル管理・遷移通知）/ `StateNode<T>`（引数なしノード。`Enter` / `Leave` を override）/ `StateNode<T, TArgument>`（引数付きノード）/ `StateArgument`（遷移引数の基底）/ `IStateNode<T>`（ノード共通の interface。`State` のみ）。

遷移の実行順: `OnChangeStateStartAsObservable` 発火 → 前ノードの `Leave` → `Current` を次ノードへ更新 → 次ノードの `Enter` → `OnChangeStateFinishAsObservable` 発火。

シンボルゲート無しでコンパイル対象。画面遷移・ゲームフロー等でステートマシンが必要になった場合の選択肢。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| enumベースのステートマシンが欲しい | `StateController<TEnum>` を生成 → `Register(state, node)` → `Request(state)` |
| ステート処理を書く | `StateNode<T>` 継承で `Enter` / `Leave` を override（コンストラクタで自分のステートを `base(state)` に渡す） |
| 遷移時にパラメータを渡す | `StateNode<T, TArgument>`（`TArgument : StateArgument`）＋ `Request(next, argument)`（`Request` 側の `TArgument` には引数なしコンストラクタが必要） |
| 実行中でも強制的に遷移したい | `Request(next, force: true)`（実行中の Enter/Leave をキャンセル） |
| 遷移を監視したい | `OnChangeStateStartAsObservable()` / `OnChangeStateFinishAsObservable()`（`ChangeStateInfo.from/to`） |
| 現在のステート・遷移中かを知りたい | `Current` / `IsExecute` |
| 登録済みのノードを取得したい | `Get(state)`（未登録なら null） |
| 登録を全部消したい | `Clear()`（実行中の遷移をキャンセルし、ノード登録と現在ノードを消す。`IsExecute` も false に戻す） |

## 注意点・罠

- `Request` は fire-and-forget（`ChangeState(...).Forget()`）。遷移完了を待ちたい場合は `OnChangeStateFinishAsObservable` を併用する（await できる API は無い）。
- 実行中（`IsExecute`）の `Request` は force なしだと**黙って捨てられる**（ログも出ない）。連打・多重要求に注意。
- 未登録ステートへの遷移は `KeyNotFoundException`。ただし `Forget()` 内で throw されるため呼び出し元では catch できない。
- **遷移の途中で例外・キャンセルが起きると、その遷移は打ち切られ `IsExecute` は false に戻る**（未登録ステート・`Enter` / `Leave` 内の例外・キャンセル）。打ち切られた時点までの処理は取り消されない（前のステートの `Leave` が済んでいれば、現在ノードは前のステートのまま残る）。キャンセルされた遷移は、`Leave` / `Enter` が完了しても現在ノードの更新と完了通知を行わない。
- `force: true` は実行中の遷移の `CancellationToken` をキャンセルするだけ。ノードがトークンを見ていない場合、前の遷移処理は止まらず新しい遷移と並行して進む。
- `Current` と `ChangeStateInfo` の値はノードの `State`（コンストラクタに渡した値）から取る。`Register(state, node)` の `state` とノードの `State` は一致させる。
- `StateNode<T, TArgument>.Enter` の呼び出しは **CancellationToken を渡していない**（`node.Enter(argument)` のみ。実装: `StateController.ChangeState`）。引数付きノードの `Enter` は force キャンセルが効かない点に注意。
- 引数付きノードは `Request` の `TArgument` 型と完全一致した `StateNode<T, TArgument>` にのみディスパッチされる（型が合わないと Enter/Leave がスキップされる）。引数なし `Request` は内部で `StateEmptyArgument` を使うため、引数なしノード（`StateNode<T>`）と組み合わせて使う。
- 遷移の再入・キュー機構は無い（Enter 中に次の Request をしたい場合は force 必須）。
- `Current`（現在ステート）は未遷移時は enum の default を返す。

## 関連

- [Scene](../Scene/Manual.md) — 画面（シーン）遷移基盤（本モジュールとは独立）
- [UniTaskExtension](../UniTaskExtension/Manual.md) / [R3Extension](../R3Extension/Manual.md) — Enter/Leave の非同期基盤・通知基盤

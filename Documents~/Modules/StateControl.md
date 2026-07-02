# StateControl

> **namespace**: `Modules.StateControl`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/StateControl/`（`StateController.cs` / `StateNode.cs` の2ファイル）
> **Client側使用**: 0ファイル（基盤内使用も0、2026-07時点）
> **依存**: UniTask / R3 / Extensions（`LifetimeDisposable`）

## 概要

enum をキーにした非同期ステートマシン。ステートごとに `Enter` / `Leave` を `UniTask` で実装したノードを登録し、`Request` で遷移する（前ステートの `Leave` 完了 → 次ステートの `Enter` 実行）。遷移引数（`StateArgument`）の受け渡しと、遷移開始/完了の R3 通知を持つ。

**本プロジェクトでは未使用**（ガード無しでコンパイルはされている）。画面遷移・ゲームフロー等でステートマシンが必要になった場合は、自作する前に本クラスの採用を検討すること。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| enumベースのステートマシンが欲しい | `StateController<TEnum>` を生成 → `Register(state, node)` → `Request(state)` |
| ステート処理を書く | `StateNode<T>` 継承で `Enter` / `Leave` を override |
| 遷移時にパラメータを渡す | `StateNode<T, TArgument>`（`TArgument : StateArgument`）＋ `Request(next, argument)` |
| 実行中でも強制的に遷移したい | `Request(next, force: true)`（実行中の Enter/Leave をキャンセル） |
| 遷移を監視したい | `OnChangeStateStartAsObservable()` / `OnChangeStateFinishAsObservable()`（`ChangeStateInfo.from/to`） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `StateController<T>` | sealed class（`LifetimeDisposable`、`T : Enum`） | 本体。ノード登録テーブル・遷移実行（`ChangeState`）・キャンセル管理・遷移通知 |
| `IStateNode<T>` | interface | `State` プロパティのみ。登録テーブルの共通型 |
| `StateNode<T>` | abstract class | 引数なしノード。`Enter(CancellationToken)` / `Leave(CancellationToken)` を virtual で提供（既定は即完了） |
| `StateNode<T, TArgument>` | abstract class | 引数付きノード。`Enter(TArgument, CancellationToken)` / `Leave(CancellationToken)` |
| `StateArgument` | abstract class | 遷移引数の基底（空クラス。派生で自由にフィールド定義） |

## 使い方(実例)

Client側・基盤内とも使用例なし。実コードのシグネチャに基づく最小の想定例。

```csharp
// 想定例（実在コードではない）. シグネチャは
// Client/Assets/UniModules/Scripts/Modules/StateControl/StateController.cs 参照.
public enum GameState { Title, Home, Battle }

public sealed class BattleStateNode : StateNode<GameState>
{
    public BattleStateNode() : base(GameState.Battle) { }

    public override async UniTask Enter(CancellationToken cancelToken = default)
    {
        // ステート開始処理.
    }

    public override async UniTask Leave(CancellationToken cancelToken = default)
    {
        // ステート終了処理.
    }
}

// 構築と遷移.
var stateController = new StateController<GameState>();

stateController.Register(GameState.Battle, new BattleStateNode());

stateController.OnChangeStateFinishAsObservable()
    .Subscribe(x => Debug.Log($"{x.from} -> {x.to}"))
    .AddTo(Disposable);

stateController.Request(GameState.Battle);
```

## API(主要公開メンバー)

### StateController&lt;T&gt;

| メンバー | 説明 |
|---|---|
| `Register(T state, IStateNode<T> node)` | ノード登録（同一ステートは上書き） |
| `Get(T state) : IStateNode<T>` | 登録ノード取得（未登録は null） |
| `Request(T next, bool force = false)` | 遷移要求（引数なし）。実行中は force=false だと**無視される** |
| `Request<TArgument>(T next, TArgument argument, bool force = false)` | 引数付き遷移要求（`TArgument : StateArgument, new()`） |
| `Clear()` | 実行中遷移をキャンセルし全ノード破棄 |
| `Current : T` | 現在ステート（未遷移時は enum の default） |
| `IsExecute : bool` | 遷移処理（Leave/Enter）実行中か |
| `OnChangeStateStartAsObservable()` / `OnChangeStateFinishAsObservable()` : `Observable<ChangeStateInfo>` | 遷移開始/完了通知（`from` / `to`） |

## 注意点・罠

- `Request` は fire-and-forget（`ChangeState(...).Forget()`）。遷移完了を待ちたい場合は `OnChangeStateFinishAsObservable` を併用する（await できる API は無い）。
- 実行中（`IsExecute`）の `Request` は force なしだと**黙って捨てられる**（ログも出ない）。連打・多重要求に注意。
- 未登録ステートへの遷移は `KeyNotFoundException`。ただし `Forget()` 内で throw されるため呼び出し元では catch できない。
- `StateNode<T, TArgument>.Enter` の呼び出しは **CancellationToken を渡していない**（`node.Enter(argument)` のみ。実装: `StateController.ChangeState`）。引数付きノードの `Enter` は force キャンセルが効かない点に注意。
- 引数付きノードは `Request` の `TArgument` 型と完全一致した `StateNode<T, TArgument>` にのみディスパッチされる（型が合わないと Enter/Leave がスキップされる）。引数なし `Request` は内部で `StateEmptyArgument` を使うため、引数なしノード（`StateNode<T>`）と組み合わせて使う。
- 遷移の再入・キュー機構は無い（Enter 中に次の Request をしたい場合は force 必須）。

## 関連

- [Scene](Scene.md) — 画面（シーン）遷移はこちらの基盤が担当（本モジュールとは独立）
- [UniTask](UniTask.md) / [R3Extension](R3Extension.md) — Enter/Leave の非同期基盤・通知基盤

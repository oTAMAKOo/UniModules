# PathFinding

> **namespace**: `Modules.PathFinding`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PathFinding/`（`Astar.cs` / `Node.cs` の2ファイル）
> **Client側使用**: 0ファイル（基盤内使用も0、2026-07時点）
> **依存**: UniTask / R3 / UnityEngine（`Vector2Int`）

## 概要

2Dグリッド用の A*（A-star）経路探索。`Vector2Int` 座標のグリッドを構築し、障害物（Lock）を設定した上で開始→ゴールの経路を非同期（フレーム分割）で探索する。4方向/8方向（斜め）移動に対応。

**本プロジェクトでは未使用**（ガード無しでコンパイルはされている）。グリッドマップの経路探索が必要になった場合、自作前に本クラスを検討する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| グリッド上の最短経路を求めたい | `AStar` を生成 → `Initialize(sizeX, sizeY, allowDiagonal)` → `SearchRoute(start, goal)` を購読 |
| 障害物（通行不可マス）を設定したい | `AStar.SetLock(nodeId, true)` |
| 斜め移動を許可したい | `Initialize(..., allowDiagonal: true, diagonalMoveCost)`（コスト省略時は √2） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `AStar` | sealed class | 本体。グリッド初期化・Lock設定・経路探索（open/closedリスト方式、1000ステップごとに `UniTask.Yield` でフレーム分割） |
| `Node` | struct（メンバーは internal） | 1マスの探索状態（座標・遷移元・移動コスト・ヒューリスティックコスト・Lock/Active）。モジュール外から直接は操作しない |

## 使い方(実例)

Client側・基盤内とも使用例なし。実コードのシグネチャに基づく最小の想定例。

```csharp
// 想定例（実在コードではない）. シグネチャは
// Client/Assets/UniModules/Scripts/Modules/PathFinding/Astar.cs 参照.
var astar = new AStar();

astar.Initialize(sizeX: 20, sizeY: 20, allowDiagonal: false);

// 障害物設定.
astar.SetLock(new Vector2Int(5, 3), true);

// 探索（R3のObservable。結果は開始マスを含まずゴールマスを含む経路）.
astar.SearchRoute(new Vector2Int(0, 0), new Vector2Int(10, 8))
    .Subscribe(
        route => OnRouteFound(route.ToArray()),     // 参照が使い回されるため必ずコピーする.
        result => Debug.LogError($"PathFinding failed. {result}"))
    .AddTo(Disposable);
```

## API(主要公開メンバー)

### AStar

| メンバー | 説明 |
|---|---|
| `Initialize(int sizeX, int sizeY, bool allowDiagonal, float? diagonalMoveCost = null)` | グリッド構築（全ノード生成）。斜めコスト既定は `Mathf.Sqrt(2f)` |
| `SearchRoute(Vector2Int startNodeId, Vector2Int goalNodeId) : Observable<IEnumerable<Vector2Int>>` | 経路探索。購読で開始（コールド）。成功時 `OnNext(経路)` → `OnCompleted`、失敗時は `Result.Failure(Exception)` 付き `OnCompleted` |
| `SetLock(Vector2Int lockNodeId, bool isLock)` | 通行可否の設定（探索前に設定しておく） |

### Node（internal操作のみ・参考）

| メンバー | 説明 |
|---|---|
| `NodeId` / `FromNodeId` / `MoveCost` / `IsLock` / `IsActive` | 探索状態。`GetScore()` = 実コスト + ヒューリスティック |
| ヒューリスティック | 4方向: マンハッタン距離 / 8方向: チェビシェフ距離（`UpdateGoalNodeId`） |

## 注意点・罠

- **2026-07 に2件の不具合を修正済み**: ①到達不能ゴール（Lockで囲まれている等）で探索ループが永遠に完了しない問題 → open ノード枯渇時に `"PathFinding unreachable"` の `Result.Failure` で完了するよう修正。②探索ループのカウンタが経路復元ループの上限（固定1000）に持ち越され、大きなグリッドで正当な経路でも失敗する問題 → 復元カウンタを分離し上限を `sizeX * sizeY` に適正化。
- 結果の `IEnumerable<Vector2Int>` は内部フィールド `routeList` の**参照そのもの**。次回 `SearchRoute` で Clear されるため、受け取ったら即 `ToArray()` 等でコピーする。
- 経路は「開始マスを含まない・ゴールマスを含む」順列。`start == goal` の場合は空の経路で成功通知。
- 失敗は例外ではなく `Result.Failure` 付き完了（R3）。`Subscribe(onNext, onCompleted)` で `Result.IsFailure` を確認する。
- `Initialize` はグリッド全マスの `Node` を3面（nodes/open/closed）確保する。サイズ変更のたびに呼び直す設計（部分更新APIは無い）。
- スコア同点時は実コスト（`MoveCost`）が小さい方を優先する実装。

## 関連

- [UniTask](UniTask.md) / [R3Extension](R3Extension.md) — 非同期実行・通知の基盤

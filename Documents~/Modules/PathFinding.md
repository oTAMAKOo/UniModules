# PathFinding

> **namespace**: `Modules.PathFinding`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PathFinding/`（`Astar.cs` / `Node.cs` の2ファイル）
> **依存**: UniTask / R3 / UnityEngine（`Vector2Int`）

## 概要

2Dグリッド用の A*（A-star）経路探索。`Vector2Int` 座標のグリッドを構築し、障害物（Lock）を設定した上で開始→ゴールの経路を非同期（フレーム分割）で探索する。4方向/8方向（斜め）移動に対応。

主要クラス: `AStar`（本体。グリッド初期化・Lock設定・経路探索）/ `Node`（1マスの探索状態。メンバーは internal でモジュール外から直接は操作しない）。

シンボルゲート無しでコンパイル対象。グリッドマップの経路探索が必要になった場合の選択肢。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| グリッド上の最短経路を求めたい | `AStar` を生成 → `Initialize(sizeX, sizeY, allowDiagonal)` → `SearchRoute(start, goal)` を購読 |
| 障害物（通行不可マス）を設定したい | `AStar.SetLock(nodeId, true)` |
| 斜め移動を許可したい | `Initialize(..., allowDiagonal: true, diagonalMoveCost)`（コスト省略時は √2） |

## 注意点・罠

- `SearchRoute` は `Observable<IEnumerable<Vector2Int>>` を返し、購読で開始（コールド）。
- ゴールへ到達不能（Lock で囲まれている等）の場合は `"PathFinding unreachable"` の `Result.Failure` で完了する。呼び出し側は失敗ケースを必ずハンドリングすること。
- 結果の `IEnumerable<Vector2Int>` は内部フィールド `routeList` の**参照そのもの**。次回 `SearchRoute` で Clear されるため、受け取ったら即 `ToArray()` 等でコピーする。
- 経路は「開始マスを含まない・ゴールマスを含む」順列。`start == goal` の場合は空の経路で成功通知。
- 失敗は例外ではなく `Result.Failure` 付き完了（R3）。`Subscribe(onNext, onCompleted)` で `Result.IsFailure` を確認する。
- `Initialize` はグリッド全マスの `Node` を3面（nodes/open/closed）確保する。サイズ変更のたびに呼び直す設計（部分更新APIは無い）。
- スコア同点時は実コスト（`MoveCost`）が小さい方を優先する実装。

## 関連

- [UniTask](UniTask.md) / [R3Extension](R3Extension.md) — 非同期実行・通知の基盤

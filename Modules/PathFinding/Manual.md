# PathFinding

マス目の上で経路を探す機能。
通れないマスを設定して、開始マスからゴールまでの最短の道順を求められる。

## 基本情報

> **namespace**: `Modules.PathFinding`
> **場所**: `Modules/PathFinding/`（`Astar.cs` / `Node.cs` の2ファイル）
> **依存**: UniTask / R3 / UnityEngine（`Vector2Int`）

## 概要

2Dグリッド用の A*（A-star）経路探索。`Vector2Int` 座標のグリッドを構築し、障害物（Lock）を設定した上で開始→ゴールの経路を非同期（フレーム分割）で探索する。4方向/8方向（斜め）移動に対応。

主要クラス: `AStar`（sealed class。本体。グリッド初期化・Lock設定・経路探索）/ `Node`（struct。1マスの探索状態。メンバーは internal でモジュール外から直接は操作しない）。

シンボルゲート無しでコンパイル対象。グリッドマップの経路探索が必要になった場合の選択肢。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| グリッド上の最短経路を求めたい | `AStar` を生成 → `Initialize(sizeX, sizeY, allowDiagonal)` → `SearchRoute(start, goal)` を購読 |
| 障害物（通行不可マス）を設定したい | `AStar.SetLock(nodeId, true)` |
| 斜め移動を許可したい | `Initialize(..., allowDiagonal: true, diagonalMoveCost)`（コスト省略時は √2） |

## 注意点・罠

- `SearchRoute` は `Observable<IEnumerable<Vector2Int>>` を返し、購読で開始（コールド）。
- `SearchRoute` / `SetLock` の前に `Initialize` が必要（未初期化だと内部の配列が null）。座標の範囲チェックは無く、グリッド外の座標を渡すと配列の範囲外アクセスになる。
- `SetLock` の状態は探索をまたいで残る。`Initialize` を呼び直すと全マスが作り直され、Lock も消える。
- 探索は1000回の展開ごと（経路の逆引きは100回ごと）に `UniTask.Yield` で次のフレームへ処理を分ける。購読を破棄するとキャンセルされ、何も通知せずに終わる。
- ゴールへ到達不能（Lock で囲まれている等）の場合は `"PathFinding unreachable"` の `Result.Failure` で完了する。呼び出し側は失敗ケースを必ずハンドリングすること。
- 結果の `IEnumerable<Vector2Int>` は内部フィールド `routeList` の**参照そのもの**。次回 `SearchRoute` で Clear されるため、受け取ったら即 `ToArray()` 等でコピーする。
- 経路は「開始マスを含まない・ゴールマスを含む」順列。`start == goal` の場合は空の経路で成功通知。
- 失敗は例外ではなく `Result.Failure` 付き完了（R3）。`Subscribe(onNext, onCompleted)` で `Result.IsFailure` を確認する。
- `Initialize` はグリッド全マスの `Node` を3面（nodes/open/closed）確保する。サイズ変更のたびに呼び直す設計（部分更新APIは無い）。
- スコア同点時は実コスト（`MoveCost`）が小さい方を優先する実装。

## 関連

- [UniTaskExtension](../UniTaskExtension/Manual.md) / [R3Extension](../R3Extension/Manual.md) — 非同期実行・通知の基盤

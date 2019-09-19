
using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UniRx;

namespace Modules.PathFinding
{
    public sealed class AStar
    {
        //----- params -----

        //----- field -----

        private int sizeX = 0;
        private int sizeY = 0;

        private Node[,] nodes = null;
        private Node[,] openNodes = null;
        private Node[,] closedNodes = null;

        // 斜め移動の場合のコスト
        private float diagonalMoveCost = 0;

        // 斜め移動可能か.
        private bool allowDiagonal = false;

        private List<Vector2Int> routeList = null;

        //----- property -----

        //----- method -----

        /// <summary>
        /// 使用する前に実行して初期化してください
        /// </summary>
        public void Initialize(int sizeX, int sizeY, bool allowDiagonal, float? diagonalMoveCost = null)
        {
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.allowDiagonal = allowDiagonal;
            this.diagonalMoveCost = diagonalMoveCost.HasValue ? diagonalMoveCost.Value : Mathf.Sqrt(2f);

            routeList = new List<Vector2Int>();

            nodes = new Node[sizeY, sizeX];
            openNodes = new Node[sizeY, sizeX];
            closedNodes = new Node[sizeY, sizeX];

            for (var y = 0; y < sizeY; y++)
            {
                for (var x = 0; x < sizeX; x++)
                {
                    nodes[y, x] = Node.CreateBlankNode(allowDiagonal, new Vector2Int(x, y));
                    openNodes[y, x] = Node.CreateBlankNode(allowDiagonal, new Vector2Int(x, y));
                    closedNodes[y, x] = Node.CreateBlankNode(allowDiagonal, new Vector2Int(x, y));
                }
            }
        }
        
        /// <summary> ルート検索開始 </summary>
        public IObservable<IEnumerable<Vector2Int>> SearchRoute(Vector2Int startNodeId, Vector2Int goalNodeId)
        {
            return Observable.FromMicroCoroutine<IEnumerable<Vector2Int>>(observer => SearchRouteInternal(observer, startNodeId, goalNodeId));
        }

        private IEnumerator SearchRouteInternal(IObserver<IEnumerable<Vector2Int>> observer, Vector2Int startNodeId, Vector2Int goalNodeId)
        {
            routeList.Clear();

            ResetNode();

            if (startNodeId == goalNodeId)
            {
                observer.OnError(new Exception($"{startNodeId}/{goalNodeId}/同じ場所なので終了"));
                yield break;
            }

            // 全ノード更新
            for (var y = 0; y < sizeY; y++)
            {
                for (var x = 0; x < sizeX; x++)
                {
                    nodes[y, x].UpdateGoalNodeId(goalNodeId);
                    openNodes[y, x].UpdateGoalNodeId(goalNodeId);
                    closedNodes[y, x].UpdateGoalNodeId(goalNodeId);
                }
            }

            // スタート地点の初期化
            openNodes[startNodeId.y, startNodeId.x] = Node.CreateNode(allowDiagonal, startNodeId, goalNodeId);
            openNodes[startNodeId.y, startNodeId.x].SetFromNodeId(startNodeId);
            openNodes[startNodeId.y, startNodeId.x].Add();

            var cnt = 0;

            while (true)
            {
                var bestScoreNodeId = GetBestScoreNodeId();

                OpenAround(bestScoreNodeId, goalNodeId);

                // ゴールに辿り着いたら終了
                if (bestScoreNodeId == goalNodeId){ break; }

                if (cnt % 100 == 0)
                {
                    yield return null;
                }

                cnt++;
            }

            var node = closedNodes[goalNodeId.y, goalNodeId.x];

            routeList.Add(goalNodeId);

            // 捜査トライ回数を1000と決め打ち(無限ループ対応).
            var tryCount = 1000;
            var isSuccess = false;

            while (cnt++ < tryCount)
            {
                var beforeNode = routeList[0];

                if (beforeNode == node.FromNodeId)
                {
                    // 同じポジションなので終了.
                    observer.OnError(new Exception("同じポジションなので終了失敗" + beforeNode + " / " + node.FromNodeId + " / " + goalNodeId));
                    yield break;
                }

                if (node.FromNodeId == startNodeId)
                {
                    isSuccess = true;
                    break;
                }

                // 開始座標は結果リストには追加しない.
                routeList.Insert(0, node.FromNodeId);

                node = closedNodes[node.FromNodeId.y, node.FromNodeId.x];

                if (cnt % 100 == 0)
                {
                    yield return null;
                }
            }

            if (!isSuccess)
            {
                observer.OnError(new Exception("失敗" + startNodeId + " / " + node.FromNodeId));
                yield break;
            }

            observer.OnNext(routeList);
            observer.OnCompleted();
        }

        private void ResetNode()
        {
            for (var y = 0; y < sizeY; y++)
            {
                for (var x = 0; x < sizeX; x++)
                {
                    nodes[y, x].Clear();
                    openNodes[y, x].Clear();
                    closedNodes[y, x].Clear();
                }
            }
        }

        /// <summary> 最良のノードIDを返却 </summary>
        private Vector2Int GetBestScoreNodeId()
        {
            var result = new Vector2Int(0, 0);
            
            // 最小スコア.
            var min = double.MaxValue;
            // 最小実コスト
            var minCost = double.MaxValue;

            for (var y = 0; y < sizeY; y++)
            {
                for (var x = 0; x < sizeX; x++)
                {
                    var node = openNodes[y, x];

                    if (!node.IsActive) { continue; }

                    var score = node.GetScore();

                    if (min < score) { continue; }

                    // スコアが同じときは実コストも比較する.
                    if (score == min && node.MoveCost >= minCost)
                    {
                        continue;
                    }

                    // 優秀なコストの更新(値が低いほど優秀)
                    min = score;
                    minCost = node.MoveCost;
                    result = node.NodeId;
                }
            }

            return result;
        }

        // ノードを展開する
        private void OpenAround(Vector2Int bestNodeId, Vector2Int goalNodeId)
        {
            // 8方向走査.
            if (allowDiagonal)
            {
                for (var dy = -1; dy < 2; dy++)
                {
                    for (var dx = -1; dx < 2; dx++)
                    {
                        // 縦横で動く場合はコスト : 1
                        // 斜めに動く場合はコスト : _diagonalMoveCost
                        var addCost = dx * dy == 0 ? 1 : diagonalMoveCost;

                        OpenNode(bestNodeId, goalNodeId, dx, dy, addCost);
                    }
                }
            }
            // 4方向走査.
            else
            {
                OpenNode(bestNodeId, goalNodeId, -1,  0, 1); // 右.
                OpenNode(bestNodeId, goalNodeId,  0, -1, 1); // 上.
                OpenNode(bestNodeId, goalNodeId,  1,  0, 1); // 左.
                OpenNode(bestNodeId, goalNodeId,  0,  1, 1); // 下.
            }

            // 展開が終わったノードは closed に追加する
            closedNodes[bestNodeId.y, bestNodeId.x] = openNodes[bestNodeId.y, bestNodeId.x];
            // closedNodesに追加
            closedNodes[bestNodeId.y, bestNodeId.x].Add();
            // openNodesから削除
            openNodes[bestNodeId.y, bestNodeId.x].Remove();
        }

        private void OpenNode(Vector2Int bestNodeId, Vector2Int goalNodeId, int x, int y, float cost)
        {
            if (!CheckOutOfRange(x, y, bestNodeId.x, bestNodeId.y)) { return; }

            var cx = bestNodeId.x + x;
            var cy = bestNodeId.y + y;

            if (nodes[cy, cx].IsLock) { return; }

            nodes[cy, cx].SetMoveCost(openNodes[bestNodeId.y, bestNodeId.x].MoveCost + cost);
            nodes[cy, cx].SetFromNodeId(bestNodeId);

            // ノードのチェック
            UpdateNodeList(cx, cy, goalNodeId);
        }

        /// <summary> 走査範囲内チェック </summary>
        private bool CheckOutOfRange(int dx, int dy, int x, int y)
        {
            if (dx == 0 && dy == 0)
            {
                return false;
            }

            var cx = x + dx;
            var cy = y + dy;

            if (cx < 0 || cx == sizeX || cy < 0 || cy == sizeY)
            {
                return false;
            }

            return true;
        }

        /// <summary> ノードリストの更新 </summary>
        private void UpdateNodeList(int x, int y, Vector2Int goalNodeId)
        {
            if (openNodes[y, x].IsActive)
            {
                // より優秀なスコアであるならMoveCostとfromを更新する
                if (openNodes[y, x].GetScore() > nodes[y, x].GetScore())
                {
                    // Node情報の更新
                    openNodes[y, x].SetMoveCost(nodes[y, x].MoveCost);
                    openNodes[y, x].SetFromNodeId(nodes[y, x].FromNodeId);
                }
            }
            else if (closedNodes[y, x].IsActive)
            {
                // より優秀なスコアであるなら closedNodesから除外しopenNodesに追加する
                if (closedNodes[y, x].GetScore() > nodes[y, x].GetScore())
                {
                    closedNodes[y, x].Remove();
                    openNodes[y, x].Add();
                    openNodes[y, x].SetMoveCost(nodes[y, x].MoveCost);
                    openNodes[y, x].SetFromNodeId(nodes[y, x].FromNodeId);
                }
            }
            else
            {
                openNodes[y, x] = new Node(allowDiagonal, new Vector2Int(x, y), goalNodeId);
                openNodes[y, x].SetFromNodeId(nodes[y, x].FromNodeId);
                openNodes[y, x].SetMoveCost(nodes[y, x].MoveCost);
                openNodes[y, x].Add();
            }
        }

        /// <summary>
        /// ノードのロックフラグを変更
        /// </summary>
        public void SetLock(Vector2Int lockNodeId, bool isLock)
        {
            nodes[lockNodeId.y, lockNodeId.x].SetIsLock(isLock);
        }
    }
}
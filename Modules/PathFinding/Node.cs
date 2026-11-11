
using UnityEngine;

namespace Modules.PathFinding
{
    public struct Node
    {
        //----- params -----

        //----- field -----

        private bool allowDiagonal;

        /// <summary>
        /// ヒューリスティックなコスト
        /// </summary>
        private double heuristicCost;

        //----- property -----

        /// <summary>
        /// ノードのポジション
        /// </summary>
        internal Vector2Int NodeId { get; }

        /// <summary>
        /// このノードにたどり着く前のノードポジション
        /// </summary>
        internal Vector2Int FromNodeId { get; private set; }

        /// <summary>
        /// 経路として使用できないフラグ
        /// </summary>
        internal bool IsLock { get; private set; }

        /// <summary>
        /// ノードの有無
        /// </summary>
        internal bool IsActive { get; private set; }

        /// <summary>
        /// 必要コスト
        /// </summary>
        internal double MoveCost { get; private set; }

        //----- method -----

        /// <summary>
        /// 空のノードの生成
        /// </summary>
        internal static Node CreateBlankNode(bool allowDiagonal, Vector2Int position)
        {
            return new Node(allowDiagonal, position, new Vector2Int(-1, -1));
        }

        /// <summary>
        /// ノード生成
        /// </summary>
        internal static Node CreateNode(bool allowDiagonal, Vector2Int position, Vector2Int goalPosition)
        {
            return new Node(allowDiagonal, position, goalPosition);
        }

        /// <summary>
        /// CreateBlankNode,CreateNodeを使用してください
        /// </summary>
        internal Node(bool allowDiagonal, Vector2Int nodeId, Vector2Int goalNodeId) : this()
        {
            this.allowDiagonal = allowDiagonal;

            NodeId = nodeId;
            IsLock = false;
            Remove();
            MoveCost = 0;
            UpdateGoalNodeId(goalNodeId);
        }

        /// <summary>
        /// ゴール更新 ヒューリスティックコストの更新
        /// </summary>
        internal void UpdateGoalNodeId(Vector2Int goal)
        {
            // 斜め移動あり.
            if (allowDiagonal)
            {
                var dx = (int)Mathf.Abs(goal.x - NodeId.x);
                var dy = (int)Mathf.Abs(goal.y - NodeId.y);

                // 大きい方をコストにする
                heuristicCost = dx > dy ? dx : dy;
            }
            // 縦横移動のみ.
            else
            {
                var dx = Mathf.Abs(goal.x - NodeId.x);
                var dy = Mathf.Abs(goal.y - NodeId.y);

                heuristicCost = (int)(dx + dy);
            }
        }

        internal double GetScore()
        {
            return MoveCost + heuristicCost;
        }

        internal void SetFromNodeId(Vector2Int value)
        {
            FromNodeId = value;
        }

        internal void Remove()
        {
            IsActive = false;
        }

        internal void Add()
        {
            IsActive = true;
        }

        internal void SetMoveCost(double cost)
        {
            MoveCost = cost;
        }

        internal void SetIsLock(bool isLock)
        {
            IsLock = isLock;
        }

        internal void Clear()
        {
            Remove();
            MoveCost = 0;
            UpdateGoalNodeId(new Vector2Int(-1, -1));
        }
    }
}
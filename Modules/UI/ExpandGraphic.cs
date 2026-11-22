
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI
{
    /// <summary> Graphic の描画範囲を外側へ広げるコンポーネント </summary>
    /// <remarks>
    /// 1枚の矩形(4頂点)のメッシュだけを対象にし、それ以外のメッシュは変更しない.
    /// 広げた頂点の UV は元の位置と UV の対応を延長して求める. 広げた範囲はテクスチャの元の範囲の外を指す.
    /// 元の矩形の中の位置(0〜1. 広げた範囲は 0 未満・1 超)に PositionOffset を足した値を uv0.zw に入れる.
    /// 元の範囲の外を描かないようにするのはシェーダーの役割(uv0.zw が 0〜1 の外のときにテクスチャを読まない).
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class ExpandGraphic : BaseMeshEffect
    {
        //----- params -----

        /// <summary> 対象にする頂点数(1枚の矩形) </summary>
        private const int VertexCount = 4;

        /// <summary> 元の矩形の中の位置に足す値. uv0.zw が 0 の(このコンポーネントが無い)メッシュと見分けるために使う </summary>
        public const float PositionOffset = 4f;

        //----- field -----

        [SerializeField]
        private float padding = 4f;

        private UIVertex[] vertices = null;

        //----- property -----

        /// <summary> 外側へ広げる長さ(RectTransform と同じ単位) </summary>
        public float Padding
        {
            get { return padding; }

            set
            {
                var amount = Mathf.Max(0f, value);

                if (padding == amount){ return; }

                padding = amount;

                Refresh();
            }
        }

        //----- method -----

        public override void ModifyMesh(VertexHelper vh)
        {
            if (IsActive() == false){ return; }

            if (vh.currentVertCount != VertexCount){ return; }

            if (vertices == null)
            {
                vertices = new UIVertex[VertexCount];
            }

            for (var i = 0; i < VertexCount; i++)
            {
                vh.PopulateUIVertex(ref vertices[i], i);
            }

            if (ModifyVertices(vertices) == false){ return; }

            for (var i = 0; i < VertexCount; i++)
            {
                vh.SetUIVertex(vertices[i], i);
            }
        }

        /// <summary> 矩形の頂点を外へ広げる. 変更した場合 true </summary>
        private bool ModifyVertices(UIVertex[] verts)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (var i = 0; i < verts.Length; i++)
            {
                var position = verts[i].position;

                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var size = max - min;

            if (size.x <= 0f || size.y <= 0f){ return false; }

            // 位置から UV への対応(アフィン変換)を、3つの頂点から求める.
            Vector2 uvByX;
            Vector2 uvByY;
            Vector2 uvOrigin;

            if (TryGetUVMapping(verts, out uvByX, out uvByY, out uvOrigin) == false){ return false; }

            var center = (min + max) * 0.5f;

            for (var i = 0; i < verts.Length; i++)
            {
                var vertex = verts[i];

                var position = (Vector2)vertex.position;

                // 中心から見た向きへ広げる.
                var expanded = new Vector2(
                    position.x + (position.x < center.x ? -padding : padding),
                    position.y + (position.y < center.y ? -padding : padding));

                var uv = uvOrigin + uvByX * expanded.x + uvByY * expanded.y;

                var normalized = new Vector2((expanded.x - min.x) / size.x, (expanded.y - min.y) / size.y);

                vertex.position = new Vector3(expanded.x, expanded.y, vertex.position.z);
                vertex.uv0 = new Vector4(uv.x, uv.y, normalized.x + PositionOffset, normalized.y + PositionOffset);

                verts[i] = vertex;
            }

            return true;
        }

        /// <summary> 位置から UV への対応を求める(uv = uvOrigin + uvByX * x + uvByY * y) </summary>
        private static bool TryGetUVMapping(UIVertex[] verts, out Vector2 uvByX, out Vector2 uvByY, out Vector2 uvOrigin)
        {
            uvByX = Vector2.zero;
            uvByY = Vector2.zero;
            uvOrigin = Vector2.zero;

            var p0 = (Vector2)verts[0].position;
            var p1 = (Vector2)verts[1].position;
            var p2 = (Vector2)verts[2].position;

            var e1 = p1 - p0;
            var e2 = p2 - p0;

            var det = e1.x * e2.y - e2.x * e1.y;

            if (Mathf.Abs(det) < 1e-6f){ return false; }

            var uv0 = (Vector2)verts[0].uv0;
            var d1 = (Vector2)verts[1].uv0 - uv0;
            var d2 = (Vector2)verts[2].uv0 - uv0;

            // [uvByX uvByY] = [d1 d2] * inverse([e1 e2]).
            uvByX = (d1 * e2.y - d2 * e1.y) / det;
            uvByY = (d2 * e1.x - d1 * e2.x) / det;

            uvOrigin = uv0 - uvByX * p0.x - uvByY * p0.y;

            return true;
        }

        public void Refresh()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        #if UNITY_EDITOR

        protected override void OnValidate()
        {
            padding = Mathf.Max(0f, padding);

            base.OnValidate();
        }

        #endif
    }
}

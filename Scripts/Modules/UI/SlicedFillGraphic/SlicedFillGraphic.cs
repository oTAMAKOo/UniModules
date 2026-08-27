using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Modules.UI
{
    /// <summary> Graphicのメッシュを塗り量でカットするコンポーネント </summary>
    /// <remarks>
    /// Image.fillAmount は Image.Type.Filled でしか機能しないため、9スライス(Sliced)のままゲージ表現をしたい場合に使用する.
    /// 生成済みのメッシュ(軸平行なquadの集合)をカットするため、9スライスの端(border)やテクスチャが引き伸ばされない.
    /// 他の IMeshModifier(ColorGradation / FlipGraphic 等)と併用する場合、本コンポーネントを最後(コンポーネント順で下)に配置する.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class SlicedFillGraphic : BaseMeshEffect
    {
        //----- params -----

        public enum FillOrigin
        {
            Left,
            Right,
            Bottom,
            Top,
        }

        /// <summary> 三角形ストリーム上の1quadあたりの頂点数 </summary>
        private const int VertexCountPerQuad = 6;

        /// <summary> 同一辺上の頂点と判定する許容誤差 </summary>
        private const float PositionTolerance = 0.01f;

        //----- field -----

        [SerializeField]
        private FillOrigin fillOrigin = FillOrigin.Left;
        [SerializeField]
        [Range(0f, 1f)]
        private float fillAmount = 1f;

        private List<UIVertex> vertices = null;

        //----- property -----

        /// <summary> 塗りの起点 </summary>
        public FillOrigin Origin
        {
            get { return fillOrigin; }

            set
            {
                if (fillOrigin == value){ return; }

                fillOrigin = value;

                Refresh();
            }
        }

        /// <summary> 塗り量(0f〜1f) </summary>
        public float FillAmount
        {
            get { return fillAmount; }

            set
            {
                var amount = Mathf.Clamp01(value);

                if (fillAmount == amount){ return; }

                fillAmount = amount;

                Refresh();
            }
        }

        /// <summary> カット軸が水平方向か </summary>
        public bool IsHorizontal
        {
            get { return fillOrigin == FillOrigin.Left || fillOrigin == FillOrigin.Right; }
        }

        //----- method -----

        public override void ModifyMesh(VertexHelper vh)
        {
            if (IsActive() == false){ return; }

            if (1f <= fillAmount){ return; }

            if (fillAmount <= 0f)
            {
                vh.Clear();

                return;
            }

            if (vertices == null)
            {
                vertices = new List<UIVertex>();
            }

            vertices.Clear();

            vh.GetUIVertexStream(vertices);

            if (ModifyVertices(vertices) == false){ return; }

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
        }

        /// <summary> 塗り量でメッシュをカットする. カットした場合 true </summary>
        private bool ModifyVertices(List<UIVertex> verts)
        {
            if (verts == null){ return false; }

            if (verts.Count == 0){ return false; }

            // uGUIが生成するquadの三角形ストリーム(6頂点 = 1quad)を前提とする. 一致しないメッシュは変形しない.
            if (verts.Count % VertexCountPerQuad != 0){ return false; }

            var horizontal = IsHorizontal;
            var invert = fillOrigin == FillOrigin.Right || fillOrigin == FillOrigin.Top;

            // 描画されるメッシュ全体の範囲からカット位置を求める.
            var meshMin = float.MaxValue;
            var meshMax = float.MinValue;

            for (var i = 0; i < verts.Count; i++)
            {
                var value = GetAxisValue(verts[i].position, horizontal);

                meshMin = Mathf.Min(meshMin, value);
                meshMax = Mathf.Max(meshMax, value);
            }

            if (meshMax - meshMin <= 0f){ return false; }

            var border = invert
                ? Mathf.Lerp(meshMax, meshMin, fillAmount)
                : Mathf.Lerp(meshMin, meshMax, fillAmount);

            var writeCount = 0;

            for (var i = 0; i < verts.Count; i += VertexCountPerQuad)
            {
                var quadMin = float.MaxValue;
                var quadMax = float.MinValue;

                for (var j = 0; j < VertexCountPerQuad; j++)
                {
                    var value = GetAxisValue(verts[i + j].position, horizontal);

                    quadMin = Mathf.Min(quadMin, value);
                    quadMax = Mathf.Max(quadMax, value);
                }

                // カット位置の外側に収まっているquadは破棄する.
                if (invert ? quadMax <= border : border <= quadMin){ continue; }

                // カット位置を跨いでいるquadは端の頂点を寄せる.
                var crossed = invert ? quadMin < border : border < quadMax;

                for (var j = 0; j < VertexCountPerQuad; j++)
                {
                    var vertex = verts[i + j];

                    if (crossed)
                    {
                        vertex = CutVertex(verts, i, i + j, horizontal, invert, border);
                    }

                    verts[writeCount] = vertex;

                    writeCount++;
                }
            }

            if (writeCount < verts.Count)
            {
                verts.RemoveRange(writeCount, verts.Count - writeCount);
            }

            return true;
        }

        /// <summary> カット位置の外側にある頂点をカット位置まで寄せる </summary>
        private static UIVertex CutVertex(List<UIVertex> verts, int quadStart, int index, bool horizontal, bool invert, float border)
        {
            var vertex = verts[index];

            var value = GetAxisValue(vertex.position, horizontal);

            // カット位置の内側にある頂点はそのまま.
            if (invert ? border <= value : value <= border){ return vertex; }

            var oppositeIndex = FindOppositeVertexIndex(verts, quadStart, index, horizontal);

            if (oppositeIndex == -1){ return vertex; }

            var opposite = verts[oppositeIndex];

            var oppositeValue = GetAxisValue(opposite.position, horizontal);

            var length = value - oppositeValue;

            if (Mathf.Approximately(length, 0f)){ return vertex; }

            var t = Mathf.Clamp01((border - oppositeValue) / length);

            return LerpVertex(opposite, vertex, t);
        }

        /// <summary> quad内でカット軸方向の反対側にある頂点を探す </summary>
        private static int FindOppositeVertexIndex(List<UIVertex> verts, int quadStart, int index, bool horizontal)
        {
            var target = verts[index].position;

            var targetAxis = GetAxisValue(target, horizontal);
            var targetCross = GetCrossAxisValue(target, horizontal);

            for (var i = quadStart; i < quadStart + VertexCountPerQuad; i++)
            {
                if (i == index){ continue; }

                var position = verts[i].position;

                // カット軸に直交する方向が一致する = quadの同じ辺上にある頂点.
                if (PositionTolerance < Mathf.Abs(GetCrossAxisValue(position, horizontal) - targetCross)){ continue; }

                if (Mathf.Abs(GetAxisValue(position, horizontal) - targetAxis) <= PositionTolerance){ continue; }

                return i;
            }

            return -1;
        }

        private static float GetAxisValue(Vector3 position, bool horizontal)
        {
            return horizontal ? position.x : position.y;
        }

        private static float GetCrossAxisValue(Vector3 position, bool horizontal)
        {
            return horizontal ? position.y : position.x;
        }

        private static UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
        {
            var v = new UIVertex();

            v.position = Vector3.Lerp(a.position, b.position, t);
            v.normal = Vector3.Lerp(a.normal, b.normal, t);
            v.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
            v.color = Color.Lerp(a.color, b.color, t);
            v.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
            v.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
            v.uv2 = Vector4.Lerp(a.uv2, b.uv2, t);
            v.uv3 = Vector4.Lerp(a.uv3, b.uv3, t);

            return v;
        }

        /// <summary> 描画を更新する </summary>
        public void Refresh()
        {
            if (graphic == null){ return; }

            graphic.SetVerticesDirty();
        }
    }
}

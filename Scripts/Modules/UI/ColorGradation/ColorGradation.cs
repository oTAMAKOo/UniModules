
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Modules.UI
{
    [RequireComponent(typeof(Graphic))]
    public sealed class ColorGradation : BaseMeshEffect
    {
        //----- params -----

        public enum ModeType
        {
            Simple,
            Gradient,
        }

        public enum DirectionType
        {
            Vertical,
            Horizontal,
            Both,
        }

        //----- field -----

        [SerializeField]
        private ModeType mode = ModeType.Simple;
        [SerializeField]
        private DirectionType direction = DirectionType.Both;
        [SerializeField]
        private Color colorTop = Color.white;
        [SerializeField]
        private Color colorBottom = Color.black;
        [SerializeField]
        private Color colorLeft = Color.red;
        [SerializeField]
        private Color colorRight = Color.blue;
        [SerializeField]
        private Gradient verticalGradient = null;
        [SerializeField]
        private Gradient horizontalGradient = null;

        //----- property -----

        public ModeType Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                Refresh();
            }
        }

        public DirectionType Direction
        {
            get { return direction; }
            set
            {
                direction = value;
                Refresh();
            }
        }

        public Color ColorTop
        {
            get { return colorTop; }
            set
            {
                colorTop = value;
                Refresh();
            }
        }

        public Color ColorBottom
        {
            get { return colorBottom; }
            set
            {
                colorBottom = value;
                Refresh();
            }
        }

        public Color ColorLeft
        {
            get { return colorLeft; }
            set
            {
                colorLeft = value;
                Refresh();
            }
        }

        public Color ColorRight
        {
            get { return colorRight; }
            set
            {
                colorRight = value;
                Refresh();
            }
        }

        public Gradient VerticalGradient
        {
            get { return verticalGradient; }
            set
            {
                verticalGradient = value;
                Refresh();
            }
        }

        public Gradient HorizontalGradient
        {
            get { return horizontalGradient; }
            set
            {
                horizontalGradient = value;
                Refresh();
            }
        }

        //----- method -----

        public override void ModifyMesh(VertexHelper vh)
        {
            if (IsActive() == false){ return; }

            var vList = new List<UIVertex>();
            vh.GetUIVertexStream(vList);

            ModifyVertices(vList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vList);
        }

        public void ModifyVertices(List<UIVertex> vList)
        {
            if (IsActive() == false || vList == null || vList.Count == 0){ return; }

            switch (mode)
            {
                case ModeType.Simple:
                    ApplySimpleMode(vList);
                    break;

                case ModeType.Gradient:
                    // 6 頂点 = 1 quad の三角形ストリーム前提. 非 6 倍数は Simple へフォールバック.
                    if (vList.Count >= 6 && vList.Count % 6 == 0)
                    {
                        ApplyGradientMode(vList);
                    }
                    else
                    {
                        ApplySimpleMode(vList);
                    }
                    break;
            }
        }

        // 既存ロジック (4 色) - 互換維持のためロジックは変更しない.
        private void ApplySimpleMode(List<UIVertex> vList)
        {
            float topX = 0f, topY = 0f, bottomX = 0f, bottomY = 0f;

            for (var i = 0; i < vList.Count; i++)
            {
                var vertex = vList[i];
                topX = Mathf.Max(topX, vertex.position.x);
                topY = Mathf.Max(topY, vertex.position.y);
                bottomX = Mathf.Min(bottomX, vertex.position.x);
                bottomY = Mathf.Min(bottomY, vertex.position.y);
            }

            var width = topX - bottomX;
            var height = topY - bottomY;

            var tempVertex = vList[0];

            for (int i = 0; i < vList.Count; i++)
            {
                tempVertex = vList[i];

                var colorOrg = tempVertex.color;
                var colorV = Color.Lerp(colorBottom, colorTop, (tempVertex.position.y - bottomY) / height);
                var colorH = Color.Lerp(colorLeft, colorRight, (tempVertex.position.x - bottomX) / width);

                switch (direction)
                {
                    case DirectionType.Both:
                        tempVertex.color = colorOrg * colorV * colorH;
                        break;
                    case DirectionType.Vertical:
                        tempVertex.color = colorOrg * colorV;
                        break;
                    case DirectionType.Horizontal:
                        tempVertex.color = colorOrg * colorH;
                        break;
                }

                vList[i] = tempVertex;
            }
        }

        // 多段 Gradient (複数 quad 対応のメッシュ分割).
        private void ApplyGradientMode(List<UIVertex> vList)
        {
            // 全体 bounds を計算 (Gradient 評価のグローバル座標系).
            float topX = float.MinValue, topY = float.MinValue;
            float bottomX = float.MaxValue, bottomY = float.MaxValue;

            for (var i = 0; i < vList.Count; i++)
            {
                var p = vList[i].position;

                topX = Mathf.Max(topX, p.x);
                topY = Mathf.Max(topY, p.y);
                bottomX = Mathf.Min(bottomX, p.x);
                bottomY = Mathf.Min(bottomY, p.y);
            }

            var globalWidth = topX - bottomX;
            var globalHeight = topY - bottomY;

            if (globalWidth <= 0f || globalHeight <= 0f){ return; }

            var useVertical = direction == DirectionType.Vertical || direction == DirectionType.Both;
            var useHorizontal = direction == DirectionType.Horizontal || direction == DirectionType.Both;

            // Gradient のキー時間 (グローバル座標系) を一度だけ抽出.
            var yKeysGlobal = useVertical ? BuildKeyTimes(verticalGradient) : BuildEndpointTimes();
            var xKeysGlobal = useHorizontal ? BuildKeyTimes(horizontalGradient) : BuildEndpointTimes();

            // 元頂点を退避して vList をクリア (再構築先として再利用).
            var source = vList.ToArray();
            vList.Clear();

            var quadVerts = new List<UIVertex>(6);

            var quadCount = source.Length / 6;

            for (var quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                quadVerts.Clear();

                for (var i = 0; i < 6; i++)
                {
                    quadVerts.Add(source[quadIndex * 6 + i]);
                }

                ProcessQuad(quadVerts, vList, bottomX, bottomY, globalWidth, globalHeight, xKeysGlobal, yKeysGlobal, useVertical, useHorizontal);
            }
        }

        // 単一 quad を分割して resultList に三角形ストリームを追加.
        private void ProcessQuad(List<UIVertex> quadVerts, List<UIVertex> resultList,
            float globalBottomX, float globalBottomY, float globalWidth, float globalHeight,
            List<float> xKeysGlobal, List<float> yKeysGlobal,
            bool useVertical, bool useHorizontal)
        {
            // quad ローカル bounds.
            float qTopX = float.MinValue, qTopY = float.MinValue;
            float qBottomX = float.MaxValue, qBottomY = float.MaxValue;

            for (var i = 0; i < quadVerts.Count; i++)
            {
                var p = quadVerts[i].position;

                qTopX = Mathf.Max(qTopX, p.x);
                qTopY = Mathf.Max(qTopY, p.y);
                qBottomX = Mathf.Min(qBottomX, p.x);
                qBottomY = Mathf.Min(qBottomY, p.y);
            }

            var qWidth = qTopX - qBottomX;
            var qHeight = qTopY - qBottomY;

            // 退化 quad はそのまま追加.
            if (qWidth <= 0f || qHeight <= 0f)
            {
                for (var i = 0; i < quadVerts.Count; i++)
                {
                    resultList.Add(quadVerts[i]);
                }
                return;
            }

            // quad の global 範囲 (0~1).
            var qGlobalYMin = (qBottomY - globalBottomY) / globalHeight;
            var qGlobalYMax = (qTopY - globalBottomY) / globalHeight;
            var qGlobalXMin = (qBottomX - globalBottomX) / globalWidth;
            var qGlobalXMax = (qTopX - globalBottomX) / globalWidth;

            // 範囲内 global キーを quad ローカル t に射影.
            var yKeysLocal = ProjectKeyTimesToLocal(yKeysGlobal, qGlobalYMin, qGlobalYMax);
            var xKeysLocal = ProjectKeyTimesToLocal(xKeysGlobal, qGlobalXMin, qGlobalXMax);

            var bl = FindCorner(quadVerts, qBottomX, qBottomY);
            var br = FindCorner(quadVerts, qTopX, qBottomY);
            var tl = FindCorner(quadVerts, qBottomX, qTopY);
            var tr = FindCorner(quadVerts, qTopX, qTopY);

            // グリッド頂点生成 (位置/UV は quad ローカル, 色は global で Evaluate).
            var grid = new UIVertex[yKeysLocal.Count, xKeysLocal.Count];

            for (var iy = 0; iy < yKeysLocal.Count; iy++)
            {
                var ty = yKeysLocal[iy];
                var tyGlobal = Mathf.Lerp(qGlobalYMin, qGlobalYMax, ty);

                for (var ix = 0; ix < xKeysLocal.Count; ix++)
                {
                    var tx = xKeysLocal[ix];
                    var txGlobal = Mathf.Lerp(qGlobalXMin, qGlobalXMax, tx);

                    grid[iy, ix] = BuildGridVertex(bl, br, tl, tr, tx, ty, txGlobal, tyGlobal, useVertical, useHorizontal);
                }
            }

            // 三角形ストリーム展開.
            for (var iy = 0; iy < yKeysLocal.Count - 1; iy++)
            {
                for (var ix = 0; ix < xKeysLocal.Count - 1; ix++)
                {
                    var v00 = grid[iy, ix];
                    var v10 = grid[iy, ix + 1];
                    var v01 = grid[iy + 1, ix];
                    var v11 = grid[iy + 1, ix + 1];

                    // 三角形 1: BL, TL, TR.
                    resultList.Add(v00);
                    resultList.Add(v01);
                    resultList.Add(v11);

                    // 三角形 2: BL, TR, BR.
                    resultList.Add(v00);
                    resultList.Add(v11);
                    resultList.Add(v10);
                }
            }
        }

        // global キー時間のうち quad 範囲内のものを quad ローカル t に変換 (端点 0,1 を必ず含む).
        private static List<float> ProjectKeyTimesToLocal(List<float> globalKeys, float quadMin, float quadMax)
        {
            var result = new List<float>();

            result.Add(0f);
            result.Add(1f);

            var quadRange = quadMax - quadMin;

            if (quadRange <= 0f)
            {
                return result;
            }

            for (var i = 0; i < globalKeys.Count; i++)
            {
                var tg = globalKeys[i];

                if (tg > quadMin && tg < quadMax)
                {
                    var tLocal = (tg - quadMin) / quadRange;
                    result.Add(tLocal);
                }
            }

            result.Sort();

            const float Epsilon = 0.0001f;

            for (var i = result.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(result[i] - result[i - 1]) < Epsilon)
                {
                    result.RemoveAt(i);
                }
            }

            return result;
        }

        // 指定位置に最も近い頂点を 4 角の頂点として取得.
        private static UIVertex FindCorner(List<UIVertex> vList, float x, float y)
        {
            var nearest = vList[0];
            var minSqr = float.MaxValue;

            for (var i = 0; i < vList.Count; i++)
            {
                var p = vList[i].position;
                var dx = p.x - x;
                var dy = p.y - y;
                var sqr = dx * dx + dy * dy;

                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    nearest = vList[i];
                }
            }

            return nearest;
        }

        // Gradient のキー時間を抽出して [0, 1] を含めた昇順リストを返す.
        private static List<float> BuildKeyTimes(Gradient gradient)
        {
            var times = new List<float>();

            times.Add(0f);
            times.Add(1f);

            if (gradient != null)
            {
                var colorKeys = gradient.colorKeys;

                if (colorKeys != null)
                {
                    for (var i = 0; i < colorKeys.Length; i++)
                    {
                        times.Add(Mathf.Clamp01(colorKeys[i].time));
                    }
                }

                var alphaKeys = gradient.alphaKeys;

                if (alphaKeys != null)
                {
                    for (var i = 0; i < alphaKeys.Length; i++)
                    {
                        times.Add(Mathf.Clamp01(alphaKeys[i].time));
                    }
                }
            }

            times.Sort();

            // 重複除外 (浮動小数誤差吸収).
            const float Epsilon = 0.0001f;

            for (var i = times.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(times[i] - times[i - 1]) < Epsilon)
                {
                    times.RemoveAt(i);
                }
            }

            return times;
        }

        private static List<float> BuildEndpointTimes()
        {
            return new List<float> { 0f, 1f };
        }

        // 4 角の頂点を bilinear 補間してグリッド頂点を生成. 位置/UV は quad ローカル, 色は global で Evaluate.
        private UIVertex BuildGridVertex(UIVertex bl, UIVertex br, UIVertex tl, UIVertex tr,
            float tx, float ty, float txGlobal, float tyGlobal,
            bool useVertical, bool useHorizontal)
        {
            var bottom = LerpVertex(bl, br, tx);
            var top = LerpVertex(tl, tr, tx);
            var v = LerpVertex(bottom, top, ty);

            // Gradient による色を計算して元の頂点色に乗算.
            var gradColor = Color.white;

            if (useVertical && useHorizontal)
            {
                gradColor = SafeEvaluate(verticalGradient, tyGlobal) * SafeEvaluate(horizontalGradient, txGlobal);
            }
            else if (useVertical)
            {
                gradColor = SafeEvaluate(verticalGradient, tyGlobal);
            }
            else if (useHorizontal)
            {
                gradColor = SafeEvaluate(horizontalGradient, txGlobal);
            }

            v.color = v.color * gradColor;

            return v;
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

        private static Color SafeEvaluate(Gradient gradient, float t)
        {
            if (gradient == null){ return Color.white; }

            return gradient.Evaluate(t);
        }

        /// <summary> 再生中にGradient Colorを更新する </summary>
        public void Refresh()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}

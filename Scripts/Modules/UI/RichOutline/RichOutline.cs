﻿
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Modules.UI
{
    public class RichOutline : BaseMeshEffect
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color color = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField]
        private Vector2 distance = Vector2.zero;
        [SerializeField]
        private int copyCount = 4;
        [SerializeField]
        private bool useGraphicAlpha = true;

        //----- property -----

        public Color effectColor
        {
            get { return color; }
            set { color = value; }
        }

        public Vector2 effectDistance
        {
            get { return distance; }
            set { distance = value; }
        }

        //----- method -----

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) { return; }

            var list = new List<UIVertex>();
            vh.GetUIVertexStream(list);

            ModifyVertices(list);

            vh.Clear();
            vh.AddUIVertexTriangleStream(list);
        }

        private void ModifyVertices(List<UIVertex> verts)
        {
            var start = 0;
            var end = verts.Count;

            for (var n = 0; n < copyCount; ++n)
            {
                var rad = 2.0f * Mathf.PI * n / copyCount;

                var x = effectDistance.x * Mathf.Cos(rad);
                var y = effectDistance.y * Mathf.Sin(rad);

                ApplyShadow(verts, start, end, x, y);

                start = end;
                end = verts.Count;
            }
        }

        private void ApplyShadow(List<UIVertex> verts, int start, int end, float x, float y)
        {
            for (var i = start; i < end; ++i)
            {
                var vt = verts[i];
                verts.Add(vt);

                var v = vt.position;

                v.x += x;
                v.y += y;
                vt.position = v;

                Color32 newColor = effectColor;

                if (useGraphicAlpha)
                {
                    newColor.a = (byte)((newColor.a * verts[i].color.a) / 255.0f);
                }

                vt.color = newColor;
                verts[i] = vt;
            }
        }
    }
}
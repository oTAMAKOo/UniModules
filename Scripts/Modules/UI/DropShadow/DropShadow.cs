﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Serialize;

namespace Modules.UI
{
    public class DropShadow : BaseMeshEffect
    {
        //----- params -----

        public enum Placement
        {
            Simple,
            Custom,
        }

        [Flags]
        public enum Direction
        {
            Top     =  1 << 0,
            Right   =  1 << 1,
            Bottom  =  1 << 2,
            Left    =  1 << 3,
        }

        [Serializable]
        public class Info
        {
            public IntNullable direction = new IntNullable(null);
            public Vector2 distance = Vector2.zero;
        }

        //----- field -----

        // Editorからリフレクションで値を設定するので警告を抑制.

        #pragma warning disable CS0414

        [SerializeField]
        private Placement placement = Placement.Simple;
        [SerializeField]
        private Direction direction = 0;

        #pragma warning restore CS0414

        [SerializeField]
        private Color color = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField]
        private Info[] infos = new Info[0];
        [SerializeField]
        private bool useGraphicAlpha = true;

        //----- property -----

        public Color effectColor
        {
            get { return color; }
            set { color = value; }
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

            foreach (var info in infos)
            {
                if(info == null) { continue; }

                ApplyShadow(verts, start, end, info.distance.x, info.distance.y);

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
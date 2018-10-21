/*
uGui-Effect-Tool
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Modules.UI
{
    [RequireComponent(typeof(Graphic))]
    public class UIColorGradation : BaseMeshEffect
    {
        public enum DIRECTION
        {
            Vertical,
            Horizontal,
            Both,
        }

        public DIRECTION direction = DIRECTION.Both;
        public Color colorTop = Color.white;
        public Color colorBottom = Color.black;
        public Color colorLeft = Color.red;
        public Color colorRight = Color.blue;

        public override void ModifyMesh (VertexHelper vh)
        {
            if (IsActive() == false) { return; }

            var vList = new List<UIVertex>();
            vh.GetUIVertexStream(vList);

            ModifyVertices(vList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vList);
        }

        public void ModifyVertices (List<UIVertex> vList)
        {
            if (IsActive() == false || vList == null || vList.Count == 0)
            {
                return;
            }

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
                    case DIRECTION.Both:
                        tempVertex.color = colorOrg * colorV * colorH;
                        break;
                    case DIRECTION.Vertical:
                        tempVertex.color = colorOrg * colorV;
                        break;
                    case DIRECTION.Horizontal:
                        tempVertex.color = colorOrg * colorH;
                        break;
                }

                vList[i] = tempVertex;
            }
        }

        /// <summary>
        /// Refresh Gradient Color on playing.
        /// </summary>
        public void Refresh()
        {
            if (graphic != null) {
                graphic.SetVerticesDirty();
            }
        }
    }
}

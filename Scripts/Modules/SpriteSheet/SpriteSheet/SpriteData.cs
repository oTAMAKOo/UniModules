﻿﻿
using UnityEngine;
using System;
using System.Collections;

namespace Modules.SpriteSheet
{
    [System.Serializable]
    public class SpriteData
    {
        //----- params -----

        //----- field -----

        public string name = "Sprite";

        public string guid = null;

        public int x = 0;
        public int y = 0;

        public int width = 0;
        public int height = 0;

        public int borderLeft = 0;
        public int borderRight = 0;
        public int borderTop = 0;
        public int borderBottom = 0;

        public float u0 = 0f;
        public float v0 = 0f;
        public float u1 = 1f;
        public float v1 = 1f;

        //----- property -----

        //----- method -----

        public bool HasBorder
        {
            get { return (borderLeft | borderRight | borderTop | borderBottom) != 0; }
        }

        public void SetSize(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void SetRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public void SetBorder(int left, int bottom, int right, int top)
        {
            borderLeft = left;
            borderBottom = bottom;
            borderRight = right;
            borderTop = top;
        }

        public void CopyFrom(SpriteData source)
        {
            name = source.name;            
            guid = source.guid;

            x = source.x;
            y = source.y;

            width = source.width;
            height = source.height;

            borderLeft = source.borderLeft;
            borderRight = source.borderRight;
            borderTop = source.borderTop;
            borderBottom = source.borderBottom;
        }

        public void CopyBorderFrom(SpriteData source)
        {
            borderLeft = source.borderLeft;
            borderRight = source.borderRight;
            borderTop = source.borderTop;
            borderBottom = source.borderBottom;
        }
    }
}

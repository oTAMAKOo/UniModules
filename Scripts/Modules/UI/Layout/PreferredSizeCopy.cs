﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using Extensions;
using Extensions.Serialize;
using UniRx;

namespace Modules.UI.Layout
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class PreferredSizeCopy : LayoutElement, ILayoutSelfController
    {
        //----- params -----

        [Serializable]
        public class LayoutInfo
        {
            [SerializeField]
            private bool enable = false;
            [SerializeField]
            private float padding = 0f;
            [SerializeField]
            private FloatNullable min = new FloatNullable(null);
            [SerializeField]
            private FloatNullable max = new FloatNullable(null);
            [SerializeField]
            private FloatNullable flexible = new FloatNullable(null);

            public bool Enable
            {
                get { return enable; }
                set { enable = value; }
            }

            public float Padding
            {
                get { return padding; }
                set { padding = value; }
            }

            public FloatNullable Min
            {
                get { return min; }
                set { min = value; }
            }

            public FloatNullable Max
            {
                get { return max; }
                set { max = value; }
            }

            public FloatNullable Flexible
            {
                get { return flexible; }
                set { flexible = value; }
            }
        }

        //----- field -----

        [SerializeField]
        private RectTransform copySource = null;

        [SerializeField]
        private LayoutInfo horizontal = new LayoutInfo();
        [SerializeField]
        private LayoutInfo vertical = new LayoutInfo();

        private Vector2? prevPreferredSize = null;

        //----- property -----

        public override float preferredWidth
        {
            get
            {
                if (copySource == null || !IsActive() || !horizontal.Enable)
                {
                    return -1f;
                }

                var width = LayoutUtility.GetPreferredWidth(copySource);

                if (horizontal.Min.HasValue)
                {
                    width = width < horizontal.Min.Value ? horizontal.Min.Value : width;
                }

                if (horizontal.Max.HasValue)
                {
                    width = width > horizontal.Max.Value ? horizontal.Max.Value : width;
                }

                return width + horizontal.Padding;
            }
        }

        public float? horizontalMin
        {
            get { return horizontal.Min; }
            set { horizontal.Min = value; }
        }

        public float? horizontalMax
        {
            get { return horizontal.Max; }
            set { horizontal.Max = value; }
        }

        public override float preferredHeight
        {
            get
            {
                if (copySource == null || !IsActive() || !vertical.Enable)
                {
                    return -1f;
                }

                var height = LayoutUtility.GetPreferredHeight(copySource);

                if (vertical.Min.HasValue)
                {
                    height = height < vertical.Min.Value ? vertical.Min.Value : height;
                }

                if (vertical.Max.HasValue)
                {
                    height = height > vertical.Max.Value ? vertical.Max.Value : height;
                }

                return height + vertical.Padding;
            }
        }

        public float? verticalMin
        {
            get { return vertical.Min; }
            set { vertical.Min = value; }
        }

        public float? verticalMax
        {
            get { return vertical.Max; }
            set { vertical.Max = value; }
        }

        public override float flexibleWidth
        {
            get
            {
                return horizontal.Flexible.GetValueOrDefault(-1);
            }

            set
            {
                horizontal.Flexible = value;
                SetDirty();
            }
        }

        public override float flexibleHeight
        {
            get
            {
                return vertical.Flexible.GetValueOrDefault(-1);
            }

            set
            {
                vertical.Flexible = value;
                SetDirty();
            }
        }

        public override int layoutPriority
        {
            get { return 2; }
        }

        //----- method -----

        void Update()
        {
            var preferredSize = copySource.GetPreferredSize();

            if (prevPreferredSize.HasValue)
            {
                if(prevPreferredSize.Value != preferredSize)
                {
                    SetDirty();
                }
            }

            prevPreferredSize = preferredSize;
        }


        public void UpdateLayoutImmediate()
        {
            SetLayoutHorizontal();
            SetLayoutVertical();
        }

        public void SetLayoutHorizontal()
        {
            if (copySource == null) { return; }

            if (!horizontal.Enable) { return; }

            var rectTransform = transform as RectTransform;
            
            rectTransform.sizeDelta = Vector.SetX(rectTransform.sizeDelta, preferredWidth);
        }

        public void SetLayoutVertical()
        {
            if (copySource == null) { return; }

            if (!vertical.Enable) { return; }

            var rectTransform = transform as RectTransform;

            rectTransform.sizeDelta = Vector.SetY(rectTransform.sizeDelta, preferredHeight);
        }
    }
}

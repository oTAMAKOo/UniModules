
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Extensions;
using Extensions.Serialize;
using Unity.Linq;

namespace Modules.UI.Layout
{
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public sealed class PreferredSizeCopy : LayoutElement, ILayoutSelfController
	{
		//----- params -----

		[Serializable]
		public sealed class LayoutInfo
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
        [SerializeField]
        private int priority = 2;

		private Vector2? prevPreferredSize = null;
        
        #if UNITY_EDITOR

        private DrivenRectTransformTracker drivenRectTransformTracker = new DrivenRectTransformTracker();

        #endif

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
			get { return priority; }
		}

		//----- method -----

        public void SetCopySource(RectTransform target)
        {
            copySource = target;

            UpdatePreferredSize();
        }

		protected override void OnEnable()
		{
			base.OnEnable();

            UpdateLayoutImmediate();
			UpdatePreferredSize();
		}

        #if UNITY_EDITOR

        protected override void OnDisable()
        {
            base.OnDisable();

            drivenRectTransformTracker.Clear();
        }

        #endif

		void LateUpdate()
		{
			UpdatePreferredSize();
		}

		private void UpdatePreferredSize()
		{
			if (copySource == null) { return; }

            var rectTransform = transform as RectTransform;

            var changed = false;

			var preferredSize = copySource.GetPreferredSize();

			if (prevPreferredSize.HasValue)
			{
				if (prevPreferredSize.Value != preferredSize)
				{
                    changed = true;
                }
			}
			else
			{
                changed = true;
			}

			prevPreferredSize = preferredSize;

            if (changed)
            {
                var sizeDelta = rectTransform.sizeDelta;

                if (horizontal.Enable)
                {
                    sizeDelta.x = preferredWidth;
                }

                if (vertical.Enable)
                {
                    sizeDelta.y = preferredHeight;
                }

                rectTransform.sizeDelta = sizeDelta;

                SetDirty();
            }

            #if UNITY_EDITOR

            var drivenProperties = DrivenTransformProperties.None;

            if (horizontal.Enable)
            {
                drivenProperties = drivenProperties.SetFlag(DrivenTransformProperties.SizeDeltaX);
            }
            else
            {
                drivenProperties = drivenProperties.RemoveFlag(DrivenTransformProperties.SizeDeltaX);
            }

            if (vertical.Enable)
            {
                drivenProperties = drivenProperties.SetFlag(DrivenTransformProperties.SizeDeltaY);
            }
            else
            {
                drivenProperties = drivenProperties.RemoveFlag(DrivenTransformProperties.SizeDeltaY);
            }

            drivenRectTransformTracker.Clear();
            drivenRectTransformTracker.Add(this, rectTransform,drivenProperties);

            #endif
		}

		public void UpdateLayoutImmediate()
		{
            if (copySource == null) { return; }

            if (!horizontal.Enable && !vertical.Enable) { return; }

            copySource.ForceRebuildLayoutGroup();

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

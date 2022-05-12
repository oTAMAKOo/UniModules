
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Serialize;

namespace Modules.UI.ScreenRotation
{
	[RequireComponent(typeof(RectTransform))]
	public sealed class RotationRoot : UIBehaviour
    {
        //----- params -----

        //----- field -----

		[SerializeField]
		private RotateType rotateType = RotateType.None;
		[SerializeField]
		private bool ignoreManage = false;
		[SerializeField]
		private FloatNullable originWidth = null;
		[SerializeField]
		private FloatNullable originHeight = null;


		//----- property -----

		public RotateType RotateType
		{
			get { return rotateType; }

			set
			{
				if (rotateType != value)
				{
					rotateType = value;

					Apply();
				}
			}
		}

        //----- method -----
		
		protected override void Start()
		{
			base.Start();

			if (!ignoreManage)
			{
				RotationManager.Instance.Add(this);
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!ignoreManage)
			{
				RotationManager.Instance.Remove(this);
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			Apply();
		}

		private void SetOriginSize()
		{
			var rt = transform as RectTransform;

			if (!originWidth.HasValue)
			{
				originWidth.Value = rt.GetWidth();
			}

			if (!originHeight.HasValue)
			{
				originHeight.Value = rt.GetHeight();
			}
		}

		private void Apply()
		{
			var rt = transform as RectTransform;

			SetOriginSize();

			switch (rotateType)
			{
				case RotateType.None:
					{
						rt.SetWidth(originWidth.Value);
						rt.SetHeight(originHeight.Value);
						rt.rotation = Quaternion.Euler(0f, 0f, 0f);

						originWidth = null;
						originHeight = null;
					}
					break;

				case RotateType.Degree90:
					{
						rt.SetWidth(originHeight.Value);
						rt.SetHeight(originWidth.Value);
						rt.rotation = Quaternion.Euler(0f, 0f, 90);
					}
					break;

				case RotateType.DegreeMinus90:
					{
						rt.SetWidth(originHeight.Value);
						rt.SetHeight(originWidth.Value);
						rt.rotation = Quaternion.Euler(0f, 0f, -90);
					}
					break;

				case RotateType.Reverse:
					{
						rt.SetWidth(originWidth.Value);
						rt.SetHeight(originHeight.Value);
						rt.rotation = Quaternion.Euler(0f, 0f, 180);
					}
					break;
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
		}
	}
}

using UnityEngine;
using UnityEngine.UI;
using Extensions.Serialize;
using UniRx;

namespace Modules.UI.ScreenRotation
{
    [ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public sealed class RotationRoot : MonoBehaviour
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
		
		void Awake()
		{
            if (Application.isPlaying)
            {
                if (!ignoreManage)
                {
                    RotationManager.Instance.Add(this);
                }
            }
		}

		void OnDestroy()
		{
            if (Application.isPlaying)
            {
                if (!ignoreManage)
                {
                    RotationManager.Instance.Remove(this);
                }
            }
		}

        void OnEnable()
		{
            // OnEnableのタイミングでApplyしてもRectTransformに反映されないので最初のUpdateで実行.
            Observable.EveryUpdate()
                .First()
                .TakeUntilDisable(this)
                .Subscribe(_ => Apply())
                .AddTo(this);
        }

		private void SetOriginSize()
		{
			var rt = transform as RectTransform;

            if (originWidth == null)
            {
                originWidth = new FloatNullable(null);
            }

			if (!originWidth.HasValue)
			{
				originWidth.Value = rt.sizeDelta.x;
			}

            if (originHeight == null)
            {
                originHeight = new FloatNullable(null);
            }

            if (!originHeight.HasValue)
			{
				originHeight.Value = rt.sizeDelta.y;
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
                        rt.sizeDelta = new Vector2(originWidth.Value, originHeight.Value);
						rt.localRotation = Quaternion.Euler(0f, 0f, 0f);

						originWidth = null;
						originHeight = null;
					}
					break;

				case RotateType.Degree90:
					{
						rt.sizeDelta = new Vector2(originHeight.Value, originWidth.Value);
						rt.localRotation = Quaternion.Euler(0f, 0f, 90);
					}
					break;

				case RotateType.DegreeMinus90:
					{
                        rt.sizeDelta = new Vector2(originHeight.Value, originWidth.Value);
						rt.localRotation = Quaternion.Euler(0f, 0f, -90);
					}
					break;

				case RotateType.Reverse:
					{
                        rt.sizeDelta = new Vector2(originWidth.Value, originHeight.Value);
						rt.localRotation = Quaternion.Euler(0f, 0f, 180);
					}
					break;
			}
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
		}
    }
}

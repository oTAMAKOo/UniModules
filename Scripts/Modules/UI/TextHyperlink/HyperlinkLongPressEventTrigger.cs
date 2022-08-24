
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Extensions;
using UniRx;

namespace Modules.UI.TextHyperlink
{
	[RequireComponent(typeof(TextMeshProUGUI))]
	public sealed class HyperlinkLongPressEventTrigger : HyperlinkEventHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		//----- params -----

		//----- field -----

		[SerializeField]
		private float longPressTime = 1.0f;

		private bool isPointerDown = false;
		private bool longPressTriggered = false;
		
		private float pressTime = 0f;

		private PointerEventData eventData = null;

		//----- property -----

		public float LongPressTime
		{
			get { return longPressTime; }
			set { longPressTime = value; }
		}

		//----- method -----

		protected override void OnEnable()
		{
			base.OnEnable();

			Observable.EveryUpdate()
				.TakeUntilDisable(this)
				.Subscribe(_ => CheckLongPressStatus())
				.AddTo(this);
		}

		private void CheckLongPressStatus()
		{
			if (eventData == null){ return; }

			if ( isPointerDown && !longPressTriggered ) 
			{
				if (longPressTime < pressTime) 
				{
					longPressTriggered = true;
					HyperlinkAction(eventData.position);

					pressTime = 0f;
				}
			}

			pressTime += Time.deltaTime;
		}

		public void OnPointerDown(PointerEventData eventData) 
		{
			this.eventData = eventData;
			
			isPointerDown = true;
			longPressTriggered = false;
			pressTime = 0f;
		}
 
		public void OnPointerUp(PointerEventData eventData) 
		{
			this.eventData = null;

			isPointerDown = false;
			pressTime = 0f;
		}

		public void OnPointerExit(PointerEventData eventData) 
		{
			this.eventData = null;

			isPointerDown = false;
			pressTime = 0f;
		}
	}
}
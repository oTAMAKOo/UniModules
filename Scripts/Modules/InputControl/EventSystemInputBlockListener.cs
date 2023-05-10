
using UnityEngine;
using UnityEngine.EventSystems;
using Extensions;

namespace Modules.InputControl.Components
{
	[RequireComponent(typeof(EventSystem))]
    public sealed class EventSystemInputBlockListener : InputBlockListener
    {
        //----- params -----

        //----- field -----

		private EventSystem target = null;

		private bool blocking = false;

		private bool? origin = null;

        //----- property -----

		protected override InputBlockType BlockType { get { return InputBlockType.EventSystem; } }

		public bool IsBlocking { get { return blocking; } }

        //----- method -----

		protected override void UpdateInputBlock(bool isBlock)
		{
			if (target == null)
			{
				target = UnityUtility.GetComponent<EventSystem>(gameObject);
			}

			if (target != null)
			{
				if (isBlock)
				{
					if (!blocking)
					{
						origin = target.enabled;
					}

					target.enabled = false;

					blocking = true;
				}
				else
				{
					if (origin.HasValue)
					{
						target.enabled = origin.Value;
					}

					origin = null;

					blocking = false;
				}
			}
		}
    }
}
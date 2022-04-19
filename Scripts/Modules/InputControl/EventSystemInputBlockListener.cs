
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

		private bool? prevState = null;

        //----- property -----

		protected override InputBlockType BlockType { get { return InputBlockType.EventSystem; } }

        //----- method -----

		protected override void UpdateInputBlock(bool isBlock)
		{
			if (target == null)
			{
				target = UnityUtility.GetComponent<EventSystem>(gameObject);
			}

			if (target != null)
			{
				if (!prevState.HasValue)
				{
					prevState = target.enabled;
				}

				target.enabled = !isBlock && prevState.Value;
			}
		}
    }
}
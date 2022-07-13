
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Extensions;

namespace Modules.UI.TextHyperlink
{
	[RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class HyperlinkClickEventTrigger : HyperlinkEventHandler, IPointerClickHandler
    {
        //----- params -----

        //----- field -----

		//----- property -----

        //----- method -----

		public void OnPointerClick(PointerEventData e)
		{
			HyperlinkAction(e.position);
		}
    }
}
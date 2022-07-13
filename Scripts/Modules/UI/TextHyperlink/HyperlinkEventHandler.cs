
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;
using TMPro;
using Extensions;

namespace Modules.UI.TextHyperlink
{
	[RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class HyperlinkEventHandler : UIBehaviour
    {
        //----- params -----

        //----- field -----

		private TextMeshProUGUI textMeshPro = null;

		private Subject<string> onHyperlinkAction = null;

        //----- property -----

        //----- method -----

		protected override void Start()
		{
			base.Start();

			textMeshPro = UnityUtility.GetComponent<TextMeshProUGUI>(gameObject);

			if (textMeshPro != null)
			{
				textMeshPro.richText = true;
				textMeshPro.raycastTarget = true;
			}
		}
		
		protected void HyperlinkAction(Vector3 mousePosition)
		{
			if (textMeshPro == null){ return; }

			var canvas = textMeshPro.canvas;
			
			var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

			if (camera == null){ return; }
			
			var index  = TMP_TextUtilities.FindIntersectingLink(textMeshPro, mousePosition, camera);

			if (index == -1){ return; }

			var linkInfo = textMeshPro.textInfo.linkInfo[index];
			
			var link = linkInfo.GetLinkID();

			if (onHyperlinkAction != null)
			{
				onHyperlinkAction.OnNext(link);
			}
		}

		public IObservable<string> OnHyperlinkActionAsObservable()
		{
			return onHyperlinkAction ?? (onHyperlinkAction = new Subject<string>());
		}
    }
}
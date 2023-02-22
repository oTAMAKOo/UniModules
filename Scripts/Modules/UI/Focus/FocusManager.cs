
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Focus
{
    public sealed class FocusManager : Singleton<FocusManager>
    {
        //----- params -----

        //----- field -----

		private HashSet<string> targets = null;

		private Subject<Unit> onUpdateFocus = null;

        //----- property -----

		public Canvas FocusCanvas { get; private set; }

		//----- method -----

		private FocusManager()
		{
			targets = new HashSet<string>();
		}

		public void SetFocusCanvas(Canvas canvas)
		{
			FocusCanvas = canvas;

			UpdateFocus();
		}

		public void AddFocus(string focusId)
		{
			if (targets.Contains(focusId)){ return; }
			
			targets.Remove(focusId);

			UpdateFocus();
		}

		public void RemoveFocus(string focusId)
		{
			if (!targets.Contains(focusId)){ return; }

			targets.Remove(focusId);

			UpdateFocus();

		}

		public void RemoveAllFocus()
		{
			targets.Clear();

			UpdateFocus();
		}

		public bool Contains(string focusId)
		{
			return targets.Contains(focusId);
		}

		private void UpdateFocus()
		{

		}

		public IObservable<Unit> OnUpdateFocusAsObservable()
		{
			return onUpdateFocus ?? (onUpdateFocus = new Subject<Unit>());
		}
    }
}
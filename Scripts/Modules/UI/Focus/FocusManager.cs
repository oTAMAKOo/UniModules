
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

		private HashSet<string> focusIds = null;

        private List<FocusTarget> targets = null;

		private Subject<Unit> onUpdateFocus = null;

        //----- property -----

		public Canvas FocusCanvas { get; private set; }

		public bool HasFocus { get { return focusIds.Any(); } }

        public IEnumerable<FocusTarget> Targets { get { return targets; } }

		//----- method -----

		private FocusManager()
		{
            focusIds = new HashSet<string>();
            targets = new List<FocusTarget>();
		}

		public void SetFocusCanvas(Canvas canvas)
		{
			FocusCanvas = canvas;

			UpdateFocus();
		}

		public void AddFocus(string focusId)
		{
			if (focusIds.Contains(focusId)){ return; }
			
            focusIds.Add(focusId);

			UpdateFocus();
		}

		public void RemoveFocus(string focusId)
		{
			if (!focusIds.Contains(focusId)){ return; }

            focusIds.Remove(focusId);

			UpdateFocus();

		}

		public void RemoveAllFocus()
		{
            focusIds.Clear();
            targets.Clear();

			UpdateFocus();
		}

		public bool Contains(string focusId)
		{
			return focusIds.Contains(focusId);
		}

		private void UpdateFocus()
		{
			if (onUpdateFocus != null)
			{
				onUpdateFocus.OnNext(Unit.Default);
			}
		}

        public void Join(FocusTarget target)
        {
            if (targets.Contains(target)){ return; }

            targets.Add(target);
        }

        public void Leave(FocusTarget target)
        {
            if (!targets.Contains(target)){ return; }

            targets.Remove(target);
        }

		public IObservable<Unit> OnUpdateFocusAsObservable()
		{
			return onUpdateFocus ?? (onUpdateFocus = new Subject<Unit>());
		}
    }
}
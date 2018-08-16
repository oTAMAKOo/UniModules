
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.UI;

namespace Modules.SoftMask
{
    public class VirtualScrollSoftMask : UIBehaviour, IVirtualScrollExtension
    {
        public IObservable<Unit> OnCreateItem(GameObject item)
        {
            return Observable.ReturnUnit();
        }

        public IObservable<Unit> OnItemInitialize(GameObject item)
        {
            var softMaskTarget = UnityUtility.GetOrAddComponent<SoftMaskTarget>(item);

            return softMaskTarget.SetupMask();
        }

        public IObservable<Unit> OnUpdateContents(GameObject[] items)
        {
            return Observable.ReturnUnit();
        }
    }
}

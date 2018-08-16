
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using SoftMasking;

namespace Modules.SoftMask
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class SoftMaskTarget : UIBehaviour
    {
        //----- params -----

        private const int MaxFrameModify = 50;

        //----- field -----

        //----- property -----

        //----- method -----

        public IObservable<Unit> SetupMask(bool immediate = false)
        {
            return Observable.FromCoroutine(() => SetupMaskInternal(immediate));
        }

        private IEnumerator SetupMaskInternal(bool immediate)
        {
            var count = 0;

            var targets = gameObject.DescendantsAndSelf().ToArray();

            foreach (var target in targets)
            {
                var softMaskable = UnityUtility.GetComponent<SoftMaskable>(target);

                if (softMaskable == null)
                {
                    UnityUtility.AddComponent<SoftMaskable>(target);

                    if (!immediate)
                    {
                        if (MaxFrameModify <= count++)
                        {
                            yield return null;

                            count = 0;
                        }
                    }
                }
            }
        }
    }
}

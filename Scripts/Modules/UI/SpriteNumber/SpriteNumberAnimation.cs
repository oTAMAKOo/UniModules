
using UnityEngine;
using System;
using UniRx;

namespace Modules.UI.SpriteNumber
{
    public sealed class SpriteNumberAnimation : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Subject<Unit> onComplete = null;

        //----- property -----

        public int Index { get; private set; }

        //----- method -----

        public void Setup(int index)
        {
            Index = index;

            onComplete = null;
        }

        public void CompleteAnimation()
        {
            if (onComplete != null)
            {
                onComplete.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnCompleteAsObservable()
        {
            return onComplete ?? (onComplete = new Subject<Unit>());
        }
    }
}

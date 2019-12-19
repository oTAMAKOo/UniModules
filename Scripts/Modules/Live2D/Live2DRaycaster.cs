using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Modules.OffScreenRendering;

#if ENABLE_LIVE2D

using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Raycasting;

#endif

namespace Modules.Live2D
{
    public sealed class Live2DRaycaster : RenderTextureRaycaster
    {
        #if ENABLE_LIVE2D

        //----- params -----

        //----- field -----

        private CubismRaycaster raycaster = null;

        private Subject<CubismDrawable> onRaycastHit = null;

        //----- property -----

        //----- method -----

        public void SetParams(CubismRaycaster raycaster)
        {
            this.raycaster = raycaster;
        }

        public IObservable<CubismDrawable> OnRaycastHitAsObservable()
        {
            return onRaycastHit ?? (onRaycastHit = new Subject<CubismDrawable>());
        }

        #endif

        protected override void Raycast(Ray ray)
        {
            #if ENABLE_LIVE2D

            var hit = new CubismRaycastHit[4];

            var hitCount = raycaster.Raycast(ray, hit);

            for (var index = 0; index < hitCount; index++)
            {
                if (onRaycastHit != null)
                {
                    onRaycastHit.OnNext(hit[index].Drawable);
                }
            }

            #endif
        }
    }
}

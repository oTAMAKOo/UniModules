
#if ENABLE_LIVE2D
﻿
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Raycasting;
using Modules.OffScreenRendering;

namespace Modules.Live2D
{
    public class Live2DRaycaster : RenderTextureRaycaster
    {
        //----- params -----

        //----- field -----

        private CubismRaycaster raycaster = null;

        protected Subject<CubismDrawable> onRaycastHit = null;

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
  
        protected override void Raycast(Ray ray)
        {
            var hit = new CubismRaycastHit[4];

            var hitCount = raycaster.Raycast(ray, hit);

            for (var index = 0; index < hitCount; index++)
            {
                if (onRaycastHit != null)
                {
                    onRaycastHit.OnNext(hit[index].Drawable);
                }
            }
        }
    }
}

#endif
﻿﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
  using System.Linq;
  using Extensions;
  using UniRx;

namespace Modules.OffScreenRendering
{
    public class Collider2DRayCast : RenderTextureRaycaster
    {
        //----- params -----

        //----- field -----

        protected Subject<GameObject[]> onRaycastHit = null;

        //----- property -----

        //----- method -----

        protected override void Raycast(Ray ray)
        {
            var maxDistance = 10f;

            var raycastHits = Physics2D.RaycastAll((Vector2)ray.origin, (Vector2)ray.direction, maxDistance);
            
            if (raycastHits.Any())
            {
                if (onRaycastHit != null)
                {
                    var gameObjects = raycastHits.Select(x => x.transform.gameObject).ToArray();

                    onRaycastHit.OnNext(gameObjects);
                }
            }
        }

        public IObservable<GameObject[]> OnRaycastHitAsObservable()
        {
            return onRaycastHit ?? (onRaycastHit = new Subject<GameObject[]>());
        }
    }
}

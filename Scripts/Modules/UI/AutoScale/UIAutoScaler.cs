﻿﻿
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI
{
    [ExecuteInEditMode]
	public class UIAutoScaler : UIBehaviour
	{
		//----- params -----

		//----- field -----

        [SerializeField]
        private RectTransform target = null;
        [SerializeField]
        private Vector2 baseSize = Vector2.zero;

        private Vector2? targetSize = null;

        //----- property -----

        //----- method -----

        void Update()
        {
            if(UnityUtility.IsNull(target)) { return; }

            UpdateScale();
        }

        public void UpdateScale()
        {
            var size = target.GetSize();

            if (targetSize.HasValue && targetSize.Value.x == size.x && targetSize.Value.y == size.y)
            {
                return;
            }

            targetSize = size;

            var rt = transform as RectTransform;

            rt.SetSize(baseSize);

            rt.localScale = new Vector3()
            {
                x = targetSize.Value.x / baseSize.x,
                y = targetSize.Value.y / baseSize.y,
                z = 1f,
            };
        }
	}
}

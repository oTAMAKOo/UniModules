﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI
{
	public class SnapScrollRect : ScrollRect
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private float smoothness = 10f;
        [SerializeField]
        private float fitRange = 10f;

        private GameObject snapTarget = null;
        private Vector3? targetPosition = null;
        private bool dragging = false;
        private GameObject[] snapTargets = null;

        private Subject<GameObject> onSnap = null;

        private RectTransform rectTransform = null;

        //----- property -----

        public RectTransform RectTransform
        {
            get { return rectTransform ?? (rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject)); }
        }

        //----- method -----

        public void SetSnapTargets(GameObject[] items)
        {
            snapTargets = items;
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            dragging = true;
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);

            UpdateSnapTarget();

            dragging = false;
        }

        public void SetSnapTarget(GameObject target)
        {
            snapTarget = target;

            if (snapTarget != null)
            {
                var position = content.InverseTransformPoint(snapTarget.transform.position);

                targetPosition = new Vector3()
                {
                    x = horizontal ? -position.x : content.localPosition.x,
                    y = vertical ? -position.y : content.localPosition.y,
                };
            }
        }

        void Update()
        {
            if (!dragging)
            {
                if (targetPosition.HasValue)
                {
                    if (content.localPosition != targetPosition.Value)
                    {
                        content.localPosition = Vector2.Lerp(content.localPosition, targetPosition.Value, smoothness * Time.deltaTime);

                        if (Vector3.Distance(content.localPosition, targetPosition.Value) < fitRange)
                        {
                            content.localPosition = targetPosition.Value;
                            velocity = Vector2.zero;

                            if (onSnap != null && snapTarget != null)
                            {
                                onSnap.OnNext(snapTarget.gameObject);
                            }

                            StopSnap();
                        }
                    }
                }
            }
        }

        private void StopSnap()
        {
            snapTarget = null;
            targetPosition = null;
        }

        private void UpdateSnapTarget()
        {
            float? nearest = null;

            GameObject nearestTarget = null;

            foreach (var target in snapTargets)
            {
                if (!UnityUtility.IsActiveInHierarchy(target)) { continue; }

                var rt = UnityUtility.GetComponent<RectTransform>(target);

                if (nearest.HasValue)
                {
                    var distance = Vector3.Distance(transform.position, rt.position);

                    if (distance < nearest.Value)
                    {
                        nearest = distance;
                        nearestTarget = target;
                    }
                }
                else
                {
                    nearest = Vector3.Distance(transform.position, rt.position);
                    nearestTarget = target;
                }
            }

            SetSnapTarget(nearestTarget);
        }

        public IObservable<GameObject> OnSnapAsObservable()
        {
            return onSnap ?? (onSnap = new Subject<GameObject>());
        }
    }
}
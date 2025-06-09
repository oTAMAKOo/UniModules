
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Linq;
using System;
using System.Linq;
using UniRx;

namespace Modules.UI
{
    public sealed class DragObject : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        //----- params -----

        public enum Rounding
        {
            Soft,
            Hard
        }

        //----- field -----

        [SerializeField]
        private RectTransform target = null;
        [SerializeField] 
        private bool horizontal = true;
        [SerializeField]
        private bool vertical = true;
        [SerializeField]
        private bool inertia = true;
        [SerializeField]
        private Rounding inertiaRounding = Rounding.Hard;
        [SerializeField]
        private float dampeningRate = 9f;
        [SerializeField]
        private bool constrainWithinCanvas = false;
        [SerializeField]
        private bool constrainDrag = true;
        [SerializeField]
        private bool constrainInertia = true;

        private Canvas canvas = null;
        private RectTransform canvasRectTransform = null;
        private Vector2 pointerStartPosition = Vector2.zero;
        private Vector2 targetStartPosition = Vector2.zero;
        private Vector2 velocity = Vector2.zero;
        private bool dragging = false;
        private Vector2 lastPosition = Vector2.zero;

        private Subject<PointerEventData> onBeginDrag = null;

        private Subject<PointerEventData> onEndDrag = null;

        private Subject<PointerEventData> onDrag = null;

        //----- property -----

        public RectTransform Target
        {
            get { return target; }
            set { target = value; }
        }

        public bool Horizontal
        {
            get { return horizontal; }
            set { horizontal = value; }
        }

        public bool Vertical
        {
            get { return vertical; }
            set { vertical = value; }
        }

        public bool Inertia
        {
            get { return inertia; }
            set { inertia = value; }
        }

        public float DampeningRate
        {
            get { return dampeningRate; }
            set { dampeningRate = value; }
        }

        public bool ConstrainWithinCanvas
        {
            get { return constrainWithinCanvas; }
            set { constrainWithinCanvas = value; }
        }

        //----- method -----

        protected override void Awake()
        {
            base.Awake();

            UpdateCanvas();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            UpdateCanvas();
        }

        private void UpdateCanvas()
        {
            var root = target != null ? target.gameObject : gameObject;

            canvas = root.Ancestors().OfComponent<Canvas>().FirstOrDefault();

            canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;
        }

        public override bool IsActive()
        {
            return base.IsActive() && target != null;
        }

        public void StopMovement()
        {
            velocity = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!IsActive()){ return; }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, data.position, data.pressEventCamera, out pointerStartPosition);

            targetStartPosition = target.anchoredPosition;
            velocity = Vector2.zero;
            dragging = true;

            if (onBeginDrag != null)
            {
                onBeginDrag.OnNext(data);
            }
        }

        public void OnEndDrag(PointerEventData data)
        {
            dragging = false;

            if (!IsActive()){ return; }

            if (onEndDrag != null)
            {
                onEndDrag.OnNext(data);
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsActive() || canvas == null){ return; }

            var mousePos = Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, data.position, data.pressEventCamera, out mousePos);

            if (constrainWithinCanvas && constrainDrag)
            {
                mousePos = ClampToCanvas(mousePos);
            }

            var newPosition = targetStartPosition + (mousePos - pointerStartPosition);

            if (!horizontal)
            {
                newPosition.x = target.anchoredPosition.x;
            }

            if (!vertical)
            {
                newPosition.y = target.anchoredPosition.y;
            }

            target.anchoredPosition = newPosition;

            if (onDrag != null)
            {
                onDrag.OnNext(data);
            }
        }

        void LateUpdate()
        {
            if (!target){ return; }

            var unscaledDeltaTime = Time.unscaledDeltaTime;

            if (dragging && inertia)
            {
                var to = (target.anchoredPosition - lastPosition) / unscaledDeltaTime;

                velocity = Vector3.Lerp(velocity, to, unscaledDeltaTime * 10f);
            }

            lastPosition = target.anchoredPosition;

            if (!dragging && velocity != Vector2.zero)
            {
                var anchoredPosition = target.anchoredPosition;

                Dampen(ref velocity, dampeningRate, unscaledDeltaTime);

                for (var i = 0; i < 2; i++)
                {
                    if (inertia)
                    {
                        anchoredPosition[i] += velocity[i] * unscaledDeltaTime;
                    }
                    else
                    {
                        velocity[i] = 0f;
                    }
                }

                if (velocity != Vector2.zero)
                {
                    if (!horizontal)
                    {
                        anchoredPosition.x = target.anchoredPosition.x;
                    }
                    if (!vertical)
                    {
                        anchoredPosition.y = target.anchoredPosition.y;
                    }

                    if (constrainWithinCanvas && constrainInertia && canvasRectTransform != null)
                    {
                        var canvasCorners = new Vector3[4];
                        canvasRectTransform.GetWorldCorners(canvasCorners);

                        var targetCorners = new Vector3[4];
                        target.GetWorldCorners(targetCorners);

                        if (targetCorners[0].x < canvasCorners[0].x || targetCorners[2].x > canvasCorners[2].x)
                        {
                            anchoredPosition.x = target.anchoredPosition.x;
                        }

                        if (targetCorners[3].y < canvasCorners[3].y || targetCorners[1].y > canvasCorners[1].y)
                        {
                            anchoredPosition.y = target.anchoredPosition.y;
                        }
                    }

                    if (anchoredPosition != target.anchoredPosition)
                    {
                        var pos = Vector2.zero;

                        switch (inertiaRounding)
                        {
                            case Rounding.Hard:
                                pos.x = Mathf.Round(anchoredPosition.x / 2f) * 2f;
                                pos.y = Mathf.Round(anchoredPosition.y / 2f) * 2f;
                                break;

                            case Rounding.Soft:
                            default:
                                pos.x = Mathf.Round(anchoredPosition.x);
                                pos.y = Mathf.Round(anchoredPosition.y);
                                break;
                        }

                        target.anchoredPosition = pos;
                    }
                }
            }
        }

        private void Dampen(ref Vector2 velocity, float strength, float delta)
        {
            if (delta > 1f)
            {
                delta = 1f;
            }

            var dampeningFactor = 1f - strength * 0.001f;
            var ms = Mathf.RoundToInt(delta * 1000f);
            var totalDampening = Mathf.Pow(dampeningFactor, ms);
            
            velocity *= totalDampening;
        }

        private Vector2 ClampToScreen(Vector2 position)
        {
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    var clampedX = Mathf.Clamp(position.x, 0f, Screen.width);
                    var clampedY = Mathf.Clamp(position.y, 0f, Screen.height);

                    return new Vector2(clampedX, clampedY);
                }
            }

            return position;
        }

        private Vector2 ClampToCanvas(Vector2 position)
        {
            if (canvasRectTransform != null)
            {
                var corners = new Vector3[4];

                canvasRectTransform.GetLocalCorners(corners);

                var clampedX = Mathf.Clamp(position.x, corners[0].x, corners[2].x);
                var clampedY = Mathf.Clamp(position.y, corners[3].y, corners[1].y);

                return new Vector2(clampedX, clampedY);
            }

            return position;
        }

        public IObservable<PointerEventData> OnBeginDragAsObservable()
        {
            return onBeginDrag ?? (onBeginDrag = new Subject<PointerEventData>());
        }

        public IObservable<PointerEventData> OnEndDragAsObservable()
        {
            return onEndDrag ?? (onEndDrag = new Subject<PointerEventData>());
        }

        public IObservable<PointerEventData> OnDragAsObservable()
        {
            return onDrag ?? (onDrag = new Subject<PointerEventData>());
        }
    }
}
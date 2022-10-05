
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Particle;

namespace Modules.TouchEffect
{
    public abstract class TouchEffectManager<TInstance> : SingletonMonoBehaviour<TInstance> where TInstance : TouchEffectManager<TInstance>, new()
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject rootObject = null;
        [SerializeField]
        private GameObject touchEffectPrefab = null;
        [SerializeField]
        private GameObject dragEffectPrefab = null;
        [SerializeField]
        private float intervalDistance = 8f;

        protected Camera renderCamera = null;

        private GameObject touchEffectRoot = null;
        private GameObject dragEffectRoot = null;

        private Vector3? prevPosition = null;
        private bool? isTouchPlatform = null;

        private Queue<ParticlePlayer> cachedTouchEffects = null;
        private Queue<ParticlePlayer> cachedDragEffects = null;

        private bool isInitialized = false;

        //----- property -----

        public virtual bool IsEnable { get; set; }

        public abstract int TargetLayer { get; }

        public abstract int TargetLayerMask { get; }

        //----- method -----

        public virtual void Initialize()
        {
            if (isInitialized) { return; }

            renderCamera = UnityUtility.FindCameraForLayer(TargetLayerMask).FirstOrDefault();

            if (touchEffectPrefab != null)
            {
                cachedTouchEffects = new Queue<ParticlePlayer>();
                touchEffectRoot = UnityUtility.CreateEmptyGameObject(rootObject, "Cache (Touch)", true);
            }

            if (dragEffectPrefab != null)
            {
                cachedDragEffects = new Queue<ParticlePlayer>();
                dragEffectRoot = UnityUtility.CreateEmptyGameObject(rootObject, "Cache (Drag)", true);
            }

            isInitialized = true;
        }

        public void Show()
        {
            UnityUtility.SetActive(touchEffectRoot, true);
            UnityUtility.SetActive(dragEffectRoot, true);
        }

        public void Hide()
        {
            UnityUtility.SetActive(touchEffectRoot, false);
            UnityUtility.SetActive(dragEffectRoot, false);
        }

        void Update()
        {
            if (!isInitialized) { return; }

            if (!IsEnable) { return; }

            if (IsTouchPlatform())
            {
                for (var i = 0; i < Input.touchCount; ++i)
                {
                    var touch = Input.GetTouch(i);
                    HandleTouch(touch.phase, touch.fingerId, touch.position);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    HandleTouch(TouchPhase.Began, 0, Input.mousePosition);
                }
                if (Input.GetMouseButton(0))
                {
                    HandleTouch(TouchPhase.Moved, 0, Input.mousePosition);
                }
                if (Input.GetMouseButtonUp(0))
                {
                    HandleTouch(TouchPhase.Ended, 0, Input.mousePosition);
                }
            }
        }

        private void SetTouchEffect(Vector3 screenPosition)
        {
            if (touchEffectPrefab == null) { return; }

            ParticlePlayer particleController = null;

            if (cachedTouchEffects.Count != 0)
            {
                particleController = cachedTouchEffects.Dequeue();
            }
            else
            {
                particleController = UnityUtility.Instantiate<ParticlePlayer>(touchEffectRoot, touchEffectPrefab);

                UnityUtility.SetLayer(TargetLayer, particleController.gameObject, true);

                // キャッシュ目的なのでDeactiveを指定.
                particleController.EndActionType = EndActionType.Deactivate;

                particleController.OnEndAsObservable()
                    .Subscribe(endEffect =>
                        {
                            if (endEffect != null)
                            {
                                cachedTouchEffects.Enqueue(endEffect);
                            }
                        })
                    .AddTo(this);
            }

            if (!UnityUtility.IsNull(particleController))
            {
                SetTouchEffectPosition(particleController, screenPosition);

                particleController.Play().Forget();
            }
        }

        protected virtual void SetTouchEffectPosition(ParticlePlayer particleController, Vector3 screenPosition)
        {
            var originPosition = renderCamera.ScreenToWorldPoint(screenPosition);
            
            particleController.transform.position = new Vector3(originPosition.x, originPosition.y);
        }

        private void SetDragEffect(Vector3 screenPosition)
        {
            if (dragEffectPrefab == null) { return; }

            ParticlePlayer particleController = null;

            if (cachedDragEffects.Count != 0)
            {
                particleController = cachedDragEffects.Dequeue();
            }
            else
            {
                particleController = UnityUtility.Instantiate<ParticlePlayer>(dragEffectRoot, dragEffectPrefab);

                UnityUtility.SetLayer(TargetLayer, particleController.gameObject, true);

                // キャッシュ目的なのでDeactiveを指定.
                particleController.EndActionType = EndActionType.Deactivate;

                particleController.OnEndAsObservable().Subscribe(x => cachedDragEffects.Enqueue(x)).AddTo(this);
            }
            
            SetTouchEffectPosition(particleController, screenPosition);

            particleController.Play().Forget();
        }

        /// <summary>
        /// タッチ環境か.
        /// </summary>
        private bool IsTouchPlatform()
        {
            if (isTouchPlatform.HasValue)
            {
                return isTouchPlatform.Value;
            }

            var platform = Application.platform;

            isTouchPlatform = platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer;

            return isTouchPlatform.Value;
        }

        #region Input

        private void HandleTouch(TouchPhase touchPhase, int touchFingerId, Vector3 touchPosition)
        {
			if (renderCamera == null)
			{
				renderCamera = UnityUtility.FindCameraForLayer(TargetLayerMask).FirstOrDefault();
			}

			if (renderCamera == null){ return; }

            if (touchFingerId == 0)
            {
                switch (touchPhase)
                {
                    case TouchPhase.Began:
                        SetTouchEffect(touchPosition);
                        break;
                    case TouchPhase.Moved:
                        if (prevPosition.HasValue && intervalDistance < Vector3.Distance(prevPosition.Value, touchPosition))
                        {
                            SetDragEffect(touchPosition);
                        }
                        break;
                    case TouchPhase.Ended:
                        // 必要になったら処理を追加.
                        break;
                }

                prevPosition = touchPosition;
            }
        }

        #endregion
    }
}

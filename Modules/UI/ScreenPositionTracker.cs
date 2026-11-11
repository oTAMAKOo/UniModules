
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.UI
{
    [ExecuteAlways]
    public sealed class ScreenPositionTracker : UIBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Transform target = null;
        [SerializeField]
        private Vector2 offset = Vector2.zero;

        private Camera targetCamera = null;

        //----- property -----

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

            var rootCanvas = gameObject.Ancestors().OfComponent<Canvas>().FirstOrDefault(x => x.isRootCanvas);

            if (rootCanvas != null)
            {
                targetCamera = rootCanvas.worldCamera;
            }
        }

        void Update()
        {
            RefreshPosition();
        }

        private void RefreshPosition()
        {
            if (target == null){ return; }

            if (targetCamera == null){ return; }

            var screenPosition = targetCamera.WorldToScreenPoint(target.position);
            
            transform.position = targetCamera.ScreenToWorldPoint(screenPosition)  + offset.ToVector3();
        }

        public void SetTarget(Transform target, Vector2 offset)
        {
            this.target = target;
            this.offset = offset;

            RefreshPosition();
        }
    }
}
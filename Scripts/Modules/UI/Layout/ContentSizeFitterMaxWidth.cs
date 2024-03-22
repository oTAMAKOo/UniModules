
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

using Object = UnityEngine.Object;

namespace Modules.UI.Layout
{
    [ExecuteAlways]
    [RequireComponent(typeof(ContentSizeFitter))]
    public sealed class ContentSizeFitterMaxWidth : UIBehaviour, ILayoutElement
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private float maxWidth = 0;

        private ILayoutElement layoutElement = null;

        private ContentSizeFitter contentSizeFitter = null;

        //----- property -----

        private ILayoutElement LayoutElement
        {
            get
            {
                return layoutElement ?? (layoutElement = GetComponents<ILayoutElement>().FirstOrDefault(x => this != (Object)x));
            }
        }

        public float MaxWidth
        {
            get { return maxWidth; }
            set { maxWidth = value; }
        }

        public float minWidth 
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return LayoutElement.minWidth;
            }
        }

        public float flexibleWidth 
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return LayoutElement.flexibleWidth;
            }
        }

        public float minHeight
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return LayoutElement.minHeight;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return maxWidth < LayoutElement.preferredWidth ? maxWidth : LayoutElement.preferredWidth;
            }
        }

        public float preferredHeight 
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return LayoutElement.preferredHeight;
            }
        }

        public float flexibleHeight 
        {
            get
            {
                if (LayoutElement == null) { return 0; }

                return LayoutElement.flexibleHeight;
            }
        }

        public int layoutPriority 
        {
            get { return int.MaxValue; }
        }

        //----- method -----

        void Update()
        {
            var rt = transform as RectTransform;

            if (rt == null){ return; }
            
            if (LayoutElement == null) { return; }

            if (contentSizeFitter == null)
            {
                contentSizeFitter = GetComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = LayoutElement.preferredWidth > maxWidth ? 
                                              ContentSizeFitter.FitMode.Unconstrained : 
                                              ContentSizeFitter.FitMode.PreferredSize;
           
            if (LayoutElement.preferredWidth > maxWidth)
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            }
            else
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        public void CalculateLayoutInputHorizontal()
        {
            if (LayoutElement == null) { return; }

            LayoutElement.CalculateLayoutInputHorizontal();
        }

        public void CalculateLayoutInputVertical()
        {
            if (LayoutElement == null) { return; }

            LayoutElement.CalculateLayoutInputVertical();
        }
    }
}
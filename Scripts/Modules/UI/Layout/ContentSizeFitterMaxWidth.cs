
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Modules.UI.Layout
{
    [ExecuteAlways]
    [RequireComponent(typeof(ContentSizeFitter))]
    public sealed class ContentSizeFitterMaxWidth : LayoutElement
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private float maxWidth = 0;

        private ContentSizeFitter contentSizeFitter = null;
        private ILayoutElement layoutElement = null;

        [NonSerialized]
        private bool setup = false;

        //----- property -----

        public override float preferredWidth
        {
            get
            {
                Setup();

                return maxWidth < layoutElement.preferredWidth ? maxWidth : preferredWidth;
            }
        }

        public float MaxWidth { get { return maxWidth; } }
        
        //----- method -----

        private void Setup()
        {
            if (setup){ return; }

            contentSizeFitter = GetComponent<ContentSizeFitter>();
            layoutElement = GetComponent<ILayoutElement>();

            setup = true;
        }

        void Update()
        {
            Setup();

            var rt = transform as RectTransform;

            if (rt == null){ return; }

            contentSizeFitter.horizontalFit = layoutElement.preferredWidth > maxWidth ? 
                                              ContentSizeFitter.FitMode.Unconstrained : 
                                              ContentSizeFitter.FitMode.PreferredSize;
           
            if (layoutElement.preferredWidth > maxWidth)
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            }
            else
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }
}
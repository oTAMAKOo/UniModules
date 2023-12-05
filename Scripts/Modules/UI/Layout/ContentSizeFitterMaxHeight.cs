
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Modules.UI.Layout
{
    [ExecuteAlways]
    [RequireComponent(typeof(ContentSizeFitter))]
    public sealed class ContentSizeFitterMaxHeight : LayoutElement
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private float maxHeight = 0;

        private ContentSizeFitter contentSizeFitter = null;
        private ILayoutElement layoutElement = null;

        [NonSerialized]
        private bool setup = false;

        //----- property -----

        public override float preferredHeight
        {
            get
            {
                Setup();

                return maxHeight < layoutElement.preferredWidth ? maxHeight : preferredWidth;
            }
        }

        public float MaxWidth { get { return maxHeight; } }
        
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

            contentSizeFitter.verticalFit = layoutElement.preferredHeight > maxHeight ? 
                                              ContentSizeFitter.FitMode.Unconstrained : 
                                              ContentSizeFitter.FitMode.PreferredSize;
           
            if (layoutElement.preferredHeight > maxHeight)
            {
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
            }
            else
            {
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }
}
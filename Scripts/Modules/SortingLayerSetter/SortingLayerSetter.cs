﻿﻿
using UnityEngine;
using Unity.Linq;
using Extensions;

namespace Modules.SortingLayerSetter
{
    [ExecuteInEditMode]
    public sealed class SortingLayerSetter : MonoBehaviour
    {
        //----- field -----

        [SerializeField]
        private Constants.SortingLayer sortingLayer = Constants.SortingLayer.Default;
        [SerializeField]
        private int sortingOrder = 0;
        [SerializeField]
        private bool applyChildObjects = false;

        //----- property -----

        public Constants.SortingLayer SortingLayer
        {
            get { return sortingLayer; }
            set
            {
                sortingLayer = value;
                SetSortingLayer();
            }
        }
        
        public int SortingOrder
        {
            get { return sortingOrder; }
            set
            {
                sortingOrder = value;
                SetSortingLayer();
            }
        }

        public bool ApplyChildObjects
        {
            get { return applyChildObjects; }
            set { applyChildObjects = value; }
        }

        //----- method -----

        void Awake()
        {
            SetSortingLayer();
        }

        public void SetSortingLayer()
        {
            var renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sortingLayerID = (int)sortingLayer;
                renderer.sortingOrder = sortingOrder;
            }

            if (applyChildObjects)
            {
                var childRenderers = gameObject.Descendants().OfComponent<Renderer>();

                childRenderers.ForEach(x => x.sortingOrder = sortingOrder);
            }
        }
    }
}

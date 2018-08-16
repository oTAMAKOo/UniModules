﻿﻿using UnityEngine;
using Unity.Linq;

namespace Modules.SortingLayerSetter
{
    [ExecuteInEditMode]
    public class SortingLayerSetter : MonoBehaviour
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
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sortingLayerID = (int)sortingLayer;
                renderer.sortingOrder = sortingOrder;
            }

            if (applyChildObjects)
            {
                var childObjects = gameObject.Children();

                foreach (var childObject in childObjects)
                {
                    renderer = childObject.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        renderer.sortingLayerID = (int)sortingLayer;
                        renderer.sortingOrder = sortingOrder;
                    }
                }
            }
        }
    }
}

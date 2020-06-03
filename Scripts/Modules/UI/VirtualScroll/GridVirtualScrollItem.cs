
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.UI.VirtualScroll;

namespace Modules.UI
{
    namespace VirtualScroll
    {
        public interface IGirdScrollItem
        {
            void SetScrollDirection(Direction direction);
        }
    }

    public abstract class GirdScrollItem<T, TComponent> : VirtualScrollItem<GridVirtualScroll<T>.GridElement>, IGirdScrollItem
        where T : class where TComponent : Component
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject elementPrefab = null;
        [SerializeField]
        protected TextAnchor alignment = TextAnchor.MiddleCenter;
        [SerializeField]
        protected float spacing = 0.0f;
        [SerializeField]
        protected RectOffset padding = new RectOffset();
        [SerializeField, Tooltip("No Require")]
        private GameObject elementParent = null;

        private List<TComponent> elements = null;

        private GameObject parentObject = null;

        private Direction scrollViewDirection = Direction.Vertical;

        //----- property -----

        //----- method -----

        public void SetScrollDirection(Direction direction)
        {
            scrollViewDirection = direction;
        }

        public override IObservable<Unit> Initialize()
        {
            elements = new List<TComponent>();

            if (parentObject == null)
            {
                parentObject = elementParent ?? UnityUtility.CreateEmptyGameObject(gameObject, "Elements");
            }

            var parentObjectRt = UnityUtility.GetOrAddComponent<RectTransform>(parentObject);

            parentObjectRt.FillRect();
            
            HorizontalOrVerticalLayoutGroup layoutGroup = null;

            switch (scrollViewDirection)
            {
                case Direction.Vertical:
                    layoutGroup = UnityUtility.GetComponent<HorizontalLayoutGroup>(parentObject);
                    break;

                case Direction.Horizontal:
                    layoutGroup = UnityUtility.GetComponent<VerticalLayoutGroup>(parentObject);
                    break;
            }

            if (layoutGroup == null)
            {
                SetupLayoutGroup(parentObject);
            }

            return base.Initialize();
        }

        private void SetupLayoutGroup(GameObject target)
        {
            HorizontalOrVerticalLayoutGroup layoutGroup = null;

            switch (scrollViewDirection)
            {
                case Direction.Vertical:
                    layoutGroup = UnityUtility.AddComponent<HorizontalLayoutGroup>(target);
                    break;

                case Direction.Horizontal:
                    layoutGroup = UnityUtility.AddComponent<VerticalLayoutGroup>(target);
                    break;
            }

            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            layoutGroup.padding = padding;
            layoutGroup.childAlignment = alignment;
            layoutGroup.spacing = spacing;
        }

        protected override IObservable<Unit> UpdateContents(GridVirtualScroll<T>.GridElement info)
        {
            return Observable.FromMicroCoroutine(() => UpdateContentsInternal(info));
        }

        private IEnumerator UpdateContentsInternal(GridVirtualScroll<T>.GridElement info)
        {
            if (elements == null) { yield break; }

            var elementObjectCount = elements.Count;

            var elementCount = info.Elements.Count;

            // 多い.
            if (elementCount < elementObjectCount)
            {
                var num = elementObjectCount - elementCount;

                for (var i = 0; i < num; i++)
                {
                    elements.RemoveAt(0);
                }
            }
            // 足りない.
            else if (elementObjectCount < elementCount)
            {
                var num = elementCount - elementObjectCount;

                var newElements = UnityUtility.Instantiate<TComponent>(parentObject, elementPrefab, num);

                elements.AddRange(newElements);
            }
            
            var observers = new List<IObservable<Unit>>();
            
            for (var i = 0; i < elementCount; i++)
            {
                var elementIndex = info.StartIndex + i;
                var elementInfo = info.Elements.ElementAtOrDefault(i);
                var element = elements.ElementAtOrDefault(i);

                var observer = Observable.Defer(() => UpdateContents(elementIndex, elementInfo, element));

                observers.Add(observer);
            }

            var updateContentsYield = observers.WhenAll().ToYieldInstruction();

            while (!updateContentsYield.IsDone)
            {
                yield return null;
            }
        }

        protected abstract IObservable<Unit> UpdateContents(int index, T content, TComponent element);
    }
}

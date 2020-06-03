
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

    public abstract class GridVirtualScrollItem<T, TComponent> : VirtualScrollItem<GridVirtualScroll<T>.GridElement>, IGirdScrollItem
        where T : class where TComponent : Component
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject elementPrefab = null;
        [SerializeField]
        private GameObject elementParent = null;

        private List<TComponent> elements = null;

        private Direction scrollViewDirection = Direction.Vertical;

        //----- property -----

        public IReadOnlyList<TComponent> Elements { get { return elements; } }

        //----- method -----

        public void SetScrollDirection(Direction direction)
        {
            scrollViewDirection = direction;
        }

        public override IObservable<Unit> Initialize()
        {
            elements = new List<TComponent>();

            var parentObjectRt = elementParent.transform as RectTransform;

            if (parentObjectRt != null)
            {
                parentObjectRt.FillRect();
            }
            
            return base.Initialize();
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

                var newElements = UnityUtility.Instantiate<TComponent>(elementParent, elementPrefab, num).ToArray();

                foreach (var newElement in newElements)
                {
                    OnCreateElement(newElement);
                }

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

        /// <summary> 要素初期化処理 </summary>
        protected virtual void OnCreateElement(TComponent element) { }
        
        /// <summary> 要素更新処理 </summary>
        protected abstract IObservable<Unit> UpdateContents(int index, T content, TComponent element);
    }
}

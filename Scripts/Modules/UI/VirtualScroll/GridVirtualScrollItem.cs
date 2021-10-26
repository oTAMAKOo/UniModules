
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI
{
    public abstract class GridVirtualScrollItem<T, TComponent> : VirtualScrollItem<GridVirtualScroll<T>.GridElement>
        where T : class where TComponent : Component
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject elementPrefab = null;
        [SerializeField]
        private GameObject elementParent = null;

        private List<TComponent> elements = null;
        
        //----- property -----

        public IReadOnlyList<TComponent> Elements { get { return elements; } }

        //----- method -----

        public override IObservable<Unit> Initialize()
        {
            elements = new List<TComponent>();

            var parentObjectRt = elementParent.transform as RectTransform;

            if (parentObjectRt != null)
            {
                UnityUtility.GetOrAddComponent<Canvas>(gameObject);

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

            try
            {
                // 足りない.
                if (elementObjectCount < elementCount)
                {
                    var num = elementCount - elementObjectCount;

                    var newElements = UnityUtility.Instantiate<TComponent>(elementParent, elementPrefab, num).ToArray();

                    foreach (var newElement in newElements)
                    {
                        OnCreateElement(newElement);
                    }

                    elements.AddRange(newElements);
                }

                // 一旦全てを非表示.
                elements.ForEach(x => UnityUtility.SetActive(x, false));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var activeElements = new List<TComponent>();
            var observers = new List<IObservable<Unit>>();
            
            for (var i = 0; i < elementCount; i++)
            {
                var elementIndex = info.StartIndex + i;
                var elementInfo = info.Elements.ElementAtOrDefault(i);
                var element = elements.ElementAtOrDefault(i);

                if (element != null && elementInfo != null)
                {
                    activeElements.Add(element);

                    var observer = Observable.Defer(() => UpdateContents(elementIndex, elementInfo, element));

                    observers.Add(observer);
                }
            }

            var updateContentsYield = observers.WhenAll().ToYieldInstruction(false);

            while (!updateContentsYield.IsDone)
            {
                yield return null;
            }

            if (updateContentsYield.HasError)
            {
                Debug.LogException(updateContentsYield.Error);
            }

            // 有効なオブジェクトだけ表示.
            activeElements.ForEach(x => UnityUtility.SetActive(x, true));
        }

        /// <summary> 要素初期化処理 </summary>
        protected virtual void OnCreateElement(TComponent element) { }
        
        /// <summary> 要素更新処理 </summary>
        protected abstract IObservable<Unit> UpdateContents(int index, T content, TComponent element);
    }
}

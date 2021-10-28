
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public override UniTask Initialize()
        {
            elements = new List<TComponent>();

            var parentObjectRt = elementParent.transform as RectTransform;

            if (parentObjectRt != null)
            {
                UnityUtility.GetOrAddComponent<Canvas>(gameObject);

                UnityUtility.GetOrAddComponent<GraphicRaycaster>(gameObject);

                parentObjectRt.FillRect();
            }

            return UniTask.CompletedTask;
        }
        
        protected override async UniTask UpdateContents(GridVirtualScroll<T>.GridElement info)
        {
            if (elements == null) { return; }

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
            var tasks = new List<UniTask>();
            
            for (var i = 0; i < elementCount; i++)
            {
                var elementIndex = info.StartIndex + i;
                var elementInfo = info.Elements.ElementAtOrDefault(i);
                var element = elements.ElementAtOrDefault(i);

                if (element != null && elementInfo != null)
                {
                    activeElements.Add(element);

                    var task = UniTask.Defer(() => UpdateContents(elementIndex, elementInfo, element));

                    tasks.Add(task);
                }
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // 有効なオブジェクトだけ表示.
            activeElements.ForEach(x => UnityUtility.SetActive(x, true));
        }

        /// <summary> 要素初期化処理 </summary>
        protected virtual void OnCreateElement(TComponent element) { }
        
        /// <summary> 要素更新処理 </summary>
        protected abstract UniTask UpdateContents(int index, T content, TComponent element);
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;
using UniRx;

namespace Modules.UI
{
    public interface IVirtualScrollItem
    {
        int Index { get; }
    }

    public abstract class VirtualScrollItem<T> : MonoBehaviour, IVirtualScrollItem where T : class
    {
        //----- params -----

        //----- field -----

        private RectTransform rectTransform = null;

        //----- property -----

        public int Index { get; private set; }

        public T Content { get; private set; }

        public RectTransform RectTransform
        {
            get { return rectTransform ?? (rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject)); }
        }

        //----- method -----

        public void SetContent(int index, IReadOnlyList<T> contents)
        {
            Index = index;

            Content = contents != null ? contents.ElementAtOrDefault(index, null) : null;
        }

        public IObservable<Unit> UpdateItem()
        {
            return UpdateContents(Content);
        }

        /// <summary> 初期化. </summary>
        public virtual IObservable<Unit> Initialize() { return Observable.ReturnUnit(); }

        protected abstract IObservable<Unit> UpdateContents(T content);
    }
}

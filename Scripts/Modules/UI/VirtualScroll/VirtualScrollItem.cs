﻿
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

    public abstract class VirtualScrollItem<T> : MonoBehaviour, IVirtualScrollItem
    {
        //----- params -----

        //----- field -----

        private RectTransform rectTransform = null;

        //----- property -----

        public int Index { get; private set; }

        public RectTransform RectTransform
        {
            get { return rectTransform ?? (rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject)); }
        }

        //----- method -----
        
        public IObservable<Unit> UpdateItem(int index, IReadOnlyList<T> contents)
        {
            Index = index;

            if (contents == null) { return Observable.ReturnUnit(); }

            if (index < 0 || contents.Count <= index) { return Observable.ReturnUnit(); }

            return UpdateContents(contents[index]);
        }

        /// <summary> 初期化. </summary>
        public virtual IObservable<Unit> Initialize() { return Observable.ReturnUnit(); }

        protected abstract IObservable<Unit> UpdateContents(T content);
    }
}

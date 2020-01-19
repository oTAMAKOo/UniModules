﻿
using UnityEngine;
using System;
using Extensions;
using UniRx;

namespace Modules.UI
{
    public abstract class VirtualScrollItem<T> : MonoBehaviour
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
        
        /// <summary> 初期化. </summary>
        public virtual IObservable<Unit> Initialize() { return Observable.ReturnUnit(); }

        public IObservable<Unit> UpdateItem(int index, T[] contents)
        {
            Index = index;

            if (contents == null) { return Observable.ReturnUnit(); }

            if (index < 0 || contents.Length <= index) { return Observable.ReturnUnit(); }

            return UpdateContents(contents[index]);
        }

        protected abstract IObservable<Unit> UpdateContents(T content);
    }
}

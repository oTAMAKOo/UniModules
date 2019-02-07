﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
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
        public virtual void Initialize() { }

        /// <summary>
        /// 初期化(非同期).
        /// 多数のオブジェクトの生成などの初期化処理はこのメソッドで非同期で実行する.
        /// </summary>
        public virtual IObservable<Unit> InitializeAsync() { return null; }

        public void UpdateItem(int index, T[] contents)
        {
            Index = index;

            if (contents != null)
            {
                if (0 <= index && index < contents.Length)
                {
                    UpdateContents(contents[index]);
                }
            }
        }

        protected abstract void UpdateContents(T content);
    }
}

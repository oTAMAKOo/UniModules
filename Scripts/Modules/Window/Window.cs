﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.InputControl;

namespace Modules.Window
{
    public abstract class Window : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private bool deleteOnClose = true;
        [SerializeField]
        private int displayPriority = 0;

        private Subject<Unit> onOpen = null;
        private Subject<Unit> onClose = null;

        //----- property -----

        /// <summary> ウィンドウが閉じた時に自動でインスタンスを破棄するか </summary>
        public bool DeleteOnClose
        {
            get { return deleteOnClose; }
            set { deleteOnClose = value; }
        }

        /// <summary> 表示優先度 </summary>
        public int DisplayPriority
        {
            get { return displayPriority; }
            set { displayPriority = value; }
        }

        //----- method -----

        public IObservable<Unit> Open(bool blockInput = true)
        {
            var inputBlock = blockInput ? new BlockInput() : null;

            return Prepare()
                .ToObservable()
                .Do(_ => UnityUtility.SetActive(gameObject, true))
                .SelectMany(_ => OnOpen().ToObservable())
                .Do(_ =>
                    {
                        if (inputBlock != null)
                        {
                            inputBlock.Dispose();
                            inputBlock = null;
                        }

                        if (onOpen != null)
                        {
                            onOpen.OnNext(Unit.Default);
                        }
                    })
                .AsUnitObservable();
        }

        public IObservable<Unit> Close(bool blockInput = true)
        {
            var inputBlock = blockInput ? new BlockInput() : null;

            return OnClose()
                .ToObservable()
                .Do(_ =>
                    {
                        UnityUtility.SetActive(gameObject, false);

                        if (inputBlock != null)
                        {
                            inputBlock.Dispose();
                            inputBlock = null;
                        }

                        if (onClose != null)
                        {
                            onClose.OnNext(Unit.Default);
                        }
                        
                        if (deleteOnClose)
                        {
                            UnityUtility.SafeDelete(gameObject);
                        }
                    })
                .AsUnitObservable();
        }

        public IObservable<Unit> Wait()
        {
            return Observable.FromMicroCoroutine(() => WaitInternal());
        }

        private IEnumerator WaitInternal()
        {
            while (true)
            {
                if (UnityUtility.IsNull(this)) { break; }

                if (!UnityUtility.IsActive(gameObject)) { break; }

                yield return null;
            }
        }

        public IObservable<Unit> OnOpenAsObservable()
        {
            return onOpen ?? (onOpen = new Subject<Unit>());
        }

        public IObservable<Unit> OnCloseAsObservable()
        {
            return onClose ?? (onClose = new Subject<Unit>());
        }

        protected virtual UniTask Prepare() { return UniTask.CompletedTask; }

        protected virtual UniTask OnOpen() { return UniTask.CompletedTask; }

        protected virtual UniTask OnClose() { return UniTask.CompletedTask; }
    }
}

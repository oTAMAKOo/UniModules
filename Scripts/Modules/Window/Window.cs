﻿﻿﻿﻿﻿
using UnityEngine;
using System;
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

        public async UniTask Open(bool blockInput = true)
        {
            var inputBlock = blockInput ? new BlockInput() : null;

            await Prepare();

            UnityUtility.SetActive(gameObject, true);

			await OnOpen();

			if (inputBlock != null)
			{
				inputBlock.Dispose();
				inputBlock = null;
			}

			if (onOpen != null)
			{
				onOpen.OnNext(Unit.Default);
			}
        }

        public async UniTask Close(bool blockInput = true)
        {
            var inputBlock = blockInput ? new BlockInput() : null;

            await OnClose();

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
        }

        public async UniTask Wait()
        {
            while (true)
            {
                if (UnityUtility.IsNull(this)) { break; }

                if (!UnityUtility.IsActive(gameObject)) { break; }

                await UniTask.NextFrame();
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

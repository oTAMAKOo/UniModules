
using UnityEngine;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using Extensions;
using Modules.InputControl;

namespace Modules.Window
{
    public abstract class Window : MonoBehaviour
    {
        //----- params -----

        public enum WindowStatus
        {
            None = 0,

            /// <summary> 準備中 </summary>
            Prepare,
            /// <summary> 表示中 </summary>
            Opened,
            /// <summary> 閉じる </summary>
            Close,
            /// <summary> 閉じた </summary>
            Closed,
        }

        //----- field -----

        [SerializeField]
        private bool deleteOnClose = true;
        [SerializeField]
        private int displayPriority = 0;

        private Subject<Unit> onOpen = null;
        private Subject<Unit> onClose = null;

        //----- property -----

        public WindowStatus Status { get; private set; }

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
            var cancelToken = this.GetCancellationTokenOnDestroy();

            var ignoreOpenStatus = new WindowStatus[]
            {
                WindowStatus.Prepare, 
                WindowStatus.Opened,
                WindowStatus.Close,
            };

            if (ignoreOpenStatus.Contains(Status)) { return; }

            var prevStatus = Status;

            Status = WindowStatus.Prepare;

            var inputBlock = blockInput ? new BlockInput() : null;

            try
            {
                await Prepare();

                if (cancelToken.IsCancellationRequested){ return; }

                UnityUtility.SetActive(gameObject, true);

                await OnOpen();

                if (cancelToken.IsCancellationRequested){ return; }

                Status = WindowStatus.Opened;
            }
            catch (Exception)
            {
                // Prepare / OnOpen が失敗した場合は Status を戻して再度開けるようにする.
                // (Prepare のまま残すと ignoreOpenStatus に引っかかり二度と開けなくなる)
                Status = prevStatus;

                UnityUtility.SetActive(gameObject, false);

                throw;
            }
            finally
            {
                if (inputBlock != null)
                {
                    inputBlock.Dispose();
                    inputBlock = null;
                }
            }

            if (onOpen != null)
            {
                onOpen.OnNext(Unit.Default);
            }
        }

        public async UniTask Close(bool blockInput = true)
        {
            var cancelToken = this.GetCancellationTokenOnDestroy();

            if (Status != WindowStatus.Opened) { return; }

            Status = WindowStatus.Close;

            var inputBlock = blockInput ? new BlockInput() : null;

            try
            {
                await OnClose();

                if (cancelToken.IsCancellationRequested){ return; }

                UnityUtility.SetActive(gameObject, false);
            }
            catch (Exception)
            {
                // OnClose が失敗した場合も閉じた状態へ確定させる.
                // (Close のまま残すと Open も Close も受け付けなくなり、以後そのウィンドウを操作できなくなる)
                UnityUtility.SetActive(gameObject, false);

                Status = WindowStatus.Closed;

                throw;
            }
            finally
            {
                if (inputBlock != null)
                {
                    inputBlock.Dispose();
                    inputBlock = null;
                }
            }

            if (onClose != null)
            {
                onClose.OnNext(Unit.Default);
            }

            Status = WindowStatus.Closed;

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

            await UniTask.NextFrame();
        }

        public Observable<Unit> OnOpenAsObservable()
        {
            return onOpen ?? (onOpen = new Subject<Unit>());
        }

        public Observable<Unit> OnCloseAsObservable()
        {
            return onClose ?? (onClose = new Subject<Unit>());
        }

        protected virtual UniTask Prepare() { return UniTask.CompletedTask; }

        protected virtual UniTask OnOpen() { return UniTask.CompletedTask; }

        protected virtual UniTask OnClose() { return UniTask.CompletedTask; }
    }
}

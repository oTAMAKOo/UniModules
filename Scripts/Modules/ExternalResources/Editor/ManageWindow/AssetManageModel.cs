
using System;
using UniRx;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public sealed class AssetManageModel
    {
        //----- params -----

        //----- field -----
        
        private Subject<Unit> onRequestRepaint = null;
        private Subject<Object[]> onDragAndDrop = null;

        //----- property -----

        //----- method -----

        public void Initialize(){ }

        public void DragAndDrop(Object[] dropObjects)
        {
            if (onDragAndDrop != null)
            {
                onDragAndDrop.OnNext(dropObjects);
            }
        }

        public IObservable<Object[]> OnDragAndDropAsObservable()
        {
            return onDragAndDrop ?? (onDragAndDrop = new Subject<Object[]>());
        }

        public void RequestRepaint()
        {
            if (onRequestRepaint != null)
            {
                onRequestRepaint.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnRequestRepaintAsObservable()
        {
            return onRequestRepaint ?? (onRequestRepaint = new Subject<Unit>());
        }
    }
}

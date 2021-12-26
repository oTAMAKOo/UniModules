
#if ENABLE_EMBEDDEDBROWSER

using UniRx;
using ZenFulcrum.EmbeddedBrowser;

namespace Modules.WebView
{
    public sealed class CustomEmbeddedBrowser : Browser
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----
        
        public void SetForceUpdateCallback()
        {
            Observable.EveryUpdate()
                .Subscribe(_ =>
                   {
                       if (!gameObject.activeInHierarchy)
                       {
                           Update();
                       }
                   })
                .AddTo(this);

            Observable.EveryLateUpdate()
                .Subscribe(_ =>
                   {
                       if (!gameObject.activeInHierarchy)
                       {
                           LateUpdate();
                       }
                   })
                .AddTo(this);
        }
    }
}

#endif
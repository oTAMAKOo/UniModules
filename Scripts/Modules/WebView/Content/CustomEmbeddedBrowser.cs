
#if ENABLE_EMBEDDEDBROWSER

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

using R3;
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

            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
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

#endif
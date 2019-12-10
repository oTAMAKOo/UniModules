
﻿﻿#if ENABLE_UNIWEBVIEW2

using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.WebView
{
	public sealed class WebViewManager : Singleton<WebViewManager>
	{
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        private WebViewManager() { }

        public void Initialize()
        {
            #if DEBUG && UNITY_ANDROID

            UniWebView.SetWebContentsDebuggingEnabled(Debug.isDebugBuild);

            #endif
        }
    }
}

#endif

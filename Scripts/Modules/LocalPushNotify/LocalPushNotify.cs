﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ApplicationEvent;

namespace Modules.LocalPushNotify
{
    public sealed partial class LocalPushNotify : Singleton<LocalPushNotify>
    {
        //----- params -----

        public class NotificationInfo
        {
            public int Identifier { get; private set; }

            // 必須項目.
            public long UnixTime { get; private set; }
            public string Title { get; private set; }
            public string Message { get; private set; }

            // オプション.
            public bool Sound { get; set; }
            public bool Vibrate { get; set; }
            public bool Lights { get; set; }
            public string LargeIconResource { get; set; }
            public string SmallIconResource { get; set; }
            public Color32 BgColor { get; set; }

            public NotificationInfo(long unixTime, string title, string message)
            {
                Identifier = Instance.initializedTime + Instance.incrementCount;

                // 通知IDを重複させない為カウンタを加算.
                Instance.incrementCount++;

                UnixTime = unixTime;
                Title = title;
                Message = message;

                Sound = true;
                Vibrate = true;
                Lights = true;
                LargeIconResource = null;
                SmallIconResource = "notify_icon_small";
                BgColor = new Color(0xff, 0x44, 0x44, 255);
            }
        }

        //----- field -----

        private int initializedTime = 0;
        private int incrementCount = 0;

        private Dictionary<long, NotificationInfo> notifications = null;

        private Subject<Unit> onNotifyRegister = null;

        private LifetimeDisposable disposable = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            disposable = new LifetimeDisposable();

            initializedTime = (int)DateTime.Now.ToUnixTime();
            incrementCount = 0;

            notifications = new Dictionary<long, NotificationInfo>();

            ApplicationEventHandler.OnQuitAsObservable().Subscribe(_ => OnSuspend()).AddTo(disposable.Disposable);

            ApplicationEventHandler.OnSuspendAsObservable().Subscribe(_ => OnSuspend()).AddTo(disposable.Disposable);

            ApplicationEventHandler.OnResumeAsObservable().Subscribe(_ => OnResume()).AddTo(disposable.Disposable);

            #if UNITY_ANDROID && !UNITY_EDITOR

            PlatformInitialize();

            #elif UNITY_IOS && !UNITY_EDITOR

            PlatformInitialize();

            #endif

            // 過去に登録した通知を削除.
            Clear();
        }

        public static long Set(NotificationInfo info)
        {
            Instance.notifications.Add(info.Identifier, info);

            return info.Identifier;
        }

        public static void Remove(long id)
        {
            if(Instance.notifications.ContainsKey(id))
            {
                Instance.notifications.Remove(id);
            }
        }

        private void Register()
        {
            // 二重登録されないように一旦クリア.
            Clear();

            // 通知登録イベント.
            if(onNotifyRegister != null)
            {
                onNotifyRegister.OnNext(Unit.Default);
            }

            #if UNITY_ANDROID && !UNITY_EDITOR

            SetNotify();
                    
            #elif UNITY_IOS && !UNITY_EDITOR

            SetNotify();

            #endif

            notifications.Clear();
        }

        private void Clear()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            ClearNotify();
                    
            #elif UNITY_IOS && !UNITY_EDITOR

            ClearNotify();

            #endif
        }

        private void OnSuspend()
        {
            Register();
        }

        private void OnResume()
        {
            Clear();
        }

        public static IObservable<Unit> OnNotifyRegisterAsObservable()
        {
            return Instance.onNotifyRegister ?? (Instance.onNotifyRegister = new Subject<Unit>());
        }
    }
}

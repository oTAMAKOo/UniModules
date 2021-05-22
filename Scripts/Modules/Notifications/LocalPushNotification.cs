﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ApplicationEvent;

namespace Modules.Notifications
{
    public abstract partial class LocalPushNotification<Tinstance> : Singleton<Tinstance> where Tinstance : LocalPushNotification<Tinstance>
    {
        //----- params -----

        public sealed class NotificationInfo
        {
            public int Identifier { get; private set; }

            // 必須項目.
            public long UnixTime { get; private set; }
            public string Title { get; private set; }
            public string Message { get; private set; }

            // オプション.
            public int BadgeCount { get; set; }
            public string LargeIconResource { get; set; }
            public string SmallIconResource { get; set; }
            public Color32? Color { get; set; }

            public NotificationInfo(long unixTime, string title, string message)
            {
                Identifier = Instance.initializedTime + Instance.incrementCount;

                // 通知IDを重複させない為カウンタを加算.
                Instance.incrementCount++;

                UnixTime = unixTime;
                Title = title;
                Message = message;

                LargeIconResource = null;
                SmallIconResource = "notify_icon_small";
                Color = null;
                BadgeCount = 1;
            }
        }

        //----- field -----

        private int initializedTime = 0;
        private int incrementCount = 0;

        private Dictionary<long, NotificationInfo> notifications = null;

        private Subject<Unit> onNotificationRegister = null;

        //----- property -----

        public abstract long CurrentTime { get; }

        //----- method -----

        private LocalPushNotification() { }

        public void Initialize()
        {
            initializedTime = (int)DateTime.Now.ToUnixTime();
            incrementCount = 0;

            notifications = new Dictionary<long, NotificationInfo>();

            // イベント登録.

            ApplicationEventHandler.OnQuitAsObservable()
                .Subscribe(_ => OnSuspend())
                .AddTo(Disposable);

            ApplicationEventHandler.OnSuspendAsObservable()
                .Subscribe(_ => OnSuspend())
                .AddTo(Disposable);

            ApplicationEventHandler.OnResumeAsObservable()
                .Subscribe(_ => OnResume())
                .AddTo(Disposable);

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
            if(onNotificationRegister != null)
            {
                onNotificationRegister.OnNext(Unit.Default);
            }

            #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

            SetNotify();

            #endif

            notifications.Clear();
        }
        /// <summary> 通知をすべてクリア </summary>
        private void Clear()
        {
            #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

            ClearNotifications();

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
            return Instance.onNotificationRegister ?? (Instance.onNotificationRegister = new Subject<Unit>());
        }
    }
}

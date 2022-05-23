﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ApplicationEvent;

namespace Modules.Notifications
{
    public abstract partial class LocalPushNotification<Tinstance> : Singleton<Tinstance> where Tinstance : LocalPushNotification<Tinstance>
    {
        //----- params -----

        public sealed class Info
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

            public Info(long unixTime, string title, string message)
            {
				Identifier = Prefs.identifier++;

				UnixTime = unixTime;
                Title = title;
                Message = message;

                LargeIconResource = "notify_icon_large";
                SmallIconResource = "notify_icon_small";
                Color = null;
                BadgeCount = 1;
            }
        }

		private sealed class Prefs
		{
			public static int identifier
			{
				get { return SecurePrefs.GetInt(typeof(Prefs).FullName + "-identifier", 1); }
				set { SecurePrefs.SetInt(typeof(Prefs).FullName + "-identifier", value); }
			}
		}

        //----- field -----

        private bool enable = false;

		private Dictionary<long, Info> notifications = null;

        private Subject<Unit> onNotificationRegister = null;

        private bool initialized = false;

        //----- property -----

        public bool Enable
        {
            get { return enable; }

            set
            {
				var prev = enable;

				enable = value;

				// 過去に登録した通知を削除.
				if (prev != value)
				{
					Clear();
				}
            }
        }

        public abstract long CurrentTime { get; }

        //----- method -----

        protected LocalPushNotification() { }

        public void Initialize()
        {
            if (initialized){ return; }

            enable = false;

            notifications = new Dictionary<long, Info>();

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

            initialized = true;
        }

        /// <summary>
        /// 通知を登録.
        /// </summary>
        /// <param name="info"> 通知のパラメータ</param>
        /// <returns> 登録成功時は正のID、失敗時は-1</returns>
        public long Set(Info info)
        {
            if(!enable) { return -1; }

            notifications.Add(info.Identifier, info);

            return info.Identifier;
        }

        public void Remove(long id)
        {
            if (!enable){ return; }

            if (id == -1){ return; }

            if(notifications.ContainsKey(id))
            {
                notifications.Remove(id);
            }
        }

        private void Schedule()
        {
            if (!initialized || !enable) { return; }

            // 二重登録されないように一旦クリア.
            Clear();

            // 通知登録イベント.
            if(onNotificationRegister != null)
            {
                onNotificationRegister.OnNext(Unit.Default);
            }

            #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

            AddSchedule();

            #endif

            notifications.Clear();
        }
        /// <summary> 通知をすべてクリア </summary>
        private void Clear()
        {
            if (!initialized || !enable) { return; }

            #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

            ClearNotifications();

            #endif
        }

        private void OnSuspend()
        {
			SecurePrefs.Save();
			Schedule();
        }

        private void OnResume()
        {
            Clear();
        }

        public IObservable<Unit> OnNotifyRegisterAsObservable()
        {
            return onNotificationRegister ?? (onNotificationRegister = new Subject<Unit>());
        }
    }
}

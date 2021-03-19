
#if UNITY_ANDROID && !UNITY_EDITOR
﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ApplicationEvent;

namespace Modules.LocalPushNotify
{
    public sealed partial class LocalPushNotify
    {
        //----- params -----

        public partial class Prefs
        {
            public static int[] notificationKeys
            {
                get { return SecurePrefs.Get<int[]>("NotificationKeys"); }
                set { SecurePrefs.Set<int[]>("NotificationKeys", value); }
            }
        }

        private const string PluginClassName = "modules.notification.LocalNotification";

        //----- field -----

        private AndroidJavaObject plugin = null;

        //----- property -----

        //----- method -----

        public void PlatformInitialize()
        {
            // プラグイン名をパッケージ名+クラス名で指定.
            plugin = new AndroidJavaObject(PluginClassName);

            if (plugin == null)
            {
                Debug.LogErrorFormat("PluginClass not found.\n{0}", PluginClassName);
            }
        }

        private void SetNotify()
        {
            if (plugin == null){ return; }

            foreach (var info in notifications.Values)
            {
                var args = new object[]
                {
                    info.Identifier,
                    info.UnixTime,
                    info.Title,
                    info.Message,
                    info.Message, 
                    info.Sound ? 1 : 0,
                    info.Vibrate ? 1 : 0, 
                    info.Lights ? 1 : 0,
                    info.LargeIconResource,
                    info.SmallIconResource,
                    info.BgColor.r * 65536 + info.BgColor.g * 256 + info.BgColor.b,
                    Application.identifier,
                };

                plugin.Call("setNotification", args);
            }

            Prefs.notificationKeys = notifications.Values.Select(x => x.Identifier).ToArray();
        }

        private void ClearNotify()
        {
            if (plugin == null){ return; }

            var primaryKeys = Prefs.notificationKeys;

            if(primaryKeys == null){  return; }

            foreach (var primaryKey in primaryKeys)
            {
                plugin.Call("cancelNotification", primaryKey);
            }

            Prefs.notificationKeys = new int[0];
            PlayerPrefs.Save();
        }
    }
}

#endif

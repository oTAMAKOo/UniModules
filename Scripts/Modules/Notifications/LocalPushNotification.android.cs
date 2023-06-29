
#if UNITY_ANDROID && !UNITY_EDITOR
﻿﻿﻿
using UnityEngine;
using Unity.Notifications.Android;
using System.Linq;
using Extensions;

namespace Modules.Notifications
{
    public abstract partial class LocalPushNotification<Tinstance>
    {
        //----- params -----

        private sealed partial class Prefs
        {
            public static int[] notificationKeys
            {
                get { return SecurePrefs.Get<int[]>("NotificationKeys"); }
                set { SecurePrefs.Set<int[]>("NotificationKeys", value); }
            }
        }

        //----- field -----

        private AndroidNotificationChannel notificationChannel = default;

        //----- property -----

        //----- method -----

        /// <summary> Androidで使用するプッシュ通知用のチャンネルを登録 </summary>
        public void RegisterChannel(string channelId, string title, Importance importance, string description)
        {
            // プッシュ通知用のチャンネルを登録.

            notificationChannel = new AndroidNotificationChannel()
            {
                Id = channelId,
                Name = title,
                Importance = importance,
                Description = description,
            };

            AndroidNotificationCenter.RegisterNotificationChannel(notificationChannel);
        }

        private void AddSchedule()
        {
            var channelId = notificationChannel.Id;

            foreach (var info in notifications.Values)
            {
                var notification = new AndroidNotification
                {
                    Title = info.Title,
                    Text = info.Message,
                    SmallIcon = info.SmallIconResource,
                    LargeIcon = info.LargeIconResource,
                    Color = info.Color,
                    FireTime = info.UnixTime.UnixTimeToDateTime(),
                    Number = info.BadgeCount,
                };

                // プッシュ通知はすでに登録済み.
                if (AndroidNotificationCenter.CheckScheduledNotificationStatus(info.Identifier) == NotificationStatus.Scheduled)
                {
                    AndroidNotificationCenter.SendNotification(notification, channelId);

                    Debug.LogWarning("Replace the currently scheduled notification with a new notification");
                }
                // プッシュ通知はすでに通知済み.
                else if (AndroidNotificationCenter.CheckScheduledNotificationStatus(info.Identifier) == NotificationStatus.Delivered)
                {
                    AndroidNotificationCenter.CancelNotification(info.Identifier);
                }
                // プッシュ通知は不明な状況.
                else if (AndroidNotificationCenter.CheckScheduledNotificationStatus(info.Identifier) == NotificationStatus.Unknown)
                {
                    AndroidNotificationCenter.SendNotification(notification, channelId);
                }
            }

            Prefs.notificationKeys = notifications.Values.Select(x => x.Identifier).ToArray();
        }

        private void ClearNotifications()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllNotifications();
        }
    }
}

#endif

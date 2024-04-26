
#if UNITY_IOS && !UNITY_EDITOR

using UnityEngine;
using Unity.Notifications.iOS;
using Extensions;

namespace Modules.Notifications
{
    public abstract partial class LocalPushNotification<Tinstance>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        private void AddSchedule()
        {
			foreach (var info in notifications.Values)
            {
                var time = info.UnixTime.UnixTimeToDateTime() - CurrentTime.UnixTimeToDateTime();

                if (time.TotalSeconds <= 0)
                {
                    Debug.LogError($"Notification schedule failed.\nid = {info.Identifier}\ntitle = {info.Title}\nmessage = {info.Message}");
                    continue;
                }

                var notification = new iOSNotification()
                {
                    Identifier = info.Identifier.ToString(),
                    Title = info.Title,
                    Body = info.Message,
                    ShowInForeground = false,
                    Badge = info.BadgeCount,

                    Trigger = new iOSNotificationTimeIntervalTrigger
                    {
                        TimeInterval = time,
                        Repeats = false,
                    }
                };

                iOSNotificationCenter.ScheduleNotification(notification);
			}
        }

        private void ClearNotifications()
        {
            // iOSの通知をすべて削除.
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();

            // バッジを消す.
            iOSNotificationCenter.ApplicationBadge = 0;
        }
    }
}

#endif

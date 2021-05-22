
#if UNITY_IOS && !UNITY_EDITOR
﻿﻿﻿﻿
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

        private void SetNotify()
        {
			foreach (var info in notifications.Values)
            {
                var time = info.UnixTime.UnixTimeToDateTime() - CurrentTime.UnixTimeToDateTime();

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

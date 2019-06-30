
#if UNITY_IOS && !UNITY_EDITOR
﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

using UnityEngine.iOS;

using NotificationServices = UnityEngine.iOS.NotificationServices;
using LocalNotification = UnityEngine.iOS.LocalNotification;

namespace Modules.LocalPushNotify
{
    public sealed partial class LocalPushNotify
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public void PlatformInitialize()
        {
            var notificationTypes = NotificationType.Alert | NotificationType.Badge | NotificationType.Sound;

            NotificationServices.RegisterForNotifications(notificationTypes);
        }

        private void SetNotify()
        {
			foreach (var info in notifications.Values)
            {
				var localNotification = new LocalNotification();

				localNotification.fireDate = info.UnixTime.UnixTimeToDateTime();
				localNotification.alertBody = info.Message;
				localNotification.alertAction = info.Title;

				NotificationServices.ScheduleLocalNotification(localNotification);
			}
        }

        private void ClearNotify()
        {
            if (0 < NotificationServices.localNotificationCount)
            {
                var localNotification = new LocalNotification();

                localNotification.applicationIconBadgeNumber = -1;

                NotificationServices.PresentLocalNotificationNow(localNotification);
            }

            if (NotificationServices.scheduledLocalNotifications.Any())
            {
                NotificationServices.CancelAllLocalNotifications();

                NotificationServices.ClearLocalNotifications();
            }
        }
    }
}

#endif

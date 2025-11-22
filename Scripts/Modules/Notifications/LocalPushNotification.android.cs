
#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using Unity.Notifications.Android;
using System.Linq;
using Cysharp.Threading.Tasks;
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

        public async UniTask RequestNotificationPermission()
        {
            var request = new PermissionRequest();
            
            while (request.Status == PermissionStatus.RequestPending)
            {
                await UniTask.NextFrame();
            }

            OnRequestPermissionResult(request.Status);
        }

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
                var time = info.UnixTime.UnixTimeToDateTime() - CurrentTime.UnixTimeToDateTime();

                if (time.TotalSeconds <= 0)
                {
                    Debug.LogError($"Notification schedule failed.\nid = {info.Identifier}\ntitle = {info.Title}\nmessage = {info.Message}");
                    continue;
                }

                var fireTime = DateTime.Now.Add(time);

                var notification = new AndroidNotification
                {
                    Title = info.Title,
                    Text = info.Message,
                    SmallIcon = info.SmallIconResource,
                    LargeIcon = info.LargeIconResource,
                    Color = info.Color,
                    FireTime = fireTime,
                    Number = info.BadgeCount,
                };

                var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(info.Identifier);

                UnityEngine.Debug.Log(status);

                // プッシュ通知はすでに登録済み.
                if (status == NotificationStatus.Scheduled)
                {
                    AndroidNotificationCenter.SendNotification(notification, channelId);

                    Debug.LogWarning("Replace the currently scheduled notification with a new notification");
                }
                // プッシュ通知はすでに通知済み.
                else if (status == NotificationStatus.Delivered)
                {
                    AndroidNotificationCenter.CancelNotification(info.Identifier);
                }
                // プッシュ通知は不明な状況.
                else if (status == NotificationStatus.Unknown)
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

        protected virtual void OnRequestPermissionResult(PermissionStatus status){ }
    }
}

#endif

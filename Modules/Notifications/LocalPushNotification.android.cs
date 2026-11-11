
#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using Unity.Notifications.Android;
using System;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Notifications
{
    public abstract partial class LocalPushNotification<Tinstance>
    {
        //----- params -----

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

                // 識別子を指定して登録.
                AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, info.Identifier);
            }
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

using Unity.Notifications.Android;
using UnityEngine;

namespace LocalCalendar.Notifications
{
    public static class NotificationInitializer
    {
        private const string ChannelId = "calendar_reminders";

        public static string Channel => ChannelId;

        public static void Initialize()
        {
            // Open a notification channel with Android
            var channel = new AndroidNotificationChannel
            {
                Id = ChannelId,
                Name = "Calendar Reminders",
                Importance = Importance.High,
                Description = "Event and reminder notifications"
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            // Make sure we have notification permissions
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            "android.permission.POST_NOTIFICATIONS"))
            {
                UnityEngine.Android.Permission.RequestUserPermission(
                    "android.permission.POST_NOTIFICATIONS");
            }
        }
    }
}

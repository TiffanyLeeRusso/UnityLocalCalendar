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
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!HasPromptedForExactAlarm())
            {
                PlayerPrefs.SetInt("ExactAlarmPrompted", 1);
                // Request exact-alarm permission
                ExactAlarmRequest.OpenExactAlarmSettings();
                //UIEvents.ShowExactAlarmExplanation();
            }
#endif

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

        private static bool HasPromptedForExactAlarm()
        {
            return PlayerPrefs.GetInt("ExactAlarmPrompted", 0) == 1;
        }
    }
}

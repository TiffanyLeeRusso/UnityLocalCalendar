using UnityEngine;
using Unity.Notifications.Android;
using UnityEngine.SceneManagement;
using System;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Services;

namespace LocalCalendar.App
{
    public class AppBootstrap : MonoBehaviour
    {
        private static bool _initialized = false;

        void Awake()
        {
            if (_initialized)
            {
                Destroy(gameObject);
                return;
            }
            _initialized = true;
            DontDestroyOnLoad(gameObject);

            Database.Initialize();
            NotificationInitializer.Initialize();

            LoggingService.Info(LogCategory.System, "App opened");
        }

        void Start()
        {
            // Schedule notifications once per app launch
            RescheduleNotifications();

            // Register listener
            AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceived;

            // If the app opened from a notification tap
            HandleNotificationIntent();
        }

        private void RescheduleNotifications()
        {
            var repo = new CalendarRepository();
            var all = repo.GetAllCalendarItems();

            foreach (var item in all)
            {
                switch (NotificationScheduler.GetReminderTiming(item))
                {
                    case ReminderTiming.Future:
                        NotificationScheduler.Schedule(item);
                        break;

                    case ReminderTiming.Late:
                        NotificationScheduler.FireImmediate(item);
                        break;

                    default:
                        // Ignore
                        break;
                }
            }
        }

        private void HandleNotificationIntent()
        {            
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            // If Android gave us an intent
            if (intent != null && !string.IsNullOrEmpty(intent.Notification.IntentData))
            {
                // If this intent is to open an item
                if (intent.Notification.IntentData.StartsWith("open:item:"))
                {
                    // Open the item
                    var itemId = intent.Notification.IntentData.Replace("open:item:", "");
                    EditItemContext.EditingItemId = itemId;
                    SceneManager.LoadScene("EditItemScene");
                }
            }
        }

        private void OnNotificationReceived(AndroidNotificationIntentData data)
        {
            NotificationLogger.Log(new NotificationLogEntry
            {
                Title = data.Notification.Title,
                FiredLocal = DateTime.Now,
                Note = "Delivered"
            });
        }

        private void OnApplicationPause(bool paused)
        {
            LoggingService.Info(LogCategory.App,
                                paused ? "App paused" : "App resumed");
        }

        private void OnDestroy()
        {
            AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
        }
    }
}

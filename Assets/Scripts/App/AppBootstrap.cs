using UnityEngine;
using Unity.Notifications.Android;
using UnityEngine.SceneManagement;
using System;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Services;
using LocalCalendar.EditItem;
using LocalCalendar.AppDebug;

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

            // Initialization
            Database.Initialize();
            NotificationInitializer.Initialize();
            GlobalExceptionHandler.Init();

            LoggingService.Info(LogCategory.System, "App started");
        }

        void Start()
        {
            // Schedule notifications once per app launch
            RescheduleNotifications();

            // Register listeners
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
#if UNITY_ANDROID && !UNITY_EDITOR
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            // If Android gave us an intent
            if (intent != null && !string.IsNullOrEmpty(intent.Notification.IntentData))
            {
                // If this intent is to open an item
                if (intent.Notification.IntentData.StartsWith("open:item:"))
                {
                    // Open the item
                    var itemId = intent.Notification.IntentData.Replace("open:item:", "");
                    LoggingService.Info(LogCategory.App, "Handling Android notification intent for reminder item with ID " + itemId);
                    EditItemContext.EditingItemId = itemId;
                    SceneManager.LoadScene("EditItemScene");
                }
            }
#endif
        }

        // --- Event Handling ---

        private void OnNotificationReceived(AndroidNotificationIntentData data)
        {
            NotificationLogger.Log(new NotificationLogEntry
            {
                Title = data.Notification.Title,
                FiredLocal = DateTime.Now,
                Note = "Delivered"
            });
        }

        // App brought to foreground
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                LoggingService.Info(LogCategory.App, "App focused");

                // Handle intent when we resume focus (not from fresh app startup)
                HandleNotificationIntent();
            }
            else
            {
                LoggingService.Info(LogCategory.App, "App not focused");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            LoggingService.Info(LogCategory.App,
                                paused ? "App paused" : "App resumed");
        }

        private void OnApplicationQuit()
        {
            LoggingService.Info(LogCategory.App,"App exiting");
        }

        private void OnDestroy()
        {
            AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
        }
    }
}

using UnityEngine;
using Unity.Notifications.Android;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Permissions;
using LocalCalendar.Services;
using LocalCalendar.EditItem;
using LocalCalendar.AppDebug;

namespace LocalCalendar.App
{
    public class AppBootstrap : MonoBehaviour
    {
        private static bool _initialized = false;
#if UNITY_ANDROID && !UNITY_EDITOR
        private bool _handlingIntent = false;
#endif

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

        void OnEnable()
        {
            HandleNotificationIntent();
        }

        void Start()
        {
            // Register listeners
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceived;
#endif
            // Schedule notifications once per app launch
            RescheduleNotifications();

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

        private IEnumerator HandleIntentNextFrame(string itemId)
        {
            yield return null;

            EditItemContext.EditingItemId = itemId;
            SceneManager.LoadScene("EditItemScene");

#if UNITY_ANDROID && !UNITY_EDITOR
            _handlingIntent = false;
#endif
        }

        private void HandleNotificationIntent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();

            if (intent == null)
                return;

            var data = intent.Notification.IntentData;

            if (string.IsNullOrEmpty(data))
                return;

            if (!data.StartsWith("open:item:"))
                return;

            if (_handlingIntent)
                return;

            _handlingIntent = true;

            var itemId = data.Replace("open:item:", "");
            LoggingService.Info(LogCategory.App,
                                "Handling Android notification intent for item " + itemId);

            StartCoroutine(HandleIntentNextFrame(itemId));
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
                if (PermissionsUtils.CanScheduleExactAlarms())
                    LoggingService.Info(LogCategory.System, "Exact alarms granted");

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

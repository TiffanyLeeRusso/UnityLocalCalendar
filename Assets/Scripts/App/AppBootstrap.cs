using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Notifications.Android;
using System;
using System.Collections;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using LocalCalendar.Services;
using LocalCalendar.AppDebug;
using LocalCalendar.EditItem;

namespace LocalCalendar.App
{
    public class AppBootstrap : MonoBehaviour
    {
        private static bool _initialized = false;
        private static DateTime _lastKnownDate;
#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _handlingIntent = false;
#endif

        void Awake()
        {
            if (_initialized)
            {
                Destroy(gameObject);
                return;
            }
            _initialized = true;
            _lastKnownDate = DateTime.Today;
            DontDestroyOnLoad(gameObject);

            // Initialization
            Database.Initialize();
            NotificationInitializer.Initialize();
            GlobalExceptionHandler.Init();

            // Note playerprefs and DB may be saved from Google Auto Backup
            // or Cloud restore even on reinstall unless we want to disable
            // backups explicitly (in the manifest:
            //<application android:allowBackup="false" android:fullBackupContent="false">)
            if (!PlayerPrefs.HasKey("installed"))
            {
                NotificationScheduler.CancelAll();
                PlayerPrefs.SetInt("installed", 1);
                PlayerPrefs.Save();
            }
            
            LoggingService.Info(LogCategory.System, "App started");
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

        // Handle the DB-import message from our Java PickerHelper.
        // Note the PickerHelper defines exactly who receives this message;
        // currently it is AppBootstrap::OnReceiveUri so make sure they stay in sync.
        public void OnReceiveUri(string uriString)
        {
            LoggingService.Info(LogCategory.App, "Received URI from Android: " + uriString);
            AppUtils.ImportFromUri(uriString);
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

        // --- Intent handling ---
        
        private void HandleNotificationIntent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();

            if (intent == null)
                return;

            var data = intent.Notification.IntentData;

            if (string.IsNullOrEmpty(data)) return;
            if (!data.StartsWith("open:item:")) return;
            if (_handlingIntent) return;

            _handlingIntent = true;

            var itemId = data.Replace("open:item:", "");
            LoggingService.Info(LogCategory.App,
                                "Handling Android notification intent for item " + itemId);

            StartCoroutine(HandleIntentNextFrame(itemId));
#endif
        }

        private static IEnumerator HandleIntentNextFrame(string itemId)
        {
            yield return null;

            EditItemContext.EditingItemId = itemId;
            SceneManager.LoadScene("EditItemScene");

#if UNITY_ANDROID && !UNITY_EDITOR
            _handlingIntent = false;
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
                // Handle intent when we resume focus
                HandleNotificationIntent();

                if (DateTime.Today != _lastKnownDate)
                {
                    _lastKnownDate = DateTime.Today;
                    CalendarRefreshSignal.NeedsRefresh = true;
                }
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused)
                OnApplicationFocus(true);
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

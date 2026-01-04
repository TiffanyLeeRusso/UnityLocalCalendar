using UnityEngine;
using LocalCalendar.Data;
using LocalCalendar.Notifications;
using Unity.Notifications.Android;
using System;

namespace LocalCalendar.App
{
    public class AppBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Database.Initialize();
            NotificationInitializer.Initialize();
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            var repo = new CalendarRepository();
            var all = repo.GetAll();

            foreach (var item in all)
            {
                NotificationScheduler.Schedule(item);
            }

            NotificationCatchUpService.Run();

            AndroidNotificationCenter.OnNotificationReceived += data =>
            {
                NotificationLogger.Log(new NotificationLogEntry
                {
                    Title = data.Notification.Title,
                    FiredLocal = DateTime.Now,
                    Note = "Delivered"
                });
            };
        }
    }
}

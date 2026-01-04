using System;
using LocalCalendar.Data;
using LocalCalendar.Models;
using Unity.Notifications.Android;

namespace LocalCalendar.Notifications
{
    public static class NotificationCatchUpService
    {
        public static void Run()
        {
            var repo = new CalendarRepository();
            var items = repo.GetAll();

            foreach (var item in items)
            {
                if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                    continue;

                DateTime intendedUtc = item.StartUtc + item.Reminder.Offset;
                DateTime nowUtc = DateTime.UtcNow;

                // Missed but still relevant (within last 10 min)
                if (intendedUtc < nowUtc &&
                    intendedUtc > nowUtc.AddMinutes(-10))
                {
                    FireImmediate(item);
                }
                else if (intendedUtc > nowUtc)
                {
                    NotificationScheduler.Schedule(item);
                }
            }
        }

        private static void FireImmediate(CalendarItem item)
        {
            var notification = new AndroidNotification
            {
                Title = item.Title,
                Text = "[Late] " + (item.Note ?? "Reminder"),
                FireTime = DateTime.Now.AddSeconds(1)
            };

            AndroidNotificationCenter.SendNotification(
                notification,
                NotificationInitializer.Channel
            );
        }
    }
}

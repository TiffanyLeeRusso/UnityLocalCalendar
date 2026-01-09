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
            var items = repo.GetAllCalendarItems();

            foreach (var item in items)
            {
                if(NotificationScheduler.isCurrentReminder(item, true))
                    FireImmediate(item);
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

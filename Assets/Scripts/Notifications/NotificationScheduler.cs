using Unity.Notifications.Android;
using UnityEngine;
using LocalCalendar.Models;
using LocalCalendar.Data;
using System;

namespace LocalCalendar.Notifications
{
    public static class NotificationScheduler
    {
        public static void Schedule(CalendarItem item)
        {
            if (item.Type != CalendarItemType.Reminder || item.Reminder == null)
                return;

            // Exact time commented out. Since Android does not guarantee exact delivery time
            // for local notifcations we schedule notifications slightly beforehand.
            //DateTime fireTimeUtc = item.StartUtc + item.Reminder.Offset;
            DateTime fireTimeUtc =
                item.StartUtc
                + item.Reminder.Offset
                - TimeSpan.FromMinutes(NotificationSettings.EarlyFireBufferMinutes);
            if (fireTimeUtc <= DateTime.UtcNow)
                fireTimeUtc = DateTime.UtcNow.AddSeconds(5);
            DateTime fireTimeLocal = fireTimeUtc.ToLocalTime();

            if (fireTimeLocal <= DateTime.Now)
                return; // don't schedule past notifications

            var notification = new AndroidNotification
            {
                Title = item.Title,
                Text = string.IsNullOrEmpty(item.Note)
                    ? "Reminder"
                    : item.Note,
                FireTime = fireTimeLocal,
                SmallIcon = "icon_small",
                LargeIcon = "icon_large"
            };

            int id = AndroidNotificationCenter.SendNotification(
                notification,
                NotificationInitializer.Channel
            );

            // Save notification ID for later cancel/update
            item.NotificationId = id;

            NotificationLogger.Log(new NotificationLogEntry
            {
                ItemId = item.Id,
                Title = item.Title,
                IntendedUtc = item.StartUtc + item.Reminder.Offset,
                ScheduledLocal = fireTimeLocal,
                Note = "Scheduled"
            });
        }

        public static void Cancel(CalendarItem item)
        {
            if (item.NotificationId.HasValue)
            {
                AndroidNotificationCenter.CancelNotification(
                    item.NotificationId.Value);
            }
        }
    }
}

